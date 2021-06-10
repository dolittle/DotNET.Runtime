// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Machine.Specifications;
using Moq;
using Dolittle.Runtime.Events.Processing.Streams;
using System.Threading;
using Dolittle.Runtime.Events.Processing;
using Dolittle.Runtime.Resilience;
using Dolittle.Runtime.Events.Store.Streams;
using Nito.AsyncEx;
using Microsoft.Extensions.Logging.Abstractions;
using Dolittle.Runtime.Services.Clients;
using Dolittle.Runtime.Microservices;

namespace Dolittle.Runtime.EventHorizon.Consumer.Connections.for_EventHorizonConnectionFactory.given
{
    public class all_dependencies
    {
        protected static Mock<IReverseCallClients> reverse_call_clients;
        protected static MicroserviceAddress microservice_address;
        protected static EventHorizonConnectionFactory factory;

        Establish context = () =>
        {
            reverse_call_clients = new Mock<IReverseCallClients>();
            microservice_address = new MicroserviceAddress("host", 42);

            factory = new EventHorizonConnectionFactory(reverse_call_clients.Object, NullLoggerFactory.Instance);
        };
    }
}