// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Dolittle.Runtime.Management.GraphQL.EventHandlers
{
    /// <summary>
    /// Represents an event handler and its current state.
    /// </summary>
    public class EventHandler
    {
        /// <summary>
        /// Gets the unique identifier of the <see cref="EventHandler"/>
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets the scope the unique handler works on.
        /// </summary>
        public Guid Scope { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="EventHandlerStatusPerTenant">statuses per tenant</see>.
        /// </summary>
        public IEnumerable<EventHandlerStatusForTenant> StatusPerTenant { get; set; }
    }
}