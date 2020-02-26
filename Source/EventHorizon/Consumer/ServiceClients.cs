// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias contracts;

using System.Collections.Generic;
using Dolittle.Services;
using Dolittle.Services.Clients;
using grpc = contracts::Dolittle.Runtime.Events.Processing;

namespace Dolittle.Runtime.EventHorizon
{
    /// <summary>
    /// Represents something that knows about service clients.
    /// </summary>
    public class ServiceClients : IKnowAboutClients
    {
        /// <inheritdoc/>
        public IEnumerable<Client> Clients => new[]
        {
            new Client(EndpointVisibility.Private, typeof(grpc.EventHorizon.EventHorizonClient), grpc.Filters.Descriptor),
        };
    }
}