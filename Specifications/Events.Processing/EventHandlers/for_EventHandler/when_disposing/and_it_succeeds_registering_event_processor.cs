// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Contracts;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store.Streams;
using Machine.Specifications;
using static Moq.It;
using static Moq.Times;

namespace Dolittle.Runtime.Events.Processing.EventHandlers.for_EventHandler
{
    [Ignore("This needs to be fixed (mock IStreamProcessors)")]
    public class and_it_succeeds_registering_event_processor : given.an_event_handler
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
                 )).Returns(true);

            stream_processors.Setup(_ => _
                .TryRegister(
                        event_handler.Scope,
                        event_handler.EventProcessor,
                        event_handler.FilteredStreamDefinition,
                        IsAny<FactoryFor<IEventProcessor>>(),
                        IsAny<CancellationToken>(),
                    out stream_processor
                )).Returns(true);
        };

        Because of = async () => await event_handler.Register().ConfigureAwait(false);

        It should_try_to_register_filter_processor = () => stream_processors.Verify(_ => _
                                                                .TryRegister(
                                                                    event_handler.Scope,
                                                                    event_handler.EventProcessor,
                                                                    IsAny<EventLogStreamDefinition>(),
                                                                    IsAny<FactoryFor<IEventProcessor>>(),
                                                                    IsAny<CancellationToken>(),
                                                                    out stream_processor), Once());

        It should_accept_event_handler = () => reverse_call_dispatcher.Verify(_ => _.Accept(IsAny<EventHandlerRegistrationResponse>(), IsAny<CancellationToken>()), Once());
    }
}