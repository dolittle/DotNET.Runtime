/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Collections.Generic;
using System.Linq;
using Dolittle.Types;
using Prometheus;

namespace Dolittle.Runtime.Metrics
{
    /// <summary>
    /// Represents an implementation of <see cref="IMetricProviders"/>
    /// </summary>
    public class MetricProviders : IMetricProviders
    {
        readonly IInstancesOf<ICanProvideMetrics> _providers;
        readonly IMetricFactory _metricFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="MetricProviders"/>
        /// </summary>
        /// <param name="providers"><see cref="IInstancesOf{T}"/> of <see cref="ICanProvideMetrics"/></param>
        /// <param name="metricFactory"><see cref="IMetricFactory"/></param>
        public MetricProviders(
            IInstancesOf<ICanProvideMetrics> providers,
            IMetricFactory metricFactory)
        {
            _providers = providers;
            _metricFactory = metricFactory;
        }

        /// <inheritdoc/>
        public IEnumerable<Collector> Provide()
        {
            return _providers.SelectMany(_ => _.Provide(_metricFactory));
        }
    }
}