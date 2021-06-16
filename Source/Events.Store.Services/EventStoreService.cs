// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Artifacts;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Lifecycle;
using Dolittle.Runtime.Rudimentary;
using Microsoft.Extensions.Logging;
using DolittleExecutionContext = Dolittle.Runtime.Execution.ExecutionContext;
using IExecutionContextManager = Dolittle.Runtime.Execution.IExecutionContextManager;

namespace Dolittle.Runtime.Events.Store.Services
{
    /// <summary>
    /// Represents the implementation of <see cref="IEventStoreService" />.
    /// </summary>
    [Singleton]
    public class EventStoreService : IEventStoreService
    {
        readonly FactoryFor<IEventStore> _eventStoreFactory;
        readonly IExecutionContextManager _executionContextManager;
        readonly IMetricsCollector _metrics;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreService"/> class.
        /// </summary>
        /// <param name="eventStoreFactory"><see cref="IEventStore"/>.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager" />.</param>
        /// <param name="metrics"><see cref="IMetricsCollector" />.</param>
        /// <param name="logger"><see cref="ILogger"/> for logging.</param>
        public EventStoreService(
            FactoryFor<IEventStore> eventStoreFactory,
            IExecutionContextManager executionContextManager,
            IMetricsCollector metrics,
            ILogger logger)
        {
            _eventStoreFactory = eventStoreFactory;
            _executionContextManager = executionContextManager;
            _metrics = metrics;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Try<CommittedEvents>> TryCommit(UncommittedEvents events, DolittleExecutionContext context, CancellationToken token)
        {
            try
            {
                _executionContextManager.CurrentFor(context);
                _logger.EventsReceivedForCommitting(false, events.Count);
                var committedEvents = await _eventStoreFactory().CommitEvents(events, token).ConfigureAwait(false);
                _logger.LogDebug("Events were successfully committed");
                _metrics.IncrementCommittedEvents(committedEvents);
                return committedEvents;
            }
            catch (Exception ex)
            {
                _metrics.IncrementFailedEvents(events);
                _logger.LogWarning(ex, "Error committing events");
                return ex;
            }
        }

        /// <inheritdoc/>
        public async Task<Try<CommittedAggregateEvents>> TryCommitForAggregate(UncommittedAggregateEvents events, DolittleExecutionContext context, CancellationToken token)
        {
            try
            {
                _executionContextManager.CurrentFor(context);
                _logger.EventsReceivedForCommitting(true, events.Count);
                var committedEvents = await _eventStoreFactory().CommitAggregateEvents(events, token).ConfigureAwait(false);
                _logger.LogDebug("Aggregate events were successfully committed");
                _metrics.IncrementCommittedAggregateEvents(committedEvents);
                return committedEvents;
            }
            catch (Exception ex)
            {
                _metrics.IncrementFailedAggregateEvents(events);
                _logger.LogWarning(ex, "Error committing aggregate events");
                return ex;
            }
        }

        /// <inheritdoc/>
        public async Task<Try<CommittedAggregateEvents>> TryFetchForAggregate(ArtifactId aggregateRoot, EventSourceId eventSource, DolittleExecutionContext context, CancellationToken token)
        {
            try
            {
                _logger.LogDebug("Fetch for aggregate");
                _executionContextManager.CurrentFor(context);
                var committedEvents = await _eventStoreFactory().FetchForAggregate(eventSource, aggregateRoot, token).ConfigureAwait(false);
                _logger.LogDebug("Successfully fetched events for aggregate");
                return committedEvents;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching events from aggregate");
                return ex;
            }
        }
    }
}
