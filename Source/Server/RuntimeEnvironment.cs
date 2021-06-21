// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dolittle.Runtime.Server
{
    /// <summary>
    /// Represents helpers for getting details about the Runtime environment.
    /// </summary>
    public static class RuntimeEnvironment
    {
        static RuntimeEnvironment()
        {
            IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }

        /// <summary>
        /// Gets whether or not we're running in development or not.
        /// </summary>
        public static readonly bool IsDevelopment;
    }
}
