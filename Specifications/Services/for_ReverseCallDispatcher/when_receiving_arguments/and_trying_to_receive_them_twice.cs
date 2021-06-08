// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Protobuf;
using Dolittle.Services.Contracts;
using Google.Protobuf.WellKnownTypes;
using Machine.Specifications;
using Moq;
using ExecutionContracts = Dolittle.Execution.Contracts;
using It = Machine.Specifications.It;

namespace Dolittle.Runtime.Services.for_ReverseCallDispatcher.when_receiving_arguments
{
    public class and_trying_to_receive_them_twice : given.a_dispatcher
    {
        static ExecutionContracts.ExecutionContext execution_context;
        static MyConnectArguments arguments;

        Establish context = () =>
        {
            execution_context = given.execution_contexts.create_protobuf();
            arguments = new MyConnectArguments { Context = new ReverseCallArgumentsContext { ExecutionContext = execution_context, PingInterval = Duration.FromTimeSpan(new System.TimeSpan(0, 0, 1)) } };

            client_stream.Setup(_ => _.MoveNext(Moq.It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            client_stream.SetupGet(_ => _.Current).Returns(new MyClientMessage { Arguments = arguments });
        };
        static bool result;
        static Exception exception;

        Because of = () =>
        {
            result = dispatcher.ReceiveArguments(CancellationToken.None).GetAwaiter().GetResult();
            exception = Catch.Exception(() => dispatcher.ReceiveArguments(CancellationToken.None).GetAwaiter().GetResult());
        };

        It should_return_true = () => result.ShouldBeTrue();
        It should_have_the_correct_arguments = () => dispatcher.Arguments.ShouldEqual(arguments);
        It should_change_execution_context = () => execution_context_manager
            .Verify(
                _ => _.CurrentFor(
                    execution_context.ToExecutionContext(),
                    Moq.It.IsAny<string>(),
                    Moq.It.IsAny<int>(),
                    Moq.It.IsAny<string>()), Times.Once);
        It should_throw_exception_on_second_try = () => exception.ShouldBeOfExactType<ReverseCallDispatcherAlreadyTriedToReceiveArguments>();
        It shouldnt_have_tried_to_read_the_stream = () => client_stream.Verify(_ => _.MoveNext(Moq.It.IsAny<CancellationToken>()), Times.Once);
    }
}
