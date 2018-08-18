/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Linq;
using System.Threading.Tasks;
using Dolittle.Applications;
using Dolittle.Artifacts;
using Dolittle.Logging;
using Dolittle.Runtime.Execution;
using Dolittle.Serialization.Protobuf;
using Grpc.Core;

namespace Dolittle.Runtime.Events.Relativity
{
    /// <summary>
    /// Represents an implementation of the <see cref="QuantumTunnelService.QuantumTunnelServiceBase"/>
    /// </summary>
    public class QuantumTunnelServiceImplementation : QuantumTunnelService.QuantumTunnelServiceBase
    {
        readonly IEventHorizon _eventHorizon;
        readonly ISerializer _serializer;
        readonly ILogger _logger;
        readonly IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initializes a new instance of <see cref="QuantumTunnelServiceImplementation"/>
        /// </summary>
        /// <param name="eventHorizon"><see cref="IEventHorizon"/> to work with</param>
        /// <param name="serializer"><see cref="ISerializer"/> to be used for serialization</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for dealing with <see cref="IExecutionContext"/></param>
        /// <param name="logger"><see cref="ILogger"/> for logging</param>
        public QuantumTunnelServiceImplementation(
            IEventHorizon eventHorizon,
            ISerializer serializer,
            IExecutionContextManager executionContextManager,
            ILogger logger)
        {
            _eventHorizon = eventHorizon;
            _serializer = serializer;
            _executionContextManager = executionContextManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override async Task Open(OpenTunnelMessage request, IServerStreamWriter<CommittedEventStreamParticleMessage> responseStream, ServerCallContext context)
        {
            var tunnel = new QuantumTunnel(_serializer, responseStream, _executionContextManager, _logger);
            var application = (Application) new Guid(request.Application.ToByteArray());
            var boundedContext = (BoundedContext) new Guid(request.Application.ToByteArray());
            var events = request
                .Events
                .Select(@event => @event.ToArtifact())
                .ToArray();

            var subscription = new EventParticleSubscription(events);

            _logger.Information($"Opening up a quantum tunnel for bounded context '{boundedContext}' in application '{application}'");
            
            var singularity = new Singularity(application, boundedContext, tunnel, subscription);
            _eventHorizon.GravitateTowards(singularity);
            tunnel.Collapsed += qt => _eventHorizon.Collapse(singularity);

            await tunnel.Open();

            _logger.Information($"Quantum tunnel collapsed for bounded context '{boundedContext}' in application '{application}'");
            
            await Task.CompletedTask;
        }
    }
}