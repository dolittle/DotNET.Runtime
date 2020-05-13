// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Events.Store.MongoDB.Events;
using Dolittle.Runtime.Events.Store.Streams;
using MongoDB.Driver;

namespace Dolittle.Runtime.Events.Store.MongoDB.Streams
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventFetchers" />.
    /// </summary>
    public class EventFetchers : IEventFetchers
    {
        readonly EventStoreConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventFetchers"/> class.
        /// </summary>
        /// <param name="connection">The <see cref="EventStoreConnection" />.</param>
        public EventFetchers(EventStoreConnection connection)
        {
            _connection = connection;
        }

        /// <inheritdoc/>
        public async Task<ICanFetchEventsFromStream> GetFetcherFor(ScopeId scopeId, IStreamDefinition streamDefinition, CancellationToken cancellationToken)
        {
            if (streamDefinition.StreamId == StreamId.EventLog)
            {
                return await CreateStreamFetcherForEventLog(scopeId, cancellationToken).ConfigureAwait(false);
            }

            if (streamDefinition.Public)
            {
                return CreateStreamFetcherForStreamEventCollection(
                    await _connection.GetPublicStreamCollection(streamDefinition.StreamId, cancellationToken).ConfigureAwait(false),
                    streamDefinition.StreamId);
            }

            return CreateStreamFetcherForStreamEventCollection(
                await _connection.GetStreamCollection(scopeId, streamDefinition.StreamId, cancellationToken).ConfigureAwait(false),
                streamDefinition.StreamId);
        }

        /// <inheritdoc/>
        public async Task<ICanFetchEventsFromPartitionedStream> GetPartitionedFetcherFor(ScopeId scopeId, IStreamDefinition streamDefinition, CancellationToken cancellationToken)
        {
            if (!streamDefinition.Partitioned) throw new CannotGetPartitionedFetcherForUnpartitionedStream(streamDefinition);
            if (streamDefinition.StreamId == StreamId.EventLog) throw new CannotGetPartitionedFetcherForEventLog();
            if (streamDefinition.Public)
            {
                return CreateStreamFetcherForStreamEventCollection(
                    await _connection.GetPublicStreamCollection(streamDefinition.StreamId, cancellationToken).ConfigureAwait(false),
                    streamDefinition.StreamId);
            }

            return CreateStreamFetcherForStreamEventCollection(
                await _connection.GetStreamCollection(scopeId, streamDefinition.StreamId, cancellationToken).ConfigureAwait(false),
                streamDefinition.StreamId);
        }

        /// <inheritdoc/>
        public async Task<ICanFetchRangeOfEventsFromStream> GetRangeFetcherFor(ScopeId scopeId, IStreamDefinition streamDefinition, CancellationToken cancellationToken)
        {
            return await GetFetcherFor(scopeId, streamDefinition, cancellationToken).ConfigureAwait(false) as ICanFetchRangeOfEventsFromStream;
        }

        /// <inheritdoc/>
        public async Task<ICanFetchEventTypesFromStream> GetTypeFetcherFor(ScopeId scopeId, IStreamDefinition streamDefinition, CancellationToken cancellationToken)
        {
            return await GetFetcherFor(scopeId, streamDefinition, cancellationToken).ConfigureAwait(false) as ICanFetchEventTypesFromStream;
        }

        /// <inheritdoc/>
        public async Task<ICanFetchEventTypesFromPartitionedStream> GetPartitionedTypeFetcherFor(ScopeId scopeId, IStreamDefinition streamDefinition, CancellationToken cancellationToken)
        {
            return await GetPartitionedFetcherFor(scopeId, streamDefinition, cancellationToken).ConfigureAwait(false) as ICanFetchEventTypesFromPartitionedStream;
        }

        async Task<StreamFetcher<MongoDB.Events.Event>> CreateStreamFetcherForEventLog(ScopeId scopeId, CancellationToken cancellationToken) =>
            new StreamFetcher<MongoDB.Events.Event>(
                await _connection.GetEventLogCollection(scopeId, cancellationToken).ConfigureAwait(false),
                Builders<MongoDB.Events.Event>.Filter,
                _ => _.EventLogSequenceNumber,
                Builders<MongoDB.Events.Event>.Projection.Expression(_ => _.ToRuntimeStreamEvent()),
                Builders<MongoDB.Events.Event>.Projection.Expression(_ => new Artifacts.Artifact(_.Metadata.TypeId, _.Metadata.TypeGeneration)));

        StreamFetcher<MongoDB.Events.StreamEvent> CreateStreamFetcherForStreamEventCollection(IMongoCollection<MongoDB.Events.StreamEvent> collection, StreamId streamId) =>
            new StreamFetcher<MongoDB.Events.StreamEvent>(
                collection,
                Builders<MongoDB.Events.StreamEvent>.Filter,
                _ => _.StreamPosition,
                Builders<MongoDB.Events.StreamEvent>.Projection.Expression(_ => _.ToRuntimeStreamEvent(streamId)),
                Builders<MongoDB.Events.StreamEvent>.Projection.Expression(_ => new Artifacts.Artifact(_.Metadata.TypeId, _.Metadata.TypeGeneration)),
                _ => _.Partition);
    }
}
