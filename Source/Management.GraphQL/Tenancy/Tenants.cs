// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Dolittle.Runtime.Tenancy;

namespace Dolittle.Runtime.Management.GraphQL.Tenancy
{
    /// <summary>
    /// Represents an API for getting all tenants.
    /// </summary>
    public class Tenants
    {
        readonly ITenants _tenants;

        /// <summary>
        /// Initializes a new instance of <see cref="Tenants"/>.
        /// </summary>
        /// <param name="tenants">The runtime <see cref="ITenants"/>.</param>
        public Tenants(ITenants tenants)
        {
            _tenants = tenants;
        }

        /// <summary>
        /// Gets all the tenants registered with the runtime.
        /// </summary>
        public IEnumerable<Guid> All() => _tenants.All.Select(_ => _.Value);
    }
}