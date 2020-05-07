// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dolittle.Execution;
using Dolittle.Runtime.Events.Store.MongoDB.Aggregates;
using Dolittle.Runtime.Events.Store.MongoDB.Events;
using Machine.Specifications;
using Moq;

namespace Dolittle.Runtime.Events.Store.MongoDB.for_EventStore.given
{
    public class all_dependencies
    {
        protected static UncommittedEvent an_uncommitted_event => new UncommittedEvent(Guid.NewGuid(), new Artifacts.Artifact(Guid.NewGuid(), 0), false, events.some_event_content);

        protected static an_event_store_connection an_event_store_connection;
        protected static Mock<IExecutionContextManager> execution_context_manager;
        protected static IEventCommitter event_committer;
        protected static IAggregateRoots aggregate_roots;
        protected static Mock<IMetrics> metrics;

        Establish context = () =>
        {
            an_event_store_connection = new an_event_store_connection(new a_mongo_db_connection());
            execution_context_manager = new Mock<IExecutionContextManager>();
            execution_context_manager.SetupGet(_ => _.Current).Returns(execution_contexts.create());
            event_committer = new EventCommitter(an_event_store_connection);
            aggregate_roots = new AggregateRoots(an_event_store_connection);
            metrics = new Mock<IMetrics>();
        };

        Cleanup cleanup = () => an_event_store_connection.Dispose();
    }
}