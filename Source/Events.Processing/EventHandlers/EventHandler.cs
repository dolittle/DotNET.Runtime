// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Artifacts;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Contracts;
using Dolittle.Runtime.Events.Processing.EventProcessors;
using Dolittle.Runtime.Events.Processing.Filters;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Events.Store.Streams.Filters;
using Dolittle.Runtime.Protobuf;
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
    public class EventHandler : EventProcessorBase<EventHandlerClientToRuntimeMessage, EventHandlerRuntimeToClientMessage, EventHandlerRegistrationRequest, EventHandlerRegistrationResponse, HandleEventRequest, EventHandlerResponse, EventHandlerRegistrationArguments>
    {
        readonly IValidateFilterForAllTenants _filterValidator;
        readonly IStreamDefinitions _streamDefinitions;
        readonly FactoryFor<IWriteEventsToStreams> _getEventsToStreamsWriter;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;
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
            : base(streamProcessors, dispatcher, arguments, cancellationToken)
        {
            _logger = loggerFactory.CreateLogger<EventHandler>();
            _filterValidator = filterValidationForAllTenants;
            _streamDefinitions = streamDefinitions;
            _getEventsToStreamsWriter = getEventsToStreamsWriter;
            _loggerFactory = loggerFactory;

            TargetStream = arguments.EventHandler.Value;
            EventTypes = arguments.EventTypes;
            Partitioned = arguments.Partitioned;

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
        public StreamId TargetStream { get; }

        /// <summary>
        /// Gets the <see cref="ArtifactId"/> for the <see cref="EventHandler"/>.
        /// </summary>
        public IEnumerable<ArtifactId> EventTypes { get; }

        /// <summary>
        /// Gets whether or not the <see cref="EventHandler"/> is partitioned.
        /// </summary>
        public bool Partitioned { get; }

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

        /// <summary>
        /// Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                FilterStreamProcessor?.Dispose();
                EventProcessorStreamProcessor?.Dispose();
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        public override async Task RegisterAndStart()
        {
            _logger.LogDebug($"Connecting Event Handler '{EventProcessor.Value}'");
            if (await RejectIfNonWriteableStream().ConfigureAwait(false)
                || !await RegisterFilterStreamProcessor().ConfigureAwait(false)
                || !await RegisterEventProcessorStreamProcessor().ConfigureAwait(false))
            {
                return;
            }

            await Start(
                Dispatcher.Accept(CreateConnectResponse(), CancellationToken),
                ValidateFilter,
                exception => _logger.ErrorWhileStartingEventHandler(exception, EventProcessor, Scope),
                exception => _logger.ErrorWhileRunningEventHandler(exception, EventProcessor, Scope)).ConfigureAwait(false);
            _logger.EventHandlerDisconnected(EventProcessor, Scope);
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
                HandleFailedToRegisterFilter,
                (streamProcessor) => FilterStreamProcessor = streamProcessor
            ).ConfigureAwait(false);
        }

        Failure HandleFailedToRegisterFilter(Exception exception)
        {
            _logger.ErrorWhileRegisteringStreamProcessorForFilter(exception, EventProcessor);

            if (exception is StreamProcessorAlreadyRegistered)
            {
                _logger.EventHandlerAlreadyRegistered(EventProcessor);
                return new(
                    EventHandlersFailures.FailedToRegisterEventHandler,
                    $"Failed to register Event Handler: {EventProcessor.Value}. Filter already registered");
            }
            return new(
                    EventHandlersFailures.FailedToRegisterEventHandler,
                    $"Failed to register Event Handler: {EventProcessor.Value}. An error occurred. {exception.Message}");
        }

        async Task<bool> RegisterEventProcessorStreamProcessor()
        {
            _logger.RegisteringStreamProcessorForEventProcessor(EventProcessor, TargetStream);
            return await RegisterStreamProcessor(
                FilteredStreamDefinition,
                GetEventProcessor,
                HandleFailedToRegisterEventProcessor,
                (streamProcessor) => EventProcessorStreamProcessor = streamProcessor
            ).ConfigureAwait(false);
        }

        Failure HandleFailedToRegisterEventProcessor(Exception exception)
        {
            _logger.ErrorWhileRegisteringStreamProcessorForEventProcessor(exception, EventProcessor);

            if (exception is StreamProcessorAlreadyRegistered)
            {
                _logger.EventHandlerAlreadyRegisteredOnSourceStream(EventProcessor);
                return new(
                    EventHandlersFailures.FailedToRegisterEventHandler,
                    $"Failed to register Event Handler: {EventProcessor.Value}. Event Processor already registered on Source Stream: '{EventProcessor.Value}'");
            }
            return new(
                    EventHandlersFailures.FailedToRegisterEventHandler,
                    $"Failed to register Event Handler: {EventProcessor.Value}. An error occurred. {exception.Message}");
        }

        async Task ValidateFilter()
        {
            _logger.ValidatingFilter(FilterDefinition.TargetStream);
            var filterValidationResults = await _filterValidator.Validate(GetFilterProcessor, CancellationToken).ConfigureAwait(false);

            if (filterValidationResults.Any(_ => !_.Value.Success))
            {
                var firstFailedValidation = filterValidationResults.First(_ => !_.Value.Success).Value;
                _logger.FilterValidationFailed(FilterDefinition.TargetStream, firstFailedValidation.FailureReason);
                throw new FilterValidationFailed(FilterDefinition.TargetStream, firstFailedValidation.FailureReason);
            }

            var filteredStreamDefinition = new StreamDefinition(FilterDefinition);
            _logger.PersistingStreamDefinition(filteredStreamDefinition.StreamId);
            await _streamDefinitions.Persist(Scope, filteredStreamDefinition, CancellationToken).ConfigureAwait(false);
        }

        IFilterProcessor<TypeFilterWithEventSourcePartitionDefinition> GetFilterProcessor()
            => new TypeFilterWithEventSourcePartition(
                    Scope,
                    FilterDefinition,
                    _getEventsToStreamsWriter(),
                    _loggerFactory.CreateLogger<TypeFilterWithEventSourcePartition>());

        IEventProcessor GetEventProcessor()
            => new EventProcessor(
                    Scope,
                    EventProcessor,
                    Dispatcher,
                    _loggerFactory.CreateLogger<EventProcessor>());

        protected override EventHandlerRegistrationResponse CreateConnectResponse()
            => new();
        protected override EventHandlerRegistrationResponse CreateConnectResponse(Failure failure)
            => new() { Failure = failure };
    }
}
