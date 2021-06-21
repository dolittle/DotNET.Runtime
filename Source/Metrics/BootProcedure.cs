// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.Runtime.Booting;

namespace Dolittle.Runtime.Metrics
{
    /// <summary>
    /// Represents a <see cref="ICanPerformBootProcedure">boot procedure</see> for hooking up metrics.
    /// </summary>
    public class BootProcedure : ICanPerformBootProcedure
    {
        readonly IMetricProviders _metricProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="BootProcedure"/> class.
        /// </summary>
        /// <param name="metricProviders"><see cref="IMetricProviders"/>.</param>
        public BootProcedure(
            IMetricProviders metricProviders)
        {
            _metricProviders = metricProviders;
        }

        /// <inheritdoc/>
        public bool CanPerform() => true;

        /// <inheritdoc/>
        public void Perform()
        {
            _metricProviders.Provide();
        }
    }
}