// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Store.Specs.for_CommittedEvents
{
    public class when_creating_with_out_of_order_event_log_version : given.events
    {
        static CommittedEvent out_of_order_event;
        static CommittedEvents events;
        static Exception exception;

        Establish context = () =>
        {
            out_of_order_event = new CommittedEvent(4, DateTimeOffset.Now, correlation_id, microservice_id, tenant_id, cause, event_b_artifact, "wrong");
        };

        Because of = () => exception = Catch.Exception(() =>
        {
            events = new CommittedEvents(new[] { out_of_order_event, event_one, event_two, event_three });
        });

        It should_throw_an_exception = () => exception.ShouldBeOfExactType<EventLogVersionIsOutOfOrder>();
        It should_not_be_created = () => events.ShouldBeNull();
    }
}