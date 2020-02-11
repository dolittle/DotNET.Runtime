﻿// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dolittle.Applications;
using Dolittle.Artifacts;
using Dolittle.Execution;
using Dolittle.Tenancy;

namespace Dolittle.Runtime.Events.Store
{
    /// <summary>
    /// Represent an Event that was applied to an Event Source by an <see cref="AggregateRoot"/> and is committed to the Event Store.
    /// </summary>
    public class CommittedAggregateEvent : CommittedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommittedAggregateEvent"/> class.
        /// </summary>
        /// <param name="eventSource">The Event Source that the Event was applied to.</param>
        /// <param name="aggregateRoot">The <see cref="Artifact"/> representing the type of the Aggregate Root that applied the Event to the Event Source.</param>
        /// <param name="aggregateRootVersion">The version of the <see cref="AggregateRoot"/> that applied the Event.</param>
        /// <param name="eventLogVersion">The version of the Event Log the Event was committed to.</param>
        /// <param name="occurred">The <see cref="DateTimeOffset" /> when the Event was committed to the Event Store.</param>
        /// <param name="correlationId">The <see cref="CorrelationId" /> to relate this event to other artifacts and actions within the system.</param>
        /// <param name="microservice">The <see cref="Microservice"/> within which the Event occurred.</param>
        /// <param name="tenant">The <see cref="TenantId"/> within which the Event occurred.</param>
        /// <param name="cause">The link to the cause of the Event.</param>
        /// <param name="type">The <see cref="Artifact"/> representing the type of the Event.</param>
        /// <param name="content">The content of the Event represented as a JSON-encoded <see cref="string"/>.</param>
        public CommittedAggregateEvent(EventSourceId eventSource, Artifact aggregateRoot, AggregateRootVersion aggregateRootVersion, EventLogVersion eventLogVersion, DateTimeOffset occurred, CorrelationId correlationId, Microservice microservice, TenantId tenant, Cause cause, Artifact type, string content)
            : base(eventLogVersion, occurred, correlationId, microservice, tenant, cause, type, content)
        {
            EventSource = eventSource;
            AggregateRoot = aggregateRoot;
            AggregateRootVersion = aggregateRootVersion;
        }

        /// <summary>
        /// Gets the Event Source that the Event was applied to.
        /// </summary>
        public EventSourceId EventSource { get; }

        /// <summary>
        /// Gets the <see cref="Artifact"/> representing the type of the Aggregate Root that applied the Event to the Event Source.
        /// </summary>
        public Artifact AggregateRoot { get; }

        /// <summary>
        /// Gets the version of the <see cref="AggregateRoot"/> that applied the Event.
        /// </summary>
        public AggregateRootVersion AggregateRootVersion { get; }
    }
}