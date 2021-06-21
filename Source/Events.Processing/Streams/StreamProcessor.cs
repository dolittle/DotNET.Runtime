// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.ApplicationModel;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Tenancy;
using Microsoft.Extensions.Logging;

namespace Dolittle.Runtime.Events.Processing.Streams
{
    /// <summary>
    /// Represents a system for working with all the <see cref="AbstractScopedStreamProcessor" /> registered for <see cref="ITenants.All" />.
    /// </summary>
    public class StreamProcessor : IDisposable
    {       
        readonly StreamProcessorId _identifier;
        readonly IPerformActionOnAllTenants _onAllTenants;
        readonly IStreamDefinition _streamDefinition;
        readonly FactoryFor<IEventProcessor> _getEventProcessor;
        readonly Action _unregister;
        readonly FactoryFor<ICreateScopedStreamProcessors> _getScopedStreamProcessorsCreator;
        readonly IExecutionContextManager _executionContextManager;
        readonly ILogger<StreamProcessor> _logger;
        readonly CancellationTokenSource _stopAllScopedStreamProcessorsTokenSource;
        bool _initialized;
        bool _started;
        bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamProcessor"/> class.
        /// </summary>
        /// <param name="streamProcessorId">The <see cref="StreamProcessorId" />.</param>
        /// <param name="onAllTenants">The <see cref="IPerformActionOnAllTenants" />.</param>
        /// <param name="streamDefinition">The <see cref="IStreamDefinition" />.</param>
        /// <param name="getEventProcessor">The <see cref="Func{TResult}" /> that returns an <see cref="IEventProcessor" />.</param>
        /// <param name="unregister">An <see cref="Action" /> that unregisters the <see cref="ScopedStreamProcessor" />.</param>
        /// <param name="getScopedStreamProcessorsCreator">The <see cref="ICreateScopedStreamProcessors" />.</param>
        /// <param name="executionContextManager">The <see cref="IExecutionContextManager" />.</param>
        /// <param name="logger">The <see cref="ILogger" />.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
        public StreamProcessor(
            StreamProcessorId streamProcessorId,
            IPerformActionOnAllTenants onAllTenants,
            IStreamDefinition streamDefinition,
            FactoryFor<IEventProcessor> getEventProcessor,
            Action unregister,
            FactoryFor<ICreateScopedStreamProcessors> getScopedStreamProcessorsCreator,
            IExecutionContextManager executionContextManager,
            ILogger<StreamProcessor> logger,
            CancellationToken cancellationToken)
        {
            _identifier = streamProcessorId;
            _onAllTenants = onAllTenants;
            _streamDefinition = streamDefinition;
            _getEventProcessor = getEventProcessor;
            _unregister = unregister;
            _getScopedStreamProcessorsCreator = getScopedStreamProcessorsCreator;
            _executionContextManager = executionContextManager;
            _logger = logger;
            _stopAllScopedStreamProcessorsTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        /// <summary>
        /// Gets the <see cref="AbstractScopedStreamProcessor"/> per <see cref="TenantId"/>.
        /// </summary>
        public IDictionary<TenantId, AbstractScopedStreamProcessor> StreamProcessorsPerTenant { get; } = new Dictionary<TenantId, AbstractScopedStreamProcessor>();

        /// <summary>
        /// Initializes the stream processor.
        /// </summary>
        /// <returns>A <see cref="Task" />that represents the asynchronous operation.</returns>
        public async Task Initialize()
        {
            _logger.InitializingStreamProcessor(_identifier);

            _stopAllScopedStreamProcessorsTokenSource.Token.ThrowIfCancellationRequested();
            if (_initialized)
            {
                throw new StreamProcessorAlreadyInitialized(_identifier);
            }
            _initialized = true;

            await _onAllTenants.PerformAsync(async tenant =>
                {
                    var scopedStreamProcessorsCreators = _getScopedStreamProcessorsCreator();
                    var scopedStreamProcessor = await scopedStreamProcessorsCreators.Create(
                        _streamDefinition,
                        _identifier,
                        _getEventProcessor(),
                        _stopAllScopedStreamProcessorsTokenSource.Token).ConfigureAwait(false);
                    StreamProcessorsPerTenant.Add(tenant, scopedStreamProcessor);
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the stream processing for all tenants.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Start()
        {
            _logger.StartingStreamProcessor(_identifier);

            if (!_initialized)
            {
                throw new StreamProcessorNotInitialized(_identifier);
            }

            if (_started)
            {
                throw new StreamProcessorAlreadyProcessingStream(_identifier);
            }

            _started = true;
            try
            {
                var tasks = StartScopedStreamProcessors(_stopAllScopedStreamProcessorsTokenSource.Token);
                await Task.WhenAny(tasks).ConfigureAwait(false);
                if (TryGetException(tasks, out var ex))
                {
                    _logger.ScopedStreamProcessorFailed(ex, _identifier);
                }

                _stopAllScopedStreamProcessorsTokenSource.Cancel();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                _unregister();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed state.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _stopAllScopedStreamProcessorsTokenSource.Cancel();
                _stopAllScopedStreamProcessorsTokenSource.Dispose();

                if (!_started)
                {
                    _unregister();
                }
            }

            _disposed = true;
        }

        IEnumerable<Task> StartScopedStreamProcessors(CancellationToken cancellationToken) => StreamProcessorsPerTenant.Select(
            _ => Task.Run(async () =>
                {
                    (var tenant, var streamProcessor) = _;
                    _executionContextManager.CurrentFor(tenant);
                    await streamProcessor.Start(cancellationToken).ConfigureAwait(false);
                }, cancellationToken)).ToList();

        static bool TryGetException(IEnumerable<Task> tasks, out Exception exception)
        {
            exception = tasks.FirstOrDefault(_ => _.Exception != default)?.Exception;
            if (exception != default)
            {
                while (exception.InnerException != null) exception = exception.InnerException;
            }

            return exception != default;
        }
    }
}
