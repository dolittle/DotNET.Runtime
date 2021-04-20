// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Rudimentary;
using Dolittle.Runtime.Embeddings.Store;
using Machine.Specifications;
using Microsoft.Extensions.Logging;
using Moq;
using It = Machine.Specifications.It;

namespace Dolittle.Runtime.Embeddings.Processing.for_EmbeddingProcessors
{
    public class when_starting_one_processor_that_is_cancelled : given.two_tenants
    {
        static EmbeddingProcessors processors;
        static Mock<EmbeddingProcessorFactory> factory;
        static CancellationToken processor_a_cancellation_token;
        static EmbeddingId embedding;

        Establish context = () =>
        {
            processors = new EmbeddingProcessors(tenants.Object, Mock.Of<ILogger>());

            var processor_a = new Mock<IEmbeddingProcessor>();
            processor_a.Setup(_ => _.Start(Moq.It.IsAny<CancellationToken>())).Returns<CancellationToken>((cancellationToken) => {
                processor_a_cancellation_token = cancellationToken;
                return new TaskCompletionSource<Try>().Task;
            });

            var processor_b = new Mock<IEmbeddingProcessor>();
            processor_b.Setup(_ => _.Start(Moq.It.IsAny<CancellationToken>())).Returns(Task.FromCanceled<Try>(CancellationToken.None));

            factory = new Mock<EmbeddingProcessorFactory>();
            factory.Setup(_ => _(tenant_a)).Returns(processor_a.Object);
            factory.Setup(_ => _(tenant_b)).Returns(processor_b.Object);

            embedding = "c0b4c09b-00e4-4974-a74f-980b33b59758";
        };

        static Task completed;
        Because of = () => completed = processors.StartEmbeddingProcessorForAllTenants(embedding, factory.Object, CancellationToken.None);

        It should_be_cancelled = () => completed.Status.ShouldEqual(TaskStatus.Canceled);
        It should_cancel_the_other_processor = () => processor_a_cancellation_token.IsCancellationRequested.ShouldBeTrue();
    }
}