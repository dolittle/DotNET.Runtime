// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dolittle.Runtime.Events.Processing
{
    public class in_memory_event_to_stream_writer : IWriteEventToStream
    {
        readonly IDictionary<StreamId, IList<CommittedEvent>> events = new Dictionary<StreamId, IList<CommittedEvent>>();

        public Task<bool> Write(CommittedEvent @event, StreamId streamId)
        {
            if (events.ContainsKey(streamId))
            {
                var newEvents = events[streamId];
                newEvents.Add(@event);
                events.Add(streamId, newEvents);
            }
            else
            {
                events.Add(streamId, new CommittedEvent[] { @event });
            }

            return Task.FromResult(true);
        }
    }
}