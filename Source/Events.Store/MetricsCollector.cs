// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Dolittle.Runtime.Collections;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Lifecycle;
using Dolittle.Runtime.Metrics;
using Prometheus;
using IMetricFactory = Dolittle.Runtime.Metrics.IMetricFactory;

namespace Dolittle.Runtime.Events.Store
{
    /// <summary>
    /// Represents an implementation of <see cref="IMetricsCollector"/>.
    /// </summary>
    [Singleton]
    public class MetricsCollector : ICanProvideMetrics, IMetricsCollector
    {
        readonly IExecutionContextManager _executionContextManager;

        Counter _totalFailedEvents;
        Counter _totalFailedAggregateEvents;
        Counter _totalCommittedEvents;
        Counter _totalCommittedAggregateEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working the <see cref="ExecutionContext"/>.</param>
        public MetricsCollector(IExecutionContextManager executionContextManager)
        {
            _executionContextManager = executionContextManager;
        }

        /// <inheritdoc/>
        public IEnumerable<Collector> Provide(IMetricFactory metricFactory)
        {
            _totalFailedEvents = metricFactory.Counter(
                "dolittle_shared_runtime_events_store_failed_events_total",
                "EventStore total number of failed uncommitted events",
                "TenantId",
                "EventSourceId",
                "EventTypeId");
            _totalFailedAggregateEvents = metricFactory.Counter(
                "dolittle_shared_runtime_events_store_failed_aggregate_events_total",
                "EventStore total number of failed uncommitted aggregate events",
                "TenantId",
                "EventSourceId",
                "AggregateRootId",
                "EventTypeId");
            _totalCommittedEvents = metricFactory.Counter(
                "dolittle_shared_runtime_events_store_committed_events_total",
                "EventStore total number of committed events",
                "TenantId",
                "EventSourceId",
                "EventTypeId");
            _totalCommittedAggregateEvents = metricFactory.Counter(
                "dolittle_shared_runtime_events_store_committed_aggregate_events_total",
                "EventStore total number of committed aggregate events",
                "TenantId",
                "EventSourceId",
                "AggregateRootId",
                "EventTypeId");

            return new Collector[]
            {
                _totalFailedEvents,
                _totalFailedAggregateEvents,
                _totalCommittedEvents,
                _totalCommittedAggregateEvents
            };
        }

        /// <inheritdoc/>
        public void IncrementCommittedEvents(CommittedEvents events)
        {
            events.ForEach(@event =>
            {
                _totalCommittedEvents.WithLabels(
                    @event.ExecutionContext.Tenant.Value.ToString(),
                    @event.EventSource.Value.ToString(),
                    @event.Type.Id.Value.ToString()).Inc();
            });
        }

        /// <inheritdoc/>
        public void IncrementFailedEvents(UncommittedEvents events)
        {
            events.ForEach(@event =>
            {
                _totalFailedEvents.WithLabels(
                    _executionContextManager.Current.Tenant.Value.ToString(),
                    @event.Type.Id.Value.ToString()).Inc();
            });
        }

        /// <inheritdoc/>
        public void IncrementCommittedAggregateEvents(CommittedAggregateEvents events)
        {
            events.ForEach(@event =>
            {
                _totalCommittedAggregateEvents.WithLabels(
                    @event.ExecutionContext.Tenant.Value.ToString(),
                    @event.EventSource.Value.ToString(),
                    @event.AggregateRoot.Id.Value.ToString(),
                    @event.Type.Id.Value.ToString()).Inc();
                _totalCommittedEvents.WithLabels(
                    @event.ExecutionContext.Tenant.Value.ToString(),
                    @event.EventSource.Value.ToString(),
                    @event.Type.Id.Value.ToString()).Inc();
            });
        }

        /// <inheritdoc/>
        public void IncrementFailedAggregateEvents(UncommittedAggregateEvents events)
        {
            events.ForEach(@event =>
            {
                _totalFailedAggregateEvents.WithLabels(
                    _executionContextManager.Current.Tenant.Value.ToString(),
                    events.EventSource.Value.ToString(),
                    events.AggregateRoot.Id.Value.ToString(),
                    @event.Type.Id.Value.ToString()).Inc();
            });
        }
    }
}
