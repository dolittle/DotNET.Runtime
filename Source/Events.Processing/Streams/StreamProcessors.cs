// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Lifecycle;
using Microsoft.Extensions.Logging;
using Dolittle.Runtime.Tenancy;

namespace Dolittle.Runtime.Events.Processing.Streams
{
    /// <summary>
    /// Represents an implementation of <see cref="IStreamProcessors" />.
    /// </summary>
    [Singleton]
    public class StreamProcessors : IStreamProcessors
    {
        readonly ConcurrentDictionary<StreamProcessorId, StreamProcessor> _streamProcessors;
        readonly IPerformActionOnAllTenants _onAllTenants;
        readonly FactoryFor<ICreateScopedStreamProcessors> _getScopedStreamProcessorsCreator;
        readonly IExecutionContextManager _executionContextManager;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamProcessors"/> class.
        /// </summary>
        /// <param name="onAllTenants">The <see cref="IPerformActionOnAllTenants" />.</param>
        /// <param name="getScopedStreamProcessorsCreator">The <see cref="FactoryFor{T}" /> <see cref="ICreateScopedStreamProcessors" />.</param>
        /// <param name="executionContextManager">The <see cref="IExecutionContextManager" />.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory" />.</param>
        public StreamProcessors(
            IPerformActionOnAllTenants onAllTenants,
            FactoryFor<ICreateScopedStreamProcessors> getScopedStreamProcessorsCreator,
            IExecutionContextManager executionContextManager,
            ILoggerFactory loggerFactory)
        {
            _onAllTenants = onAllTenants;
            _getScopedStreamProcessorsCreator = getScopedStreamProcessorsCreator;
            _streamProcessors = new ConcurrentDictionary<StreamProcessorId, StreamProcessor>();
            _executionContextManager = executionContextManager;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<StreamProcessors>();
        }

        /// <inheritdoc />
        public bool TryRegister(
            ScopeId scopeId,
            EventProcessorId eventProcessorId,
            IStreamDefinition sourceStreamDefinition,
            Func<IEventProcessor> getEventProcessor,
            CancellationToken cancellationToken,
            out StreamProcessor streamProcessor)
        {
            _logger.LogTrace("Starting StreamProcessors.TryRegister for EventProcessorId {EventProcessorId} in scope {ScopeId}", eventProcessorId, scopeId);

            streamProcessor = default;
            var streamProcessorId = new StreamProcessorId(scopeId, eventProcessorId, sourceStreamDefinition.StreamId);
            if (_streamProcessors.ContainsKey(streamProcessorId))
            {
                _logger.StreamProcessorAlreadyRegistered(streamProcessorId);
                return false;
            }

            streamProcessor = new StreamProcessor(
                streamProcessorId,
                _onAllTenants,
                sourceStreamDefinition,
                getEventProcessor,
                () => Unregister(streamProcessorId),
                _getScopedStreamProcessorsCreator,
                _executionContextManager,
                _loggerFactory.CreateLogger<StreamProcessor>(),
                cancellationToken);
            if (!_streamProcessors.TryAdd(streamProcessorId, streamProcessor))
            {
                _logger.StreamProcessorAlreadyRegistered(streamProcessorId);
                streamProcessor = default;
                return false;
            }

            _logger.StreamProcessorSuccessfullyRegistered(streamProcessorId);
            return true;
        }

        void Unregister(StreamProcessorId id)
        {
            _logger.StreamProcessorUnregistered(id);
            _streamProcessors.TryRemove(id, out var _);
        }
    }
}
