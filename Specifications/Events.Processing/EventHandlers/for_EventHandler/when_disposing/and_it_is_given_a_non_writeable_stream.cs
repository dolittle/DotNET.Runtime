// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Events.Store.Streams;
using Machine.Specifications;
using static Moq.It;

namespace Dolittle.Runtime.Events.Processing.EventHandlers.for_EventHandler
{
    public class and_it_is_given_a_non_writeable_stream : given.an_event_handler_with_non_writeable_target_stream
    {
        static StreamProcessor stream_processor;
        Because of = async () => await event_handler.Register().ConfigureAwait(false);

        It should_reject_with_cannot_register_event_handler_on_writeable_stream = () => failure.Id.ShouldEqual(EventHandlersFailures.CannotRegisterEventHandlerOnNonWriteableStream);
        It should_not_register_any_stream_processors = () => stream_processors.Verify(_ => _
                                                                    .TryRegister(
                                                                        IsAny<ScopeId>(),
                                                                        IsAny<EventProcessorId>(),
                                                                        IsAny<IStreamDefinition>(),
                                                                        IsAny<FactoryFor<IEventProcessor>>(),
                                                                        IsAny<CancellationToken>(),
                                                                        out stream_processor), Moq.Times.Never());
    }
}