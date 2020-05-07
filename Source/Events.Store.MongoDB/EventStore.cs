// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Artifacts;
using Dolittle.Execution;
using Dolittle.Logging;
using Dolittle.Runtime.Events.Store.MongoDB.Aggregates;
using Dolittle.Runtime.Events.Store.MongoDB.Events;
using MongoDB.Driver;

namespace Dolittle.Runtime.Events.Store.MongoDB
{
    /// <summary>
    /// Represents the MongoDB implementation of <see cref="IEventStore"/>.
    /// </summary>
    public class EventStore : IEventStore
    {
        readonly FilterDefinitionBuilder<MongoDB.Events.Event> _eventFilter = Builders<MongoDB.Events.Event>.Filter;
        readonly IExecutionContextManager _executionContextManager;
        readonly EventStoreConnection _connection;
        readonly IEventCommitter _eventCommitter;
        readonly IAggregateRoots _aggregateRoots;
        readonly IMetrics _metrics;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStore"/> class.
        /// </summary>
        /// <param name="executionContextManager">The <see cref="IExecutionContextManager" />.</param>
        /// <param name="connection">An <see cref="EventStoreConnection"/> to a MongoDB EventStore.</param>
        /// <param name="eventCommitter">The <see cref="IEventCommitter" />.</param>
        /// <param name="aggregateRoots">The <see cref="IAggregateRoots" />.</param>
        /// <param name="metrics"><see cref="IMetrics"/> for event store.</param>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        public EventStore(
            IExecutionContextManager executionContextManager,
            EventStoreConnection connection,
            IEventCommitter eventCommitter,
            IAggregateRoots aggregateRoots,
            IMetrics metrics,
            ILogger logger)
        {
            _executionContextManager = executionContextManager;
            _connection = connection;
            _eventCommitter = eventCommitter;
            _aggregateRoots = aggregateRoots;
            _metrics = metrics;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<CommittedEvents> CommitEvents(UncommittedEvents events, CancellationToken cancellationToken)
        {
            ThrowIfNoEventsToCommit(events);
            try
            {
                using var session = await _connection.MongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return await session.WithTransactionAsync(
                    async (transaction, cancel) =>
                    {
                        var eventLogSequenceNumber = (ulong)await _connection.EventLog.CountDocumentsAsync(
                            transaction,
                            _eventFilter.Empty,
                            cancellationToken: cancel).ConfigureAwait(false);
                        var committedEvents = new List<CommittedEvent>();
                        foreach (var @event in events)
                        {
                            _metrics.IncrementUncommittedEvents(@event);

                            var committedEvent = await _eventCommitter.CommitEvent(
                                transaction,
                                eventLogSequenceNumber,
                                DateTimeOffset.UtcNow,
                                _executionContextManager.Current,
                                @event,
                                cancel).ConfigureAwait(false);
                            committedEvents.Add(committedEvent);
                            eventLogSequenceNumber++;

                            _metrics.IncrementCommittedEvents(committedEvent);
                        }

                        return new CommittedEvents(committedEvents);
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWaitQueueFullException ex)
            {
                _metrics.IncrementFailedUncommittedEvents(events);
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<CommittedAggregateEvents> CommitAggregateEvents(UncommittedAggregateEvents events, CancellationToken cancellationToken)
        {
            ThrowIfNoEventsToCommit(events);
            try
            {
                using var session = await _connection.MongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return await session.WithTransactionAsync(
                    async (transaction, cancel) =>
                    {
                        var eventLogSequenceNumber = (ulong)await _connection.EventLog.CountDocumentsAsync(
                            transaction,
                            _eventFilter.Empty,
                            cancellationToken: cancel).ConfigureAwait(false);
                        var aggregateRootVersion = events.ExpectedAggregateRootVersion.Value;

                        var committedEvents = new List<CommittedAggregateEvent>();

                        _metrics.IncrementUncommittedAggregateEvents(events);

                        foreach (var @event in events)
                        {
                            var committedEvent = await _eventCommitter.CommitAggregateEvent(
                                transaction,
                                events.AggregateRoot,
                                aggregateRootVersion,
                                eventLogSequenceNumber,
                                DateTimeOffset.UtcNow,
                                events.EventSource,
                                _executionContextManager.Current,
                                @event,
                                cancel).ConfigureAwait(false);
                            committedEvents.Add(committedEvent);
                            eventLogSequenceNumber++;
                            aggregateRootVersion++;

                            _metrics.IncrementCommittedAggregateEvents(committedEvent);
                        }

                        await _aggregateRoots.IncrementVersionFor(
                            transaction,
                            events.EventSource,
                            events.AggregateRoot.Id,
                            events.ExpectedAggregateRootVersion,
                            aggregateRootVersion,
                            cancel).ConfigureAwait(false);

                        return new CommittedAggregateEvents(events.EventSource, events.AggregateRoot.Id, committedEvents);
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWaitQueueFullException ex)
            {
                _metrics.IncrementFailedUncommittedAggregateEvents(events);
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<CommittedAggregateEvents> FetchForAggregate(EventSourceId eventSource, ArtifactId aggregateRoot, CancellationToken cancellationToken)
        {
            try
            {
                using var session = await _connection.MongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return await session.WithTransactionAsync(
                    async (transaction, cancel) =>
                    {
                        var version = await _aggregateRoots.FetchVersionFor(
                        transaction,
                        eventSource,
                        aggregateRoot,
                        cancel).ConfigureAwait(false);
                        if (version > AggregateRootVersion.Initial)
                        {
                            var filter = _eventFilter.Eq(_ => _.Aggregate.WasAppliedByAggregate, true)
                                & _eventFilter.Eq(_ => _.Metadata.EventSource, eventSource.Value)
                                & _eventFilter.Eq(_ => _.Aggregate.TypeId, aggregateRoot.Value)
                                & _eventFilter.Lte(_ => _.Aggregate.Version, version.Value);
                            var events = await _connection.EventLog
                                .Find(transaction, filter)
                                .Sort(Builders<MongoDB.Events.Event>.Sort.Ascending(_ => _.Aggregate.Version))
                                .Project(_ => _.ToCommittedAggregateEvent())
                                .ToListAsync(cancel).ConfigureAwait(false);

                            return new CommittedAggregateEvents(
                                eventSource,
                                aggregateRoot,
                                events);
                        }
                        else
                        {
                            return new CommittedAggregateEvents(
                                eventSource,
                                aggregateRoot,
                                Array.Empty<CommittedAggregateEvent>());
                        }
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWaitQueueFullException ex)
            {
                throw new EventStoreUnavailable("Mongo wait queue is full", ex);
            }
        }

        void ThrowIfNoEventsToCommit(UncommittedEvents events)
        {
            if (!events.HasEvents) throw new NoEventsToCommit();
        }
    }
}