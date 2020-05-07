// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Prometheus;

namespace Dolittle.Runtime.Events.Store
{
    /// <summary>
    /// Defines the metrics related to the <see cref="IEventStore"/>.
    /// </summary>
    public interface IMetrics
    {
        /// <summary>
        /// Gets the counter used for counting number of committed events.
        /// </summary>
        Counter CommittedEvents { get; }

        /// <summary>
        /// Gets the counter used for counting number of committed aggregate events.
        /// </summary>
        Counter CommittedAggregateEvents { get; }

        /// <summary>
        /// Gets the counter used for counting number of uncommitted events.
        /// </summary>
        Counter UncommittedEvents { get; }

        /// <summary>
        /// Gets the counter used for counting number of uncommitted aggregate events.
        /// </summary>
        Counter UncommittedAggregateEvents { get; }

        /// <summary>
        /// Gets the counter used for counting number of failed committed events.
        /// </summary>
        Counter FailedUncommittedEvents { get; }

        /// <summary>
        /// Gets the counter used for counting number of failed committed aggregate events.
        /// </summary>
        Counter FailedUncommittedAggregateEvents { get; }

        /// <summary>
        /// Increment number of uncommitted events based on an event for labelling.
        /// </summary>
        /// <param name="event">The <see cref="UncommittedEvent"/> used for labelling.</param>
        void IncrementUncommittedEvents(UncommittedEvent @event);

        /// <summary>
        /// Increment number of failed uncommitted events based on an event for labelling.
        /// </summary>
        /// <param name="events">The <see cref="UncommittedEvents"/> used for labelling.</param>
        void IncrementFailedUncommittedEvents(UncommittedEvents events);

        /// <summary>
        /// Increment number of uncommitted events based on an event for labelling.
        /// </summary>
        /// <param name="events">The <see cref="UncommittedAggregateEvents"/> with all events and context used for labelling.</param>
        void IncrementUncommittedAggregateEvents(UncommittedAggregateEvents events);

        /// <summary>
        /// Increment number of failed uncommitted aggregate events based on an event for labelling.
        /// </summary>
        /// <param name="events">The <see cref="UncommittedAggregateEvents"/> with all events and context used for labelling.</param>
        void IncrementFailedUncommittedAggregateEvents(UncommittedAggregateEvents events);

        /// <summary>
        /// Increment number of committed events based on an event for labelling.
        /// </summary>
        /// <param name="event">The <see cref="CommittedEvent"/> used for labelling.</param>
        void IncrementCommittedEvents(CommittedEvent @event);

        /// <summary>
        /// Increment number of committed aggregate events based on an event for labelling.
        /// </summary>
        /// <param name="event">The <see cref="CommittedAggregateEvent"/> used for labelling.</param>
        void IncrementCommittedAggregateEvents(CommittedAggregateEvent @event);
    }
}