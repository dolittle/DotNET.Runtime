// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Dolittle.Logging;

namespace Dolittle.Runtime.Events.Processing
{
    /// <summary>
    /// Represents a system that processes a stream of events.
    /// </summary>
    public class StreamProcessor
    {
        readonly IEventProcessor _processor;
        readonly ILogger _logger;
        readonly IFetchEventsFromStreams _eventsFromStreamsFetcher;
        readonly IStreamProcessorStateRepository _streamProcessorStateRepository;
        bool _hasStarted = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamProcessor"/> class.
        /// </summary>
        /// <param name="sourceStreamId">The <see cref="StreamId" /> of the source stream.</param>
        /// <param name="processor">An <see cref="IEventProcessor" /> to process the event.</param>
        /// <param name="streamProcessorStateRepository">The <see cref="IStreamProcessorStateRepository" />.</param>
        /// <param name="eventsFromStreamsFetcher">The<see cref="IFetchEventsFromStreams" />.</param>
        /// <param name="logger">An <see cref="ILogger" /> to log messages.</param>
        public StreamProcessor(
            StreamId sourceStreamId,
            IEventProcessor processor,
            IStreamProcessorStateRepository streamProcessorStateRepository,
            IFetchEventsFromStreams eventsFromStreamsFetcher,
            ILogger logger)
        {
            _processor = processor;
            _logger = logger;
            _eventsFromStreamsFetcher = eventsFromStreamsFetcher;
            _streamProcessorStateRepository = streamProcessorStateRepository;
            Identifier = new StreamProcessorId(_processor.Identifier, sourceStreamId);
            LogMessageBeginning = $"Stream Partition Processor for event processor '{Identifier.EventProcessorId.Value}' with source stream '{Identifier.SourceStreamId.Value}'";
        }

        /// <summary>
        /// Gets the <see cref="StreamProcessorId">identifier</see> for the <see cref="StreamProcessor"/>.
        /// </summary>
        public StreamProcessorId Identifier { get; }

        /// <summary>
        /// Gets the current <see cref="StreamProcessorState" />.
        /// </summary>
        public StreamProcessorState CurrentState { get; private set; } = StreamProcessorState.New;

        string LogMessageBeginning { get; }

        /// <summary>
        /// Starts up the <see cref="StreamProcessor" />.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task BeginProcessing()
        {
            try
            {
                if (_hasStarted) return;
                _hasStarted = true;
                CurrentState = await GetPersistedCurrentState().ConfigureAwait(false);
                while (true)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    await CatchupFailingPartitions().ConfigureAwait(false);

                    // TODO: Handle timeout
                    var eventAndPartition = await FetchNextEventWithPartitionToProcess().ConfigureAwait(false);

                    if (CurrentState.FailingPartitions.Keys.Contains(eventAndPartition.PartitionId))
                    {
                        CurrentState = await IncrementPosition().ConfigureAwait(false);
                    }
                    else
                    {
                        var processingResult = await ProcessEvent(eventAndPartition.Event, eventAndPartition.PartitionId).ConfigureAwait(false);
                        if (processingResult.Succeeded)
                        {
                            CurrentState = await IncrementPosition().ConfigureAwait(false);
                        }
                        else if (processingResult is IRetryProcessingResult retryProcessingResult)
                        {
                            CurrentState = await AddFailingPartition(eventAndPartition.PartitionId, retryProcessingResult.RetryTimeout).ConfigureAwait(false);
                        }
                        else
                        {
                            CurrentState = await AddFailingPartition(eventAndPartition.PartitionId, DateTimeOffset.MaxValue).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogMessageBeginning} failed - {ex}");
                throw;
            }
        }

        async Task CatchupFailingPartitions()
        {
            foreach (var kvp in CurrentState.FailingPartitions.AsEnumerable())
            {
                var partitionId = kvp.Key;
                var failingPartitionState = kvp.Value;
                if (ShouldRetryProcessing(failingPartitionState))
                {
                    var nextPosition = await FindPositionOfNextEventInPartition(partitionId, failingPartitionState.Position).ConfigureAwait(false);
                    while (ShouldProcessNextEventInPartition(nextPosition))
                    {
                        if (!ShouldRetryProcessing(failingPartitionState)) break;

                        var eventAndPartition = await FetchEventWithPartitionAtPosition(nextPosition).ConfigureAwait(false);
                        var processingResult = await ProcessEvent(eventAndPartition.Event, eventAndPartition.PartitionId).ConfigureAwait(false);

                        if (processingResult.Succeeded)
                        {
                            failingPartitionState = await ChangePositionInFailingPartition(partitionId, failingPartitionState.Position, nextPosition.Increment()).ConfigureAwait(false);
                        }
                        else if (processingResult is IRetryProcessingResult retryProcessingResult)
                        {
                            failingPartitionState = await SetFailingPartitionState(partitionId, retryProcessingResult.RetryTimeout, nextPosition).ConfigureAwait(false);
                        }
                        else
                        {
                            failingPartitionState = await SetFailingPartitionState(partitionId, DateTimeOffset.MaxValue, nextPosition).ConfigureAwait(false);
                        }

                        nextPosition = await FindPositionOfNextEventInPartition(partitionId, failingPartitionState.Position).ConfigureAwait(false);
                    }

                    if (ShouldRetryProcessing(failingPartitionState)) CurrentState = await RemoveFailingPartition(partitionId).ConfigureAwait(false);
                }
            }
        }

        Task<StreamProcessorState> AddFailingPartition(PartitionId partitionId, uint retryTimeout) => AddFailingPartition(partitionId, DateTimeOffset.UtcNow.AddMilliseconds(retryTimeout));

        async Task<StreamProcessorState> AddFailingPartition(PartitionId partitionId, DateTimeOffset retryTime)
        {
            _logger.Debug($"{LogMessageBeginning} is adding failing partition '{partitionId}' with retry time '{retryTime}'");
            var streamProcessorState = await _streamProcessorStateRepository.AddFailingPartition(Identifier, CurrentState, partitionId, retryTime).ConfigureAwait(false);
            return new StreamProcessorState(streamProcessorState.Position.Increment(), streamProcessorState.FailingPartitions);
        }

        Task<StreamProcessorState> RemoveFailingPartition(PartitionId partitionId)
        {
            _logger.Debug($"{LogMessageBeginning} is removing failing partition '{partitionId}'");
            return _streamProcessorStateRepository.RemoveFailingPartition(Identifier, partitionId);
        }

        Task<StreamProcessorState> GetPersistedCurrentState()
        {
            _logger.Debug($"{LogMessageBeginning} is getting the persisted state for this stream processor.");
            return _streamProcessorStateRepository.Get(Identifier);
        }

        Task<IProcessingResult> ProcessEvent(CommittedEvent @event, PartitionId partitionId)
        {
            _logger.Debug($"{LogMessageBeginning} is processing event '{@event.Metadata.Artifact.Id.Value}' in partition '{partitionId.Value}'");
            return _processor.Process(@event, partitionId);
        }

        Task<StreamProcessorState> IncrementPosition()
        {
            _logger.Debug($"{LogMessageBeginning} is incrementing its position from '{CurrentState.Position.Value}' to '{CurrentState.Position.Increment().Value}'");
            return _streamProcessorStateRepository.IncrementPosition(Identifier);
        }

        async Task<FailingPartitionState> ChangePositionInFailingPartition(PartitionId partitionId, StreamPosition oldPosition, StreamPosition newPosition)
        {
            _logger.Debug($"{LogMessageBeginning} is chaning its position from '{oldPosition.Value}' to '{newPosition.Value}' in partition'{partitionId.Value}'");
            var newFailingPartitionState = new FailingPartitionState { Position = newPosition, RetryTime = DateTimeOffset.MinValue };
            CurrentState = await _streamProcessorStateRepository.SetFailingPartitionState(
                Identifier,
                partitionId,
                newFailingPartitionState).ConfigureAwait(false);
            return newFailingPartitionState;
        }

        Task<CommittedEventWithPartition> FetchNextEventWithPartitionToProcess() => FetchEventWithPartitionAtPosition(CurrentState.Position);

        Task<CommittedEventWithPartition> FetchEventWithPartitionAtPosition(StreamPosition position)
        {
            _logger.Debug($"{LogMessageBeginning} is fetching event at position '{position.Value}'.");
            return _eventsFromStreamsFetcher.Fetch(Identifier.SourceStreamId, position);
        }

        Task<StreamPosition> FindPositionOfNextEventInPartition(PartitionId partitionId, StreamPosition fromPosition)
        {
            _logger.Debug($"{LogMessageBeginning} is fetching next event to process in partition '{partitionId}' from position '{fromPosition}'.");
            return _eventsFromStreamsFetcher.FindNext(Identifier.SourceStreamId, partitionId, fromPosition);
        }

        Task<FailingPartitionState> SetFailingPartitionState(PartitionId partitionId, uint retryTimeout, StreamPosition position) => SetFailingPartitionState(partitionId, DateTimeOffset.UtcNow.AddMilliseconds(retryTimeout), position);

        async Task<FailingPartitionState> SetFailingPartitionState(PartitionId partitionId, DateTimeOffset retryTime, StreamPosition position)
        {
            _logger.Debug($"{LogMessageBeginning} is setting retry time '{retryTime}' and position '{position.Value}' for partition '{partitionId.Value}'");
            var newFailingPartitionState = new FailingPartitionState { Position = position, RetryTime = retryTime };
            CurrentState = await _streamProcessorStateRepository.SetFailingPartitionState(
                Identifier,
                partitionId,
                newFailingPartitionState)
                .ConfigureAwait(false);

            return newFailingPartitionState;
        }

        bool ShouldProcessNextEventInPartition(StreamPosition position) => position.Value < CurrentState.Position.Value;

        bool ShouldRetryProcessing(FailingPartitionState state) => DateTimeOffset.Now.CompareTo(state.RetryTime) >= 0;
    }
}