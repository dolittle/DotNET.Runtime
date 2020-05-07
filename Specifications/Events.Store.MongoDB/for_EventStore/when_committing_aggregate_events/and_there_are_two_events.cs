// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Dolittle.Logging;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Store.MongoDB.for_EventStore.when_committing_aggregate_events
{
    public class and_there_are_two_events : given.all_dependencies
    {
        static EventStore event_store;
        static UncommittedAggregateEvents uncommitted_events;
        static EventSourceId event_source;
        static Artifacts.Artifact aggregate_root;
        static AggregateRootVersion aggregate_root_version;
        static CommittedAggregateEvents result;

        Establish context = () =>
        {
            event_source = Guid.NewGuid();
            aggregate_root = new Artifacts.Artifact(Guid.NewGuid(), 0);
            aggregate_root_version = AggregateRootVersion.Initial;
            uncommitted_events = new UncommittedAggregateEvents(event_source, aggregate_root, aggregate_root_version, new UncommittedEvent[] { an_uncommitted_event, an_uncommitted_event });
            event_store = new EventStore(
                execution_context_manager.Object,
                an_event_store_connection,
                event_committer,
                aggregate_roots,
                metrics.Object,
                Moq.Mock.Of<ILogger>());
        };

        Because of = () => result = event_store.CommitAggregateEvents(uncommitted_events, CancellationToken.None).GetAwaiter().GetResult();

        It should_return_two_committed_events = () => result.Count.ShouldEqual(2);
        It should_have_the_correct_event_source = () => result.EventSource.ShouldEqual(event_source);
        It should_have_the_correct_aggregate_root = () => result.AggregateRoot.ShouldEqual(aggregate_root.Id);
    }
}