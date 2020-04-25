// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.Runtime.Events.Store.Streams;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Store.MongoDB.Streams.for_EventFromEventLogFetcher
{
    public class when_creating_EventFromEventLogFetcher : given.all_dependencies
    {
        It should_only_have_all_stream_as_the_well_known_stream = () => fetcher.WellKnownStreams.ShouldContainOnly(StreamId.AllStreamId);
    }
}