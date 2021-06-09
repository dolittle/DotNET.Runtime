// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Services.ReverseCalls;
using Grpc.Core;
using Machine.Specifications;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dolittle.Runtime.Services.for_ReverseCallDispatcher.given
{
    public class a_dispatcher
    {
        protected static IReverseCallDispatcher<MyClientMessage, MyServerMessage, MyConnectArguments, MyConnectResponse, MyRequest, MyResponse> dispatcher;
        protected static Mock<IExecutionContextManager> execution_context_manager;
        protected static Mock<IPingedConnection<MyClientMessage, MyServerMessage>> pinged_connection;

        protected static Mock<IAsyncStreamReader<MyClientMessage>> client_stream;
        protected static Mock<IServerStreamWriter<MyServerMessage>> server_stream;

        protected static CancellationToken cancellation_token;

        Establish context = () =>
        {
            execution_context_manager = new();
            pinged_connection = new();
            client_stream = new();
            server_stream = new();
            cancellation_token = new();

            pinged_connection.SetupGet(_ => _.RuntimeStream).Returns(client_stream.Object);
            pinged_connection.SetupGet(_ => _.ClientStream).Returns(server_stream.Object);
            pinged_connection.SetupGet(_ => _.CancellationToken).Returns(cancellation_token);
            dispatcher = new ReverseCallDispatcher<MyClientMessage, MyServerMessage, MyConnectArguments, MyConnectResponse, MyRequest, MyResponse>(
                pinged_connection.Object,
                new MyProtocol(),
                execution_context_manager.Object,
                Mock.Of<ILogger>());
        };
    }
}
