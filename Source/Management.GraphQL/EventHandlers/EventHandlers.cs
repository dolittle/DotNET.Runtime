// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dolittle.Runtime.ApplicationModel;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.EventHandlers;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Tenancy;

namespace Dolittle.Runtime.Management.GraphQL.EventHandlers
{
    public class EventHandlers
    {
        readonly IEventHandlers _eventHandlers;
        readonly IExecutionContextManager _executionContextManager;
        readonly IContainer _container;
        readonly ITenants _tenants;

        public EventHandlers(IEventHandlers eventHandlers, IExecutionContextManager executionContextManager, IContainer container, ITenants tenants)
        {
            _eventHandlers = eventHandlers;
            _executionContextManager = executionContextManager;
            _container = container;
            _tenants = tenants;
        }

        public async Task<IEnumerable<EventHandler>> All()
        {
            var lastCommittedEventSequenceNumberPerTenant = new Dictionary<TenantId, EventLogSequenceNumber>();

            var tasks = _tenants.All.Select(async tenant =>
            {
                _executionContextManager.CurrentFor(Microservice.NotSet, tenant);
                var eventStore = _container.Get<FactoryFor<IEventStore>>()();
                lastCommittedEventSequenceNumberPerTenant[tenant] = await eventStore.GetLastCommittedEventSequenceNumber().ConfigureAwait(false);
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);

            return _eventHandlers.All.Select(_ =>
            {
                var query = from filter in _.FilterStreamProcessor.StreamProcessorsPerTenant
                            join eventProcessor in _.EventProcessorStreamProcessor.StreamProcessorsPerTenant on filter.Key equals eventProcessor.Key
                            select new EventHandlerStatusForTenant
                            {
                                TenantId = filter.Key,
                                LastCommittedEventSequenceNumber = (int)lastCommittedEventSequenceNumberPerTenant[filter.Key].Value,
                                FilterPosition = (int)filter.Value.CurrentState.Position.Value,
                                EventProcessorPosition = (int)eventProcessor.Value.CurrentState.Position.Value
                            };

                return new EventHandler
                {
                    Id = _.EventProcessor,
                    Scope = _.Scope,
                    StatusPerTenant = query.ToArray()
                };
            });
        }
    }
}