// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dolittle.Runtime.Events.Store
{
    /// <summary>
    /// Defines a system for collecting metrics about event store.
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Increment number of failed uncommitted events based on an event for labelling.
        /// </summary>
        /// <param name="events">The <see cref="UncommittedEvents"/> used for labelling.</param>
        void IncrementFailedEvents(UncommittedEvents events);

        /// <summary>
        /// Increment number of failed uncommitted aggregate events based on an event for labelling.
        /// </summary>
        /// <param name="events">The <see cref="UncommittedAggregateEvents"/> with all events and context used for labelling.</param>
        void IncrementFailedAggregateEvents(UncommittedAggregateEvents events);

        /// <summary>
        /// Increment number of committed events based on an event for labelling.
        /// </summary>
        /// <param name="event">The <see cref="CommittedEvents"/> used for labelling.</param>
        void IncrementCommittedEvents(CommittedEvents events);

        /// <summary>
        /// Increment number of committed aggregate events and committed events based on an
        /// event for labelling.
        /// </summary>
        /// <param name="events">The <see cref="CommittedAggregateEvents"/> used for labelling.</param>
        void IncrementCommittedAggregateEvents(CommittedAggregateEvents events);
    }
}
