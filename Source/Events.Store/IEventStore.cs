// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Dolittle.Runtime.Events.Store
{
    /// <summary>
    /// Defines the functionality required for an EventStore implementation.
    /// </summary>
    public interface IEventStore : ICommitEvents, IFetchCommittedEvents
    {
        /// <summary>
        /// Get the <see cref="EventLogSequenceNumber"/> for the last committed event.
        /// </summary>
        /// <returns><see cref="EventLogSequenceNumber"/> for the event at the tail.</returns>
        Task<EventLogSequenceNumber> GetLastCommittedEventSequenceNumber();
    }
}