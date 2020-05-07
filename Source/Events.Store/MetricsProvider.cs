// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dolittle.Runtime.Events.Store
{
    /// <summary>
    /// Provides metrics collectors.
    /// </summary>
    public class MetricsProvider
    {
        readonly IMetrics _metrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsProvider"/> class.
        /// </summary>
        /// <param name="metrics">basdasd.</param>
        public MetricsProvider(IMetrics metrics)
        {
            _metrics = metrics;
        }
    }
}