// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Filters;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store.Streams;
using Machine.Specifications;
using static Moq.It;

namespace Dolittle.Runtime.Events.Processing.EventHandlers.for_EventHandler
{
    public class and_it_fails_registering_filter_processor : given.an_event_handler
    {
        static StreamProcessor stream_processor;
        Establish context = () =>
        {
            stream_processors.Setup(_ => _
                .TryRegister(
                    event_handler.Scope,
                    event_handler.EventProcessor,
                    IsAny<EventLogStreamDefinition>(),
                    IsAny<FactoryFor<IEventProcessor>>(),
                    IsAny<CancellationToken>(),
                    out stream_processor
                )).Returns(false);
        };

        Because of = async () => await event_handler.Register().ConfigureAwait(false);

        It should_reject_with_failed_to_register_filter = () => failure.Id.ShouldEqual(FiltersFailures.FailedToRegisterFilter);

        It should_try_to_register_filter_processor = () => stream_processors.Verify(_ => _
                                                                .TryRegister(
                                                                    event_handler.Scope,
                                                                    event_handler.EventProcessor,
                                                                    IsAny<EventLogStreamDefinition>(),
                                                                    IsAny<FactoryFor<IEventProcessor>>(),
                                                                    IsAny<CancellationToken>(),
                                                                    out stream_processor), Moq.Times.Once());

        It should_skip_trying_to_register_event_processor = () => stream_processors.Verify(_ => _
                                                                .TryRegister(
                                                                    event_handler.Scope,
                                                                    event_handler.EventProcessor,
                                                                    event_handler.FilteredStreamDefinition,
                                                                    IsAny<FactoryFor<IEventProcessor>>(),
                                                                    IsAny<CancellationToken>(),
                                                                    out stream_processor), Moq.Times.Never());
    }
}