// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dolittle.Runtime.Configuration;

namespace Dolittle.Runtime.Services.Clients
{
    /// <summary>
    /// Represents the configuration for clients by their <see cref="EndpointVisibility"/>.
    /// </summary>
    [Name("clients")]
    public class ClientEndpointsConfiguration :
        ReadOnlyDictionary<EndpointVisibility, ClientEndpointConfiguration>,
        IConfigurationObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEndpointsConfiguration"/> class.
        /// </summary>
        /// <param name="configuration">Dictionary for <see cref="EndpointVisibility"/> with <see cref="ClientEndpointConfiguration"/>.</param>
        public ClientEndpointsConfiguration(IDictionary<EndpointVisibility, ClientEndpointConfiguration> configuration)
            : base(configuration)
        {
        }
    }
}