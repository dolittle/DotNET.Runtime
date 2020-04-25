// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Dolittle.ApplicationModel;
using Dolittle.DependencyInversion;
using Dolittle.Execution;
using Dolittle.Lifecycle;
using Dolittle.Logging;
using Dolittle.Protobuf;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Tenancy;
using Dolittle.Tenancy;
using Grpc.Core;
using static Dolittle.Runtime.EventHorizon.Contracts.Subscriptions;

namespace Dolittle.Runtime.EventHorizon.Consumer
{
    /// <summary>
    /// Represents the implementation of <see creF="FiltersBase"/>.
    /// </summary>
    [Singleton]
    public class SubscriptionsService : SubscriptionsBase
    {
        readonly FactoryFor<IConsumerClient> _getConsumerClient;
        readonly IExecutionContextManager _executionContextManager;
        readonly ITenants _tenants;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsService"/> class.
        /// </summary>
        /// <param name="getConsumerClient">The <see cref="FactoryFor{T}" /><see cref="IConsumerClient" />.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for current <see cref="Execution.ExecutionContext"/>.</param>
        /// <param name="tenants">The <see cref="ITenants"/> system.</param>
        /// <param name="logger"><see cref="ILogger"/> for logging.</param>
        public SubscriptionsService(
            FactoryFor<IConsumerClient> getConsumerClient,
            IExecutionContextManager executionContextManager,
            ITenants tenants,
            ILogger<SubscriptionsService> logger)
        {
            _getConsumerClient = getConsumerClient;
            _executionContextManager = executionContextManager;
            _tenants = tenants;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override async Task<Contracts.SubscriptionResponse> Subscribe(Contracts.Subscription subscriptionRequest, ServerCallContext context)
        {
            _executionContextManager.CurrentFor(subscriptionRequest.CallContext.ExecutionContext);
            var consumerTenant = _executionContextManager.Current.Tenant;
            var subscription = new Subscription(
                consumerTenant,
                subscriptionRequest.MicroserviceId.To<Microservice>(),
                subscriptionRequest.TenantId.To<TenantId>(),
                subscriptionRequest.ScopeId.To<ScopeId>(),
                subscriptionRequest.StreamId.To<StreamId>(),
                subscriptionRequest.PartitionId.To<PartitionId>());

            _logger.Information($"Incomming event horizon subscription request from head to runtime. {subscription}");
            var subscriptionResponse = await _getConsumerClient().HandleSubscription(subscription).ConfigureAwait(false);

            return subscriptionResponse switch
                {
                    { Success: false } => new Contracts.SubscriptionResponse { Failure = subscriptionResponse.Failure },
                    _ => new Contracts.SubscriptionResponse(),
                };
        }
    }
}
