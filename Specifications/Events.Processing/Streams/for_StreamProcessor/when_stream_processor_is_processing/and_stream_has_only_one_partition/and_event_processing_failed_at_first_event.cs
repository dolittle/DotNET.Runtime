// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Logging;
using Dolittle.Runtime.Events.Streams;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Processing.Streams.for_StreamProcessor.when_stream_processor_is_processing.and_stream_has_only_one_partition
{
    public class and_event_processing_failed_at_first_event : given.all_dependencies
    {
        const string reason = "some reason";
        static readonly PartitionId partition_id = PartitionId.NotSet;
        static readonly Store.CommittedEvent first_event = Processing.given.a_committed_event;
        static readonly EventProcessorId event_processor_id = Guid.NewGuid();
        static readonly Moq.Mock<IEventProcessor> event_processor_mock = Processing.given.an_event_processor_mock(event_processor_id, (new FailedProcessingResult(reason), partition_id, first_event));
        static StreamProcessor stream_processor;
        static Task task;

        Establish context = () =>
        {
            var event_with_partition = new StreamEvent(first_event, Guid.NewGuid(), partition_id);
            next_event_fetcher.Setup(_ => _.Fetch(Moq.It.IsAny<StreamId>(), 0, Moq.It.IsAny<CancellationToken>())).Returns(Task.FromResult(event_with_partition));
            next_event_fetcher.Setup(_ => _.Fetch(Moq.It.IsAny<StreamId>(), 1, Moq.It.IsAny<CancellationToken>())).Throws(new Exception());
            stream_processor = new StreamProcessor(tenant_id, source_stream_id, event_processor_mock.Object, stream_processor_state_repository, next_event_fetcher.Object, Moq.Mock.Of<ILogger>());
        };

        Because of = () => stream_processor.BeginProcessing().Wait();

        It should_process_one_event = () => event_processor_mock.Verify(_ => _.Process(Moq.It.IsAny<Store.CommittedEvent>(), partition_id), Moq.Times.Once());
        It should_process_first_event = () => event_processor_mock.Verify(_ => _.Process(first_event, partition_id), Moq.Times.Once());
        It should_have_current_position_equal_zero = () => stream_processor.CurrentState.Position.ShouldEqual(new StreamPosition(1));
        It should_have_one_failing_partition = () => stream_processor.CurrentState.FailingPartitions.Count.ShouldEqual(1);
        It should_have_the_correct_failing_partition = () => stream_processor.CurrentState.FailingPartitions.ContainsKey(partition_id).ShouldBeTrue();
        It should_have_the_correct_position_on_the_failing_partition = () => stream_processor.CurrentState.FailingPartitions[partition_id].Position.ShouldEqual(new StreamPosition(0));
        It should_have_the_correct_retry_time_on_the_failing_partition = () => stream_processor.CurrentState.FailingPartitions[partition_id].RetryTime.ShouldEqual(DateTimeOffset.MaxValue);
        It should_have_the_correct_reason_on_the_failing_partition = () => stream_processor.CurrentState.FailingPartitions[partition_id].Reason.ShouldEqual(reason);
    }
}