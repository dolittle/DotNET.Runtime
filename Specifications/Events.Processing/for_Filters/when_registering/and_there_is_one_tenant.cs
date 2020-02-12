// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dolittle.Logging;
using Dolittle.Tenancy;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Processing.for_Filters.when_registering
{
    public class and_there_is_one_tenant : given.all_dependencies
    {
        static readonly TenantId tenant = Guid.NewGuid();
        static readonly StreamId target_stream_id = Guid.NewGuid();
        static readonly StreamId source_stream_id = StreamId.AllStreamId;

        static IFilters filters;

        Establish context = () =>
        {
            tenants_mock.SetupGet(_ => _.All).Returns(new TenantId[] { tenant });
            filters = new Filters(
                tenants_mock.Object,
                execution_context_manager_mock.Object,
                Processing.given.a_remote_filter_service(new SucceededFilteringResult(true, PartitionId.NotSet)),
                get_stream_processor_hub_mock.Object,
                get_event_to_stream_writer_mock.Object,
                Moq.Mock.Of<ILogger>());
        };

        Because of = () => filters.Register(target_stream_id, source_stream_id);

        It should_set_execution_context_for_tenant = () => execution_context_manager_mock.Verify(_ => _.CurrentFor(tenant, Moq.It.IsAny<string>(), Moq.It.IsAny<int>(), Moq.It.IsAny<string>()));
        It should_register_stream_processor_hub_with_correct_stream_id = () => stream_processor_hub_mock.Verify(_ => _.Register(Moq.It.IsAny<IEventProcessor>(), StreamId.AllStreamId));
    }
}