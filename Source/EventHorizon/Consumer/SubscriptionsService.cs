// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Dolittle.Runtime.ApplicationModel;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Lifecycle;
using Microsoft.Extensions.Logging;
using Dolittle.Runtime.Protobuf;
using Dolittle.Runtime.Tenancy;
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
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsService"/> class.
        /// </summary>
        /// <param name="getConsumerClient">The <see cref="FactoryFor{T}" /><see cref="IConsumerClient" />.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for current <see cref="ExecutionContext"/>.</param>
        /// <param name="logger"><see cref="ILogger"/> for logging.</param>
        public SubscriptionsService(
            FactoryFor<IConsumerClient> getConsumerClient,
            IExecutionContextManager executionContextManager,
            ILogger logger)
        {
            _getConsumerClient = getConsumerClient;
            _executionContextManager = executionContextManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override async Task<Contracts.SubscriptionResponse> Subscribe(Contracts.Subscription subscriptionRequest, ServerCallContext context)
        {
            var subscriptionId = new SubscriptionId(
                subscriptionRequest.CallContext.ExecutionContext.TenantId.ToGuid(),
                subscriptionRequest.MicroserviceId.ToGuid(),
                subscriptionRequest.TenantId.ToGuid(),
                subscriptionRequest.ScopeId.ToGuid(),
                subscriptionRequest.StreamId.ToGuid(),
                subscriptionRequest.PartitionId.ToGuid());
            try
            {
                _logger.IncomingSubscripton(subscriptionId);
                _executionContextManager.CurrentFor(subscriptionRequest.CallContext.ExecutionContext);
                var subscriptionResponse = await _getConsumerClient().HandleSubscriptionRequest(subscriptionId, context.CancellationToken).ConfigureAwait(false);

                return subscriptionResponse switch
                {
                    { Success: false } => new Contracts.SubscriptionResponse { Failure = subscriptionResponse.Failure },
                    _ => new Contracts.SubscriptionResponse { ConsentId = subscriptionResponse.ConsentId.ToProtobuf() },
                };
            }
            catch (TaskCanceledException)
            {
                return new Contracts.SubscriptionResponse { Failure = new Failure(SubscriptionFailures.SubscriptionCancelled, "Event Horizon subscription was cancelled") };
            }
            catch (Exception ex)
            {
                if (!context.CancellationToken.IsCancellationRequested)
                {
                    _logger.ErrorWhileSubscribing(ex, subscriptionId);
                }

                return new Contracts.SubscriptionResponse { Failure = new Failure(FailureId.Other, "InternalServerError") };
            }
        }
    }
}
