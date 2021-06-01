// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Rudimentary;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Contracts;
using Dolittle.Runtime.Events.Processing.Filters;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Events.Store.Streams.Filters;
using Dolittle.Runtime.Execution;
using Microsoft.Extensions.Logging;
using Dolittle.Runtime.Protobuf;
using Dolittle.Runtime.Services;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using static Dolittle.Runtime.Events.Processing.Contracts.EventHandlers;

namespace Dolittle.Runtime.Events.Processing.EventHandlers
{
    /// <summary>
    /// Represents the implementation of <see cref="EventHandlersBase"/>.
    /// </summary>
    public class EventHandlersService : EventHandlersBase
    {
        readonly IValidateFilterForAllTenants _filterForAllTenants;
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
            _filterForAllTenants = filterForAllTenants;
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
            var tryConnect = await _reverseCallServices.Connect(
                runtimeStream,
                clientStream,
                context,
                _eventHandlersProtocol,
                cts.Token).ConfigureAwait(false);
            if (!tryConnect.Success) return;
            var (dispatcher, arguments) = tryConnect.Result;
            _logger.SettingExecutionContext(arguments.ExecutionContext);
            _executionContextManager.CurrentFor(arguments.ExecutionContext);

            StreamId targetStream = arguments.EventHandler.Value;

            _logger.ReceivedEventHandler(StreamId.EventLog, arguments.EventHandler, arguments.Scope, arguments.EventTypes, arguments.Partitioned);
            if (await RejectIfNonWriteableStream(dispatcher, arguments.EventHandler, cts.Token).ConfigureAwait(false)) return;

            using var _ = context.CancellationToken.Register(() => _logger.LogTrace("Event Handler '{EventHandlerId}' was cancelled by the client", arguments.EventHandler.Value));

            _logger.LogDebug("Connecting Event Handler '{EventHandlerId}'", arguments.EventHandler.Value);
            var filterDefinition = new TypeFilterWithEventSourcePartitionDefinition(StreamId.EventLog, targetStream, arguments.EventTypes, arguments.Partitioned);

            Func<IFilterProcessor<TypeFilterWithEventSourcePartitionDefinition>> getFilterProcessor = () => new TypeFilterWithEventSourcePartition(
                    arguments.Scope,
                    filterDefinition,
                    _getEventsToStreamsWriter(),
                    _loggerFactory.CreateLogger<TypeFilterWithEventSourcePartition>());
            var tryRegisterFilterStreamProcessor = TryRegisterFilterStreamProcessor<TypeFilterWithEventSourcePartitionDefinition>(
                arguments.Scope,
                arguments.EventHandler,
                getFilterProcessor,
                cts.Token);

            if (!tryRegisterFilterStreamProcessor.Success)
            {
                if (tryRegisterFilterStreamProcessor.HasException)
                {
                    var exception = tryRegisterFilterStreamProcessor.Exception;
                    _logger.ErrorWhileRegisteringEventHandler(exception, arguments.EventHandler);
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }
                else
                {
                    _logger.EventHandlerAlreadyRegistered(arguments.EventHandler);
                    var failure = new Failure(
                        FiltersFailures.FailedToRegisterFilter,
                        $"Failed to register Event Handler: {arguments.EventHandler.Value}. Filter already registered.");
                    await dispatcher.Reject(new EventHandlerRegistrationResponse { Failure = failure }, cts.Token).ConfigureAwait(false);
                    return;
                }
            }

            using var filterStreamProcessor = tryRegisterFilterStreamProcessor.Result;

            // This should be the stream definition of the filtered stream for an event processor to use
            var filteredStreamDefinition = new StreamDefinition(new TypeFilterWithEventSourcePartitionDefinition(targetStream, targetStream, arguments.EventTypes, arguments.Partitioned));
            var tryRegisterEventProcessorStreamProcessor = TryRegisterEventProcessorStreamProcessor(
                arguments.Scope,
                arguments.EventHandler,
                filteredStreamDefinition,
                () => new EventProcessor(
                    arguments.Scope,
                    arguments.EventHandler,
                    dispatcher,
                    _loggerFactory.CreateLogger<EventProcessor>()),
                cts.Token);

            if (!tryRegisterEventProcessorStreamProcessor.Success)
            {
                if (tryRegisterEventProcessorStreamProcessor.HasException)
                {
                    var exception = tryRegisterEventProcessorStreamProcessor.Exception;
                    _logger.ErrorWhileRegisteringEventHandler(exception, arguments.EventHandler);
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }
                else
                {
                    _logger.EventHandlerAlreadyRegisteredOnSourceStream(arguments.EventHandler);
                    var failure = new Failure(
                        FiltersFailures.FailedToRegisterFilter,
                        $"Failed to register Event Handler: {arguments.EventHandler.Value}. Event Processor already registered on Source Stream: '{arguments.EventHandler.Value}'");
                    await dispatcher.Reject(new EventHandlerRegistrationResponse { Failure = failure }, cts.Token).ConfigureAwait(false);
                    return;
                }
            }

            using var eventProcessorStreamProcessor = tryRegisterEventProcessorStreamProcessor.Result;

            var tryStartEventHandler = await TryStartEventHandler(
                dispatcher,
                filterStreamProcessor,
                eventProcessorStreamProcessor,
                arguments.Scope,
                filterDefinition,
                getFilterProcessor,
                cts.Token).ConfigureAwait(false);
            if (!tryStartEventHandler.Success)
            {
                cts.Cancel();
                if (tryStartEventHandler.HasException)
                {
                    var exception = tryStartEventHandler.Exception;
                    _logger.ErrorWhileStartingEventHandler(exception, arguments.EventHandler, arguments.Scope);
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }
                else
                {
                    _logger.CouldNotStartEventHandler(arguments.EventHandler, arguments.Scope);
                    return;
                }
            }

            var tasks = tryStartEventHandler.Result;
            try
            {
                await Task.WhenAny(tasks).ConfigureAwait(false);

                if (tasks.TryGetFirstInnerMostException(out var ex))
                {
                    _logger.ErrorWhileRunningEventHandler(ex, arguments.EventHandler, arguments.Scope);
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }
            finally
            {
                cts.Cancel();
                await Task.WhenAll(tasks).ConfigureAwait(false);
                _logger.EventHandlerDisconnected(arguments.EventHandler, arguments.Scope);
            }
        }

        async Task<bool> RejectIfNonWriteableStream(
            IReverseCallDispatcher<EventHandlerClientToRuntimeMessage, EventHandlerRuntimeToClientMessage, EventHandlerRegistrationRequest, EventHandlerRegistrationResponse, HandleEventRequest, EventHandlerResponse> dispatcher,
            EventProcessorId eventHandler,
            CancellationToken cancellationToken)
        {
            StreamId targetStream = eventHandler.Value;
            if (targetStream.IsNonWriteable)
            {
                _logger.EventHandlerIsInvalid(eventHandler);
                var failure = new Failure(
                    EventHandlersFailures.CannotRegisterEventHandlerOnNonWriteableStream,
                    $"Cannot register Event Handler: '{eventHandler.Value}' because it is an invalid Stream Id");
                await dispatcher.Reject(new EventHandlerRegistrationResponse { Failure = failure }, cancellationToken).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        async Task<Try<IEnumerable<Task>>> TryStartEventHandler<TClientMessage, TConnectRequest, TResponse, TFilterDefinition>(
            IReverseCallDispatcher<TClientMessage, EventHandlerRuntimeToClientMessage, TConnectRequest, EventHandlerRegistrationResponse, HandleEventRequest, TResponse> dispatcher,
            StreamProcessor filterStreamProcessor,
            StreamProcessor eventProcessorStreamProcessor,
            ScopeId scopeId,
            TFilterDefinition filterDefinition,
            Func<IFilterProcessor<TFilterDefinition>> getFilterProcessor,
            CancellationToken cancellationToken)
            where TClientMessage : IMessage, new()
            where TConnectRequest : class
            where TResponse : class
            where TFilterDefinition : IFilterDefinition
        {
            _logger.StartingEventHandler(filterDefinition.TargetStream);
            try
            {
                var runningDispatcher = dispatcher.Accept(new EventHandlerRegistrationResponse(), cancellationToken);
                await filterStreamProcessor.Initialize().ConfigureAwait(false);
                await eventProcessorStreamProcessor.Initialize().ConfigureAwait(false);
                await ValidateFilter(
                    scopeId,
                    filterDefinition,
                    getFilterProcessor,
                    cancellationToken).ConfigureAwait(false);
                return new[] { filterStreamProcessor.Start(), eventProcessorStreamProcessor.Start(), runningDispatcher };
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.ErrorWhileStartingEventHandler(ex, filterDefinition.TargetStream, scopeId);
                }

                return ex;
            }
        }

        Try<StreamProcessor> TryRegisterFilterStreamProcessor<TFilterDefinition>(
            ScopeId scopeId,
            EventProcessorId eventHandlerId,
            Func<IFilterProcessor<TypeFilterWithEventSourcePartitionDefinition>> getFilterProcessor,
            CancellationToken cancellationToken)
            where TFilterDefinition : IFilterDefinition
        {
            _logger.RegisteringStreamProcessorForFilter(eventHandlerId);
            try
            {
                return (_streamProcessors.TryRegister(
                    scopeId,
                    eventHandlerId,
                    new EventLogStreamDefinition(),
                    getFilterProcessor,
                    cancellationToken,
                    out var outputtedFilterStreamProcessor), outputtedFilterStreamProcessor);
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.ErrorWhileRegisteringStreamProcessorForFilter(ex, eventHandlerId);
                }

                return ex;
            }
        }

        Try<StreamProcessor> TryRegisterEventProcessorStreamProcessor(
            ScopeId scopeId,
            EventProcessorId eventHandlerId,
            IStreamDefinition sourceStreamDefinition,
            Func<IEventProcessor> getEventProcessor,
            CancellationToken cancellationToken)
        {
            _logger.RegisteringStreamProcessorForEventProcessor(eventHandlerId, sourceStreamDefinition.StreamId);
            try
            {
                return (_streamProcessors.TryRegister(
                    scopeId,
                    eventHandlerId,
                    sourceStreamDefinition,
                    getEventProcessor,
                    cancellationToken,
                    out var outputtedEventProcessorStreamProcessor), outputtedEventProcessorStreamProcessor);
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.ErrorWhileRegisteringStreamProcessorForEventProcessor(ex, eventHandlerId);
                }

                return ex;
            }
        }

        async Task ValidateFilter<TFilterDefinition>(
            ScopeId scopeId,
            TFilterDefinition filterDefinition,
            Func<IFilterProcessor<TFilterDefinition>> getFilterProcessor,
            CancellationToken cancellationToken)
            where TFilterDefinition : IFilterDefinition
        {
            _logger.ValidatingFilter(filterDefinition.TargetStream);
            var filterValidationResults = await _filterForAllTenants.Validate(getFilterProcessor, cancellationToken).ConfigureAwait(false);

            if (filterValidationResults.Any(_ => !_.Value.Succeeded))
            {
                var firstFailedValidation = filterValidationResults.Select(_ => _.Value).First(_ => !_.Succeeded);
                _logger.FilterValidationFailed(filterDefinition.TargetStream, firstFailedValidation.FailureReason);
                throw new FilterValidationFailed(filterDefinition.TargetStream, firstFailedValidation.FailureReason);
            }

            var filteredStreamDefinition = new StreamDefinition(filterDefinition);
            _logger.PersistingStreamDefinition(filteredStreamDefinition.StreamId);
            await _streamDefinitions.Persist(scopeId, filteredStreamDefinition, cancellationToken).ConfigureAwait(false);
        }
    }
}
