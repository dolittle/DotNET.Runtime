// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Dolittle.Logging;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Store.MongoDB.for_EventStore.when_committing_events
{
    public class and_there_are_two_events : given.all_dependencies
    {
        static EventStore event_store;
        static UncommittedEvents uncommitted_events;
        static CommittedEvents result;

        Establish context = () =>
        {
            uncommitted_events = new UncommittedEvents(new UncommittedEvent[] { an_uncommitted_event, an_uncommitted_event });
            event_store = new EventStore(
                execution_context_manager.Object,
                an_event_store_connection,
                event_committer,
                aggregate_roots,
                metrics.Object,
                Moq.Mock.Of<ILogger>());
        };

        Because of = () => result = event_store.CommitEvents(uncommitted_events, CancellationToken.None).GetAwaiter().GetResult();

        It should_return_two_committed_events = () => result.Count.ShouldEqual(2);
    }
}