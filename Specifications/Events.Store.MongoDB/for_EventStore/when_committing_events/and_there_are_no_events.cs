// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Dolittle.Logging;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Store.MongoDB.for_EventStore.when_committing_events
{
    public class and_there_are_no_events : given.all_dependencies
    {
        static EventStore event_store;
        static UncommittedEvents uncommitted_events;
        static Exception exception;

        Establish context = () =>
        {
            uncommitted_events = new UncommittedEvents(System.Array.Empty<UncommittedEvent>());
            event_store = new EventStore(
                execution_context_manager.Object,
                an_event_store_connection,
                event_committer,
                aggregate_roots,
                metrics.Object,
                Moq.Mock.Of<ILogger>());
        };

        Because of = () => exception = Catch.Exception(() => event_store.CommitEvents(uncommitted_events, CancellationToken.None).GetAwaiter().GetResult());

        It should_throw_an_exception = () => exception.ShouldNotBeNull();
        It should_fail_because_there_are_no_events_to_commit = () => exception.ShouldBeOfExactType<NoEventsToCommit>();
    }
}