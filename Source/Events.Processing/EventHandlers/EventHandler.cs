// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Artifacts;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Contracts;
using Dolittle.Runtime.Events.Processing.Filters;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Events.Store.Streams.Filters;
using Dolittle.Runtime.Protobuf;
using Dolittle.Runtime.Rudimentary;
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
    /// Represents an event handler in the system.
    /// </summary>
    /// <remarks>
    /// An event handler is a formalized type that consists of a filter and an event processor.
    /// The filter filters off of an event log based on the types of events the handler is interested in
    /// and puts these into a stream for the filter. From this new stream, the event processor will handle
    /// the forwarding to the client for it to handle the event.
    /// What sets an event handler apart is that it has a formalization around the stream definition that
    /// consists of the events it is interested in, which is defined from the client.
    /// </remarks>
    public class EventHandler : IDisposable
    {
        readonly IStreamProcessors _streamProcessors;
        readonly IValidateFilterForAllTenants _filterValidator;
        readonly IStreamDefinitions _streamDefinitions;
        readonly ReverseCallDispatcherType _dispatcher;
        readonly EventHandlerRegistrationArguments _arguments;
        readonly FactoryFor<IWriteEventsToStreams> _getEventsToStreamsWriter;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;
        readonly CancellationTokenSource _cancellationTokenSource;

        bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="EventHandler"/>.
        /// </summary>
        /// <param name="streamProcessors">The <see cref="IStreamProcessors" />.</param>
        /// <param name="filterValidationForAllTenants">The <see cref="IValidateFilterForAllTenants" /> for validating the filter definition.</param>
        /// <param name="streamDefinitions">The<see cref="IStreamDefinitions" />.</param>
        /// <param name="dispatcher">The actual <see cref="ReverseCallDispatcherType"/>.</param>
        /// <param name="arguments">Connecting arguments.</param>
        /// <param name="getEventsToStreamsWriter">Factory for getting <see cref="IWriteEventsToStreams"/>.</param>
        /// <param name="loggerFactory">Logger factory for logging.</param>
        /// <param name="cancellationToken">Cancellation token that can cancel the hierarchy.</param>
        public EventHandler(
            IStreamProcessors streamProcessors,
            IValidateFilterForAllTenants filterValidationForAllTenants,
            IStreamDefinitions streamDefinitions,
            ReverseCallDispatcherType dispatcher,
            EventHandlerRegistrationArguments arguments,
            FactoryFor<IWriteEventsToStreams> getEventsToStreamsWriter,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            _logger = loggerFactory.CreateLogger<EventHandler>();
            _streamProcessors = streamProcessors;
            _filterValidator = filterValidationForAllTenants;
            _streamDefinitions = streamDefinitions;
            _dispatcher = dispatcher;
            _arguments = arguments;
            _getEventsToStreamsWriter = getEventsToStreamsWriter;
            _loggerFactory = loggerFactory;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            FilterDefinition = new TypeFilterWithEventSourcePartitionDefinition(
                StreamId.EventLog,
                TargetStream,
                EventTypes,
                Partitioned);

            FilteredStreamDefinition = new StreamDefinition(
                            new TypeFilterWithEventSourcePartitionDefinition(
                                    TargetStream,
                                    TargetStream,
                                    EventTypes,
                                    Partitioned));
        }

        /// <summary>
        /// Gets the <see cref="StreamId">target stream</see> for the <see cref="EventHandler"/>.
        /// </summary>
        public StreamId TargetStream => _arguments.EventHandler.Value;

        /// <summary>
        /// Gets the <see cref="Scope"/> for the <see cref="EventHandler"/>.
        /// </summary>
        public ScopeId Scope => _arguments.Scope.Value;

        /// <summary>
        /// Gets the <see cref="EventProcessorId"/> for the <see cref="EventHandler"/>.
        /// </summary>
        public EventProcessorId EventProcessor => _arguments.EventHandler.Value;

        /// <summary>
        /// Gets the <see cref="ArtifactId"/> for the <see cref="EventHandler"/>.
        /// </summary>
        public IEnumerable<ArtifactId> EventTypes => _arguments.EventTypes;

        /// <summary>
        /// Gets whether or not the <see cref="EventHandler"/> is partitioned.
        /// </summary>
        public bool Partitioned => _arguments.Partitioned;

        /// <summary>
        /// Gets the <see cref="StreamDefinition"/> for the filtered stream.
        /// </summary>
        public StreamDefinition FilteredStreamDefinition { get; }

        /// <summary>
        /// Gets the <see cref="TypeFilterWithEventSourcePartitionDefinition"/> for the filter.
        /// </summary>
        public TypeFilterWithEventSourcePartitionDefinition FilterDefinition { get; }

        /// <summary>
        /// Gets the <see cref="StreamProcessor"/> for the filter.
        /// </summary>
        public StreamProcessor FilterStreamProcessor { get; private set; }

        /// <summary>
        /// Gets the <see cref="StreamProcessor"/> for the event processor.
        /// </summary>
        public StreamProcessor EventProcessorStreamProcessor { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    FilterStreamProcessor?.Dispose();
                    EventProcessorStreamProcessor?.Dispose();
                    _cancellationTokenSource.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Register and start the event handler for filtering and processing.
        /// </summary>
        /// <returns>Async <see cref="Task"/>.</returns>
        public async Task Register()
        {
            if (await RejectIfNonWriteableStream().ConfigureAwait(false))
            {
                return;
            }

            _logger.LogDebug($"Connecting Event Handler '{EventProcessor.Value}'");

            if (!await RegisterFilterStreamProcessor().ConfigureAwait(false)) return;
            if (!await RegisterEventProcessorStreamProcessor().ConfigureAwait(false)) return;
        }

        /// <summary>
        /// Start the event handler.
        /// </summary>
        /// <returns>Async <see cref="Task"/>.</returns>
        public async Task Start()
        {
            _logger.StartingEventHandler(FilterDefinition.TargetStream);
            try
            {
                var runningDispatcher = _dispatcher.Accept(new EventHandlerRegistrationResponse(), _cancellationTokenSource.Token);
                await FilterStreamProcessor.Initialize().ConfigureAwait(false);
                await EventProcessorStreamProcessor.Initialize().ConfigureAwait(false);
                await ValidateFilter().ConfigureAwait(false);

                var tasks = new[] { FilterStreamProcessor.Start(), EventProcessorStreamProcessor.Start(), runningDispatcher };

                try
                {
                    await Task.WhenAny(tasks).ConfigureAwait(false);

                    if (tasks.TryGetFirstInnerMostException(out var ex))
                    {
                        _logger.ErrorWhileRunningEventHandler(ex, EventProcessor, Scope);
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
                finally
                {
                    _cancellationTokenSource.Cancel();
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    _logger.EventHandlerDisconnected(EventProcessor, Scope);
                }
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _logger.ErrorWhileStartingEventHandler(ex, EventProcessor, Scope);
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }

        async Task<bool> RejectIfNonWriteableStream()
        {
            if (TargetStream.IsNonWriteable)
            {
                _logger.EventHandlerIsInvalid(EventProcessor);
                await Fail(
                    EventHandlersFailures.CannotRegisterEventHandlerOnNonWriteableStream,
                    $"Cannot register Event Handler: '{EventProcessor.Value}' because it is an invalid Stream Id").ConfigureAwait(false);

                return true;
            }
            return false;
        }

        async Task<bool> RegisterFilterStreamProcessor()
        {
            _logger.RegisteringStreamProcessorForFilter(EventProcessor);
            return await RegisterStreamProcessor(
                new EventLogStreamDefinition(),
                GetFilterProcessor,
                () => (FiltersFailures.FailedToRegisterFilter, $"Failed to register Event Handler: {EventProcessor.Value}. Filter already registered."),
                (ex) => _logger.ErrorWhileRegisteringStreamProcessorForFilter(ex, EventProcessor),
                (streamProcessor) => FilterStreamProcessor = streamProcessor
            ).ConfigureAwait(false);
        }

        async Task<bool> RegisterEventProcessorStreamProcessor()
        {
            _logger.RegisteringStreamProcessorForEventProcessor(EventProcessor, TargetStream);
            return await RegisterStreamProcessor(
                FilteredStreamDefinition,
                GetEventProcessor,
                () => (FiltersFailures.FailedToRegisterFilter, $"Failed to register Event Handler: {EventProcessor.Value}. Event Processor already registered on Source Stream: '{EventProcessor.Value}'"),
                (ex) => _logger.ErrorWhileRegisteringStreamProcessorForFilter(ex, EventProcessor),
                (streamProcessor) => EventProcessorStreamProcessor = streamProcessor
            ).ConfigureAwait(false);
        }

        async Task<bool> RegisterStreamProcessor(IStreamDefinition streamDefinition, FactoryFor<IEventProcessor> getProcessor, Func<(FailureId, string)> onFailure, Action<Exception> onException, Action<StreamProcessor> onStreamProcessor)
        {
            _logger.RegisteringStreamProcessorForFilter(EventProcessor);
            try
            {
                var success = _streamProcessors.TryRegister(
                    Scope,
                    EventProcessor,
                    streamDefinition,
                    getProcessor,
                    _cancellationTokenSource.Token,
                    out var streamProcessor);

                onStreamProcessor(streamProcessor);

                if (!success)
                {
                    _logger.EventHandlerAlreadyRegistered(EventProcessor);
                    var (failure, message) = onFailure();
                    await Fail(
                        failure,
                        message).ConfigureAwait(false);
                }

                return success;
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    onException(ex);
                }

                _logger.ErrorWhileRegisteringEventHandler(ex, EventProcessor);
                ExceptionDispatchInfo.Capture(ex).Throw();

                return false;
            }
        }

        IFilterProcessor<TypeFilterWithEventSourcePartitionDefinition> GetFilterProcessor()
        {
            return new TypeFilterWithEventSourcePartition(
                    Scope,
                    FilterDefinition,
                    _getEventsToStreamsWriter(),
                    _loggerFactory.CreateLogger<TypeFilterWithEventSourcePartition>());
        }

        IEventProcessor GetEventProcessor()
        {
            return new EventProcessor(
                    Scope,
                    EventProcessor,
                    _dispatcher,
                    _loggerFactory.CreateLogger<EventProcessor>());
        }

        async Task Fail(FailureId failureId, string message)
        {
            var failure = new Failure(failureId, message);
            await _dispatcher.Reject(new EventHandlerRegistrationResponse { Failure = failure }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }

        async Task ValidateFilter()
        {
            _logger.ValidatingFilter(FilterDefinition.TargetStream);
            var filterValidationResults = await _filterValidator.Validate(GetFilterProcessor, _cancellationTokenSource.Token).ConfigureAwait(false);

            if (filterValidationResults.Any(_ => !_.Value.Succeeded))
            {
                var firstFailedValidation = filterValidationResults.First(_ => !_.Value.Succeeded).Value;
                _logger.FilterValidationFailed(FilterDefinition.TargetStream, firstFailedValidation.FailureReason);
                throw new FilterValidationFailed(FilterDefinition.TargetStream, firstFailedValidation.FailureReason);
            }

            var filteredStreamDefinition = new StreamDefinition(FilterDefinition);
            _logger.PersistingStreamDefinition(filteredStreamDefinition.StreamId);
            await _streamDefinitions.Persist(Scope, filteredStreamDefinition, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }
}