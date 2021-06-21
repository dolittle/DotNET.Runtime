// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dolittle.Runtime.Management.GraphQL.EventHandlers
{
    /// <summary>
    /// Represents the status for a specific <see cref="EventHandler"/> for a specific tenant.
    /// </summary>
    public class EventHandlerStatusForTenant
    {
        /// <summary>
        /// Gets or sets the identifier of the tenant.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the last committed event sequence number.
        /// </summary>
        public int LastCommittedEventSequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets the filter position.
        /// </summary>
        public int FilterPosition { get; set; }

        /// <summary>
        /// Gets or sets the vent processor position.
        /// </summary>
        public int EventProcessorPosition { get; set; }
    }
}