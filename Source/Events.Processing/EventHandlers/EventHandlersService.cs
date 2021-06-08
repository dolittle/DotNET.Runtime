// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Contracts;
using Dolittle.Runtime.Events.Processing.Filters;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Services;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Dolittle.Runtime.Events.Processing.Contracts.EventHandlers;

namespace Dolittle.Runtime.Events.Processing.EventHandlers
{
    /// <summary>
    /// Represents the implementation of <see cref="EventHandlersBase"/>.
    /// </summary>
    public class EventHandlersService : EventHandlersBase
    {
        readonly IValidateFilterForAllTenants _filterValidator;
        readonly IStreamProcessors _streamProcessors;
        readonly FactoryFor<IWriteEventsToStreams> _getEventsToStreamsWriter;
        readonly IStreamDefinitions _streamDefinitions;
        readonly IExecutionContextManager _executionContextManager;
        readonly IInitiateReverseCallServices _reverseCallServices;
        readonly IEventHandlersProtocol _eventHandlersProtocol;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;
        readonly IHostApplicationLifetime _hostApplicationLifetime;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlersService"/> class.
        /// </summary>
        /// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime" />.</param>
        /// <param name="filterForAllTenants">The <see cref="IValidateFilterForAllTenants" />.</param>
        /// <param name="streamProcessors">The <see cref="IStreamProcessors" />.</param>
        /// <param name="getEventsToStreamsWriter">The <see cref="FactoryFor{T}" /> <see cref="IWriteEventsToStreams" />.</param>
        /// <param name="streamDefinitions">The<see cref="IStreamDefinitions" />.</param>
        /// <param name="executionContextManager">The <see cref="IExecutionContextManager" />.</param>
        /// <param name="reverseCallServices">The <see cref="IInitiateReverseCallServices" />.</param>
        /// <param name="eventHandlersProtocol">The <see cref="IEventHandlersProtocol" />.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public EventHandlersService(
            IHostApplicationLifetime hostApplicationLifetime,
            IValidateFilterForAllTenants filterForAllTenants,
            IStreamProcessors streamProcessors,
            FactoryFor<IWriteEventsToStreams> getEventsToStreamsWriter,
            IStreamDefinitions streamDefinitions,
            IExecutionContextManager executionContextManager,
            IInitiateReverseCallServices reverseCallServices,
            IEventHandlersProtocol eventHandlersProtocol,
            ILoggerFactory loggerFactory)
        {
            _filterValidator = filterForAllTenants;
            _streamProcessors = streamProcessors;
            _getEventsToStreamsWriter = getEventsToStreamsWriter;
            _streamDefinitions = streamDefinitions;
            _executionContextManager = executionContextManager;
            _reverseCallServices = reverseCallServices;
            _eventHandlersProtocol = eventHandlersProtocol;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<EventHandlersService>();
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        /// <inheritdoc/>
        public override async Task Connect(
            IAsyncStreamReader<EventHandlerClientToRuntimeMessage> runtimeStream,
            IServerStreamWriter<EventHandlerRuntimeToClientMessage> clientStream,
            ServerCallContext context)
        {
            _logger.LogDebug("Connecting Event Handler");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_hostApplicationLifetime.ApplicationStopping, context.CancellationToken);
            try
            {
                var connectResult = await _reverseCallServices.Connect(
                    runtimeStream,
                    clientStream,
                    context,
                    _eventHandlersProtocol,
                    cts.Token).ConfigureAwait(false);
                if (!connectResult.Success) return;

                var (dispatcher, arguments) = connectResult.Result;
                _logger.SettingExecutionContext(arguments.ExecutionContext);
                _executionContextManager.CurrentFor(arguments.ExecutionContext);

                using var eventHandler = new EventHandler(
                    _streamProcessors,
                    _filterValidator,
                    _streamDefinitions,
                    dispatcher,
                    arguments,
                    _getEventsToStreamsWriter,
                    _loggerFactory,
                    cts.Token
                );
                await eventHandler.RegisterAndStart().ConfigureAwait(false);
            }
            finally
            {
                cts.Cancel();
            }
        }
    }
}
