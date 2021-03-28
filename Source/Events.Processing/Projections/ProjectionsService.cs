// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Rudimentary;
using Dolittle.Runtime.Events.Processing.Contracts;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Execution;
using Microsoft.Extensions.Logging;
using Dolittle.Runtime.Protobuf;
using Dolittle.Runtime.Services;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using static Dolittle.Runtime.Events.Processing.Contracts.Projections;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Projections.Store.State;
using Dolittle.Runtime.Projections.Store.Definition;
using Dolittle.Runtime.Tenancy;

namespace Dolittle.Runtime.Events.Processing.Projections
{
    /// <summary>
    /// Represents the implementation of <see cref="ProjectionsBase"/>.
    /// </summary>
    public class ProjectionsService : ProjectionsBase
    {
        readonly IStreamProcessors _streamProcessors;
        readonly IExecutionContextManager _executionContextManager;
        readonly IInitiateReverseCallServices _reverseCallServices;
        readonly IProjectionsProtocol _protocol;
        readonly ICompareProjectionDefinitionsForAllTenants _projectionDefinitionComparer;
        readonly FactoryFor<IProjectionStates> _getProjectionStates;
        readonly FactoryFor<IProjectionDefinitions> _getProjectionDefinitions;
        readonly IProjectionKeys _projectionKeys;
        readonly IPerformActionOnAllTenants _onAllTenants;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;
        readonly IHostApplicationLifetime _hostApplicationLifetime;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlersService"/> class.
        /// </summary>
        /// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime" />.</param>
        /// <param name="streamProcessors">The <see cref="IStreamProcessors" />.</param>
        /// <param name="executionContextManager">The <see cref="IExecutionContextManager" />.</param>
        /// <param name="reverseCallServices">The <see cref="IInitiateReverseCallServices" />.</param>
        /// <param name="protocol">The <see cref="IProjectionsProtocol" />.</param>
        /// <param name="projectionDefinitionComparer">The <see cref="ICompareProjectionDefinitionsForAllTenants" />.</param>
        /// <param name="getProjectionStates">The <see cref="FactoryFor{T}" /> for <see cref="IProjectionStates" />.</param>
        /// <param name="getProjectionDefinitions">The <see cref="FactoryFor{T}" /> for <see cref="IProjectionDefinitions" />.</param>
        /// <param name="projectionKeys">The <see cref="IProjectionKeys" />.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ProjectionsService(
            IHostApplicationLifetime hostApplicationLifetime,
            IStreamProcessors streamProcessors,
            IExecutionContextManager executionContextManager,
            IInitiateReverseCallServices reverseCallServices,
            IProjectionsProtocol protocol,
            ICompareProjectionDefinitionsForAllTenants projectionDefinitionComparer,
            FactoryFor<IProjectionStates> getProjectionStates,
            FactoryFor<IProjectionDefinitions> getProjectionDefinitions,
            IProjectionKeys projectionKeys,
            IPerformActionOnAllTenants onAllTenants,
            ILoggerFactory loggerFactory)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _streamProcessors = streamProcessors;
            _executionContextManager = executionContextManager;
            _reverseCallServices = reverseCallServices;
            _protocol = protocol;
            _projectionDefinitionComparer = projectionDefinitionComparer;
            _getProjectionStates = getProjectionStates;
            _getProjectionDefinitions = getProjectionDefinitions;
            _projectionKeys = projectionKeys;
            _onAllTenants = onAllTenants;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ProjectionsService>();
        }

        /// <inheritdoc/>
        public override async Task Connect(
            IAsyncStreamReader<ProjectionClientToRuntimeMessage> runtimeStream,
            IServerStreamWriter<ProjectionRuntimeToClientMessage> clientStream,
            ServerCallContext context)
        {
            _logger.LogDebug("Connecting Projections");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_hostApplicationLifetime.ApplicationStopping, context.CancellationToken);
            var tryConnect = await _reverseCallServices.Connect(
                runtimeStream,
                clientStream,
                context,
                _protocol,
                cts.Token).ConfigureAwait(false);
            if (!tryConnect.Success) return;
            var (dispatcher, arguments) = tryConnect.Result;
            _logger.ReceivedProjection(arguments);
            _logger.SettingExecutionContext(arguments.ExecutionContext);
            _executionContextManager.CurrentFor(arguments.ExecutionContext);

            var tryRegisterEventProcessorStreamProcessor = TryRegisterProjection(
                arguments,
                () => new EventProcessor(
                    arguments.ProjectionDefinition,
                    dispatcher,
                    _getProjectionStates(),
                    _projectionKeys,
                    _loggerFactory.CreateLogger<EventProcessor>()),
                cts.Token);

            if (!tryRegisterEventProcessorStreamProcessor.Success)
            {
                if (tryRegisterEventProcessorStreamProcessor.HasException)
                {
                    var exception = tryRegisterEventProcessorStreamProcessor.Exception;
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }
                else
                {
                    _logger.ProjectionAlreadyRegistered(arguments);
                    var failure = new Failure(
                        ProjectionFailures.FailedToRegisterProjection,
                        $"Failed to register Projection: {arguments.ProjectionDefinition.Projection.Value}. Event Processor already registered with the same id.");
                    await dispatcher.Reject(new ProjectionRegistrationResponse { Failure = failure }, cts.Token).ConfigureAwait(false);
                    return;
                }
            }

            using var eventProcessorStreamProcessor = tryRegisterEventProcessorStreamProcessor.Result;

            var tryStartEventHandler = await TryStartProjection(
                dispatcher,
                arguments,
                eventProcessorStreamProcessor,
                cts.Token).ConfigureAwait(false);
            if (!tryStartEventHandler.Success)
            {
                cts.Cancel();
                if (tryStartEventHandler.HasException)
                {
                    var exception = tryStartEventHandler.Exception;
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }
                else
                {
                    _logger.CouldNotStartProjection(arguments);
                    return;
                }
            }

            var tasks = tryStartEventHandler.Result;
            try
            {
                await Task.WhenAny(tasks).ConfigureAwait(false);

                if (tasks.TryGetFirstInnerMostException(out var ex))
                {
                    _logger.ErrorWhileRunningProjection(ex, arguments);
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }
            finally
            {
                cts.Cancel();
                await Task.WhenAll(tasks).ConfigureAwait(false);
                _logger.ProjectionDisconnected(arguments);
            }
        }

        async Task<Try<IEnumerable<Task>>> TryStartProjection(
            IReverseCallDispatcher<ProjectionClientToRuntimeMessage, ProjectionRuntimeToClientMessage, ProjectionRegistrationRequest, ProjectionRegistrationResponse, ProjectionRequest, ProjectionResponse> dispatcher,
            ProjectionRegistrationArguments arguments,
            StreamProcessor eventProcessorStreamProcessor,
            CancellationToken cancellationToken)
        {
            _logger.StartingProjection(arguments);
            try
            {
                var runningDispatcher = dispatcher.Accept(new ProjectionRegistrationResponse(), cancellationToken);

                await ResetIfDefinitionChanged(arguments, cancellationToken).ConfigureAwait(false);
                await eventProcessorStreamProcessor.Initialize().ConfigureAwait(false);
                return new[] { eventProcessorStreamProcessor.Start(), runningDispatcher };
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.ErrorWhileStartingProjection(ex, arguments);
                }

                return ex;
            }
        }

        Try<StreamProcessor> TryRegisterProjection(
            ProjectionRegistrationArguments arguments,
            Func<IEventProcessor> getEventProcessor,
            CancellationToken cancellationToken)
        {
            _logger.RegisteringProjection(arguments);
            try
            {
                return (_streamProcessors.TryRegister(
                    arguments.ProjectionDefinition.Scope,
                    arguments.ProjectionDefinition.Projection.Value,
                    new EventLogStreamDefinition(),
                    getEventProcessor,
                    cancellationToken,
                    out var outputtedEventProcessorStreamProcessor), outputtedEventProcessorStreamProcessor);
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.ErrorWhileRegisteringProjection(ex, arguments);
                }

                return ex;
            }
        }
        async Task ResetIfDefinitionChanged(
            ProjectionRegistrationArguments arguments,
            CancellationToken token)
        {
            _logger.ComparingProjectionDefintion(arguments);
            var tenantsAndComparisonResult = await _projectionDefinitionComparer.DiffersFromPersisted(arguments.ProjectionDefinition, token).ConfigureAwait(false);

            await _onAllTenants.PerformAsync(async tenant =>
            {
                if (!tenantsAndComparisonResult.TryGetValue(tenant, out var comparisonResult)) return;
                _logger.PersistingProjectionDefinition(arguments, tenant);
                if (!await _getProjectionDefinitions().TryPersist(arguments.ProjectionDefinition, token).ConfigureAwait(false)) throw new CouldNotPersistProjectionDefinition(arguments.ProjectionDefinition, tenant);
                if (!comparisonResult.Succeeded)
                {
                    _logger.ResetingProjections(arguments, tenant, comparisonResult.FailureReason);
                    if (!await _getProjectionStates().TryDrop(arguments.ProjectionDefinition.Projection, arguments.ProjectionDefinition.Scope, token).ConfigureAwait(false)) throw new CouldNotResetProjectionStates(arguments.ProjectionDefinition, tenant);
                }
            });
        }
    }
}
