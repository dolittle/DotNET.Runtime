// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Artifacts;
using Dolittle.Runtime.Rudimentary;
using Dolittle.Runtime.Events.Store.Streams;
using MongoDB.Driver;

namespace Dolittle.Runtime.Events.Store.MongoDB.Streams
{
    /// <summary>
    /// Represents a fetcher.
    /// </summary>
    /// <typeparam name="TEvent">The type of the stored event.</typeparam>
    public class StreamFetcher<TEvent> : ICanFetchEventsFromStream, ICanFetchEventsFromPartitionedStream, ICanFetchRangeOfEventsFromStream, ICanFetchEventTypesFromStream, ICanFetchEventTypesFromPartitionedStream
        where TEvent : class
    {
        readonly IMongoCollection<TEvent> _stream;
        readonly FilterDefinitionBuilder<TEvent> _filter;
        readonly Expression<Func<TEvent, Guid>> _partitionIdExpression;
        readonly Expression<Func<TEvent, ulong>> _sequenceNumberExpression;
        readonly ProjectionDefinition<TEvent, StreamEvent> _eventToStreamEvent;
        readonly ProjectionDefinition<TEvent, Artifact> _eventToArtifact;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFetcher{T}"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="IMongoCollection{TDocument}" />.</param>
        /// <param name="filter">The <see cref="FilterDefinitionBuilder{TDocument}" />.</param>
        /// <param name="sequenceNumberExpression">The <see cref="Expression{T}" /> for getting the sequence number from the stored event.</param>
        /// <param name="eventToStreamEvent">The <see cref="ProjectionDefinition{TSource, TProjection}" /> for projecting the stored event to a <see cref="StreamEvent" />.</param>
        /// <param name="eventToArtifact">The <see cref="ProjectionDefinition{TSource, TProjection}" /> for projecting the stored event to <see cref="Artifact" />.</param>
        /// <param name="partitionIdExpression">The <see cref="Expression{T}" /> for getting the <see cref="Guid" /> for the Partition Id from the stored event.</param>
        public StreamFetcher(
            IMongoCollection<TEvent> stream,
            FilterDefinitionBuilder<TEvent> filter,
            Expression<Func<TEvent, ulong>> sequenceNumberExpression,
            ProjectionDefinition<TEvent, StreamEvent> eventToStreamEvent,
            ProjectionDefinition<TEvent, Artifact> eventToArtifact,
            Expression<Func<TEvent, Guid>> partitionIdExpression = default)
        {
            _stream = stream;
            _filter = filter;
            _sequenceNumberExpression = sequenceNumberExpression;
            _eventToStreamEvent = eventToStreamEvent;
            _eventToArtifact = eventToArtifact;
            _partitionIdExpression = partitionIdExpression;
        }

        /// <inheritdoc/>
        public async Task<Try<StreamEvent>> Fetch(
            StreamPosition streamPosition,
            CancellationToken cancellationToken)
        {
            try
            {
                var @event = await _stream.Find(
                    _filter.Eq(_sequenceNumberExpression, streamPosition.Value))
                    .Project(_eventToStreamEvent)
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                return @event ?? Try<StreamEvent>.Failed();
            }
            catch (MongoWaitQueueFullException ex)
            {
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Try<StreamEvent>> FetchInPartition(PartitionId partitionId, StreamPosition streamPosition, CancellationToken cancellationToken)
        {
            try
            {
                var @event = await _stream.Find(
                    _filter.Eq(_partitionIdExpression, partitionId.Value)
                        & _filter.Gte(_sequenceNumberExpression, streamPosition.Value))
                    .Limit(1)
                    .Project(_eventToStreamEvent)
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                return @event ?? Try<StreamEvent>.Failed();
            }
            catch (MongoWaitQueueFullException ex)
            {
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<StreamEvent>> FetchRange(
            StreamPositionRange range,
            CancellationToken cancellationToken)
        {
            try
            {
                var maxNumEvents = range.Length;
                var events = await _stream.Find(
                        _filter.Gte(_sequenceNumberExpression, range.From.Value)
                            & _filter.Lt(_sequenceNumberExpression, range.From.Value + range.Length))
                    .Project(_eventToStreamEvent)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                return events.ToArray();
            }
            catch (MongoWaitQueueFullException ex)
            {
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Artifact>> FetchInRange(
            StreamPositionRange range,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _stream
                    .Find(_filter.Gte(_sequenceNumberExpression, range.From.Value)
                        & _filter.Lt(_sequenceNumberExpression, range.From.Value + range.Length))
                    .Project(_eventToArtifact)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWaitQueueFullException ex)
            {
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Artifact>> FetchInRangeAndPartition(PartitionId partitionId, StreamPositionRange range, CancellationToken cancellationToken)
        {
            try
            {
                return await _stream
                    .Find(_filter.Eq(_partitionIdExpression, partitionId.Value)
                        & _filter.Gte(_sequenceNumberExpression, range.From.Value)
                        & _filter.Lt(_sequenceNumberExpression, range.From.Value + range.Length))
                    .Project(_eventToArtifact)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWaitQueueFullException ex)
            {
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }
    }
}
