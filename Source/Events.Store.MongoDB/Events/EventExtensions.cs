// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.Artifacts;
using Dolittle.Runtime.Events.Processing;
using MongoDB.Bson;

namespace Dolittle.Runtime.Events.Store.MongoDB.Events
{
    /// <summary>
    /// Extension methods for <see cref="Event" />.
    /// </summary>
    public static class EventExtensions
    {
        /// <summary>
        /// Converts a <see cref="Event" /> to <see cref="CommittedAggregateEvent" />.
        /// </summary>
        /// <param name="event">The <see cref="Event" />.</param>
        /// <returns>The converted <see cref="CommittedAggregateEvent" />.</returns>
        public static CommittedAggregateEvent ToCommittedAggregateEvent(this Event @event) =>
            new CommittedAggregateEvent(
                new Artifact(@event.Aggregate.TypeId, @event.Aggregate.TypeGeneration),
                @event.Aggregate.Version,
                @event.EventLogVersion,
                @event.Metadata.Occurred,
                @event.Metadata.EventSource,
                @event.Metadata.Correlation,
                @event.Metadata.Microservice,
                @event.Metadata.Tenant,
                new Cause(@event.Metadata.CauseType, @event.Metadata.CausePosition),
                new Artifact(@event.Metadata.TypeId, @event.Metadata.TypeGeneration),
                @event.Content.ToString());

        /// <summary>
        /// Converts a <see cref="Event" /> to <see cref="CommittedEvent" />.
        /// </summary>
        /// <param name="event">The <see cref="Event" />.</param>
        /// <returns>The converted <see cref="CommittedEvent" />.</returns>
        public static CommittedEvent ToCommittedEvent(this Event @event) =>
            @event.Aggregate.WasAppliedByAggregate ?
                @event.ToCommittedAggregateEvent()
                : new CommittedEvent(
                      @event.EventLogVersion,
                      @event.Metadata.Occurred,
                      @event.Metadata.EventSource,
                      @event.Metadata.Correlation,
                      @event.Metadata.Microservice,
                      @event.Metadata.Tenant,
                      new Cause(@event.Metadata.CauseType, @event.Metadata.CausePosition),
                      new Artifact(@event.Metadata.TypeId, @event.Metadata.TypeGeneration),
                      @event.Content.ToString());

        /// <summary>
        /// Converts a <see cref="Event" /> to <see cref="CommittedEventWithPartition" />.
        /// </summary>
        /// <param name="event">The <see cref="Event" />.</param>
        /// <param name="partition">The <see cref="PartitionId" />.</param>
        /// <returns>The converted <see cref="CommittedAggregateEvent" />.</returns>
        public static CommittedEventWithPartition ToCommittedEventWithPartition(this Event @event, PartitionId partition) =>
            new CommittedEventWithPartition(@event.ToCommittedEvent(), partition);

        /// <summary>
        /// Converts a <see cref="CommittedEvent" /> to <see cref="Event" />.
        /// </summary>
        /// <param name="committedEvent">The <see cref="CommittedEvent" />.</param>
        /// <param name="streamPosition">The <see cref="StreamPosition" />.</param>
        /// <param name="partition">The <see cref="PartitionId" />.</param>
        /// <returns>The converted <see cref="Event" />.</returns>
        public static Event ToStoreRepresentation(this CommittedEvent committedEvent, StreamPosition streamPosition, PartitionId partition) =>
            new Event(
                streamPosition,
                committedEvent.EventLogVersion,
                partition,
                committedEvent.GetEventMetadata(),
                committedEvent.GetAggregateMetadata(),
                BsonDocument.Parse(committedEvent.Content));
    }
}