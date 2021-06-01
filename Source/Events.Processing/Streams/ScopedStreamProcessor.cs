// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.ApplicationModel;
using Dolittle.Runtime.Events.Store.Streams;
using Microsoft.Extensions.Logging;
using Dolittle.Runtime.Resilience;

namespace Dolittle.Runtime.Events.Processing.Streams
{
    /// <summary>
    /// Represents an implementation of <see cref="AbstractScopedStreamProcessor" /> that processes an unpartitioned stream of events.
    /// </summary>
    public class ScopedStreamProcessor : AbstractScopedStreamProcessor
    {
        readonly IResilientStreamProcessorStateRepository _streamProcessorStates;
        readonly ICanGetTimeToRetryFor<StreamProcessorState> _timeToRetryGetter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedStreamProcessor"/> class.
        /// </summary>
        /// <param name="tenantId">The <see cref="TenantId"/>.</param>
        /// <param name="streamProcessorId">The <see cref="IStreamProcessorId" />.</param>
        /// <param name="sourceStreamDefinition">The source stream <see cref="IStreamDefinition" />.</param>
        /// <param name="initialState">The <see cref="StreamProcessorState" />.</param>
        /// <param name="processor">An <see cref="IEventProcessor" /> to process the event.</param>
        /// <param name="streamProcessorStates">The <see cref="IResilientStreamProcessorStateRepository" />.</param>
        /// <param name="eventsFromStreamsFetcher">The<see cref="ICanFetchEventsFromStream" />.</param>
        /// <param name="eventsFetcherPolicy">The <see cref="IAsyncPolicyFor{T}" /> <see cref="ICanFetchEventsFromStream" />.</param>
        /// <param name="eventWatcher">The <see cref="IStreamEventWatcher" /> to wait for events to be available in stream.</param>
        /// <param name="timeToRetryGetter">The <see cref="ICanGetTimeToRetryFor{T}" /> <see cref="StreamProcessorState" />.</param>
        /// <param name="logger">An <see cref="ILogger" /> to log messages.</param>
        public ScopedStreamProcessor(
            TenantId tenantId,
            IStreamProcessorId streamProcessorId,
            IStreamDefinition sourceStreamDefinition,
            StreamProcessorState initialState,
            IEventProcessor processor,
            IResilientStreamProcessorStateRepository streamProcessorStates,
            ICanFetchEventsFromStream eventsFromStreamsFetcher,
            IAsyncPolicyFor<ICanFetchEventsFromStream> eventsFetcherPolicy,
            IStreamEventWatcher eventWatcher,
            ICanGetTimeToRetryFor<StreamProcessorState> timeToRetryGetter,
            ILogger<ScopedStreamProcessor> logger)
            : base(tenantId, streamProcessorId, sourceStreamDefinition, initialState, processor, eventsFromStreamsFetcher, eventsFetcherPolicy, eventWatcher, logger)
        {
            _streamProcessorStates = streamProcessorStates;
            _timeToRetryGetter = timeToRetryGetter;
        }

        /// <inheritdoc/>
        protected override async Task<IStreamProcessorState> Catchup(IStreamProcessorState currentState, CancellationToken cancellationToken)
        {
            var streamProcessorState = currentState as StreamProcessorState;
            Logger.LogTrace("Catchup for stream processor {streamProcessor} with state {state}", Identifier, streamProcessorState);
            while (streamProcessorState.IsFailing && !cancellationToken.IsCancellationRequested)
            {
                if (!CanRetryProcessing(streamProcessorState.RetryTime))
                {
                    await Task.Delay(GetTimeToRetryProcessing(streamProcessorState), cancellationToken).ConfigureAwait(false);
                    var tryGetStreamProcessorState = await _streamProcessorStates.TryGetFor(Identifier, cancellationToken).ConfigureAwait(false);
                    if (tryGetStreamProcessorState.Success) streamProcessorState = tryGetStreamProcessorState.Result as StreamProcessorState;
                }
                else
                {
                    var @event = await FetchNextEventToProcess(streamProcessorState, cancellationToken).ConfigureAwait(false);
                    streamProcessorState = (await RetryProcessingEvent(@event, streamProcessorState.FailureReason, streamProcessorState.ProcessingAttempts, streamProcessorState, cancellationToken).ConfigureAwait(false)) as StreamProcessorState;
                }
            }

            Logger.LogTrace("Catchup done for stream processor {streamProcessor} returning state {state}", Identifier, streamProcessorState);
            return streamProcessorState;
        }

        /// <inheritdoc/>
        protected override async Task<IStreamProcessorState> OnFailedProcessingResult(FailedProcessing failedProcessing, StreamEvent processedEvent, IStreamProcessorState currentState)
        {
            var oldState = currentState as StreamProcessorState;
            var newState = new StreamProcessorState(
                oldState.Position,
                failedProcessing.FailureReason,
                DateTimeOffset.MaxValue,
                oldState.ProcessingAttempts + 1,
                oldState.LastSuccessfullyProcessed,
                true);
            await _streamProcessorStates.Persist(Identifier, newState, CancellationToken.None).ConfigureAwait(false);
            return newState;
        }

        /// <inheritdoc/>
        protected override async Task<IStreamProcessorState> OnRetryProcessingResult(FailedProcessing failedProcessing, StreamEvent processedEvent, IStreamProcessorState currentState)
        {
            var oldState = currentState as StreamProcessorState;
            var newState = new StreamProcessorState(
                oldState.Position,
                failedProcessing.FailureReason,
                DateTimeOffset.UtcNow.Add(failedProcessing.RetryTimeout),
                oldState.ProcessingAttempts + 1,
                oldState.LastSuccessfullyProcessed,
                true);
            await _streamProcessorStates.Persist(Identifier, newState, CancellationToken.None).ConfigureAwait(false);
            return newState;
        }

        /// <inheritdoc/>
        protected override async Task<IStreamProcessorState> OnSuccessfulProcessingResult(SuccessfulProcessing successfulProcessing, StreamEvent processedEvent, IStreamProcessorState currentState)
        {
            var newState = new StreamProcessorState(processedEvent.Position + 1, DateTimeOffset.UtcNow);
            await _streamProcessorStates.Persist(Identifier, newState, CancellationToken.None).ConfigureAwait(false);
            return newState;
        }

        /// <inheritdoc/>
        protected override bool TryGetTimeToRetry(IStreamProcessorState state, out TimeSpan timeToRetry)
            => _timeToRetryGetter.TryGetTimespanToRetry(state as StreamProcessorState, out timeToRetry);

        bool CanRetryProcessing(DateTimeOffset retryTime)
            => DateTimeOffset.UtcNow.CompareTo(retryTime) >= 0;
    }
}
