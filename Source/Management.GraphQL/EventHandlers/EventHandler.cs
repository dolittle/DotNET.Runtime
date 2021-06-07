// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dolittle.Runtime.Management.GraphQL.EventHandlers
{
    public class EventHandler
    {
        public Guid Id { get; set; }
        public Guid Scope { get; set; }
        public int FilterPosition { get; set; }
        public int EventProcessorPosition { get; set; }
        public int TailEventLogSequenceNumber { get; set; }
    }
}