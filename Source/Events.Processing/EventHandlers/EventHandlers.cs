// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Filters;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Lifecycle;
using Microsoft.Extensions.Logging;
using ReverseCallDispatcherType = Dolittle.Runtime.Services.IReverseCallDispatcher<
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerClientToRuntimeMessage,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerRuntimeToClientMessage,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerRegistrationRequest,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerRegistrationResponse,
                                    Dolittle.Runtime.Events.Processing.Contracts.HandleEventRequest,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerResponse>;

namespace Dolittle.Runtime.Events.Processing.EventHandlers
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventHandlers"/>.
    /// </summary>
    [Singleton]
    public class EventHandlers : IEventHandlers
    {
        readonly IStreamProcessors _streamProcessors;
        readonly IValidateFilterForAllTenants _filterValidator;
        readonly IStreamDefinitions _streamDefinitions;
        readonly FactoryFor<IWriteEventsToStreams> _getEventsToStreamsWriter;
        readonly ILoggerFactory _loggerFactory;
        ConcurrentBag<EventHandler> _eventHandlers = new();

        /// <summary>
        /// Initializes a new instance of <see cref="EventHandlers"/>.
        /// </summary>
        /// <param name="streamProcessors">The <see cref="IStreamProcessors" />.</param>
        /// <param name="filterValidator">The <see cref="IValidateFilterForAllTenants" /> for validating the filter definition.</param>
        /// <param name="streamDefinitions">The<see cref="IStreamDefinitions" />.</param>
        /// <param name="getEventsToStreamsWriter">Factory for getting <see cref="IWriteEventsToStreams"/>.</param>
        /// <param name="loggerFactory">Logger factory for logging.</param>
        public EventHandlers(
            IStreamProcessors streamProcessors,
            IValidateFilterForAllTenants filterValidator,
            IStreamDefinitions streamDefinitions,
            FactoryFor<IWriteEventsToStreams> getEventsToStreamsWriter,
            ILoggerFactory loggerFactory)
        {
            _streamProcessors = streamProcessors;
            _filterValidator = filterValidator;
            _streamDefinitions = streamDefinitions;
            _getEventsToStreamsWriter = getEventsToStreamsWriter;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public IEnumerable<EventHandler> All => _eventHandlers.ToArray();

        /// <inheritdoc/>
        public async Task<EventHandler> Register(
            ReverseCallDispatcherType dispatcher,
            EventHandlerRegistrationArguments arguments,
            CancellationToken cancellationToken)
        {
            var eventHandler = new EventHandler(
                    _streamProcessors,
                    _filterValidator,
                    _streamDefinitions,
                    dispatcher,
                    arguments,
                    _getEventsToStreamsWriter,
                    _loggerFactory,
                    cancellationToken
                );

            await eventHandler.Register().ConfigureAwait(false);
            _eventHandlers.Add(eventHandler);

            return eventHandler;
        }

        /// <inheritdoc/>
        public void Unregister(EventHandler eventHandler)
        {
            eventHandler.Dispose();
            _eventHandlers = new(_eventHandlers.Except(new[] { eventHandler }));
        }
    }
}

