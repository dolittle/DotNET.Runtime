// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Processing.Filters;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Events.Store.Streams;
using Dolittle.Runtime.Protobuf;
using Dolittle.Runtime.Rudimentary;
using Dolittle.Runtime.Services;
using Google.Protobuf;

namespace Dolittle.Runtime.Events.Processing.EventProcessors
{
    /// <summary>
    /// Represents the base class for event processors.
    /// </summary>
    /// <typeparam name="TClientMessage">Type of the <see cref="IMessage">messages</see> that is sent from the client to the server.</typeparam>
    /// <typeparam name="TServerMessage">Type of the <see cref="IMessage">messages</see> that is sent from the server to the client.</typeparam>
    /// <typeparam name="TConnectArguments">Type of the arguments that are sent along with the initial Connect call.</typeparam>
    /// <typeparam name="TConnectResponse">Type of the response that is received after the initial Connect call.</typeparam>
    /// <typeparam name="TRequest">Type of the requests sent from the server to the client using <see cref="IReverseCallDispatcher{TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse}.Call"/>.</typeparam>
    /// <typeparam name="TResponse">Type of the responses received from the client using <see cref="IReverseCallDispatcher{TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse}.Call"/>.</typeparam>
    public abstract class EventProcessorBase<TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse, TRegistrationArguments> : IDisposable
        where TClientMessage : IMessage, new()
        where TServerMessage : IMessage, new()
        where TConnectArguments : class
        where TConnectResponse : class
        where TRequest : class
        where TResponse : class
        where TRegistrationArguments : IEventProcessorRegistrationArgument
    {
        readonly IStreamProcessors _streamProcessors;
        readonly TRegistrationArguments _arguments;
        readonly CancellationTokenSource _cancellationTokenSource;
        bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="EventHandler"/>.
        /// </summary>
        /// <param name="streamProcessors">The <see cref="IStreamProcessors" />.</param>
        /// <param name="filterValidationForAllTenants">The <see cref="IValidateFilterForAllTenants" /> for validating the filter definition.</param>
        /// <param name="streamDefinitions">The<see cref="IStreamDefinitions" />.</param>
        /// <param name="dispatcher">The reverse call dispatcher.</param>
        /// <param name="arguments">Connecting arguments.</param>
        /// <param name="getEventsToStreamsWriter">Factory for getting <see cref="IWriteEventsToStreams"/>.</param>
        /// <param name="loggerFactory">Logger factory for logging.</param>
        /// <param name="cancellationToken">Cancellation token that can cancel the hierarchy.</param>
        public EventProcessorBase(
            IStreamProcessors streamProcessors,
            IReverseCallDispatcher<TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse> dispatcher,
            TRegistrationArguments arguments,
            CancellationToken cancellationToken)
        {
            _streamProcessors = streamProcessors;
            Dispatcher = dispatcher;
            _arguments = arguments;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken = _cancellationTokenSource.Token;
        }

        /// <summary>
        /// Gets the <see cref="Scope"/> for the <see cref="EventHandler"/>.
        /// </summary>
        public ScopeId Scope => _arguments.Scope.Value;

        /// <summary>
        /// Gets the <see cref="EventProcessorId"/> for the <see cref="EventHandler"/>.
        /// </summary>
        public EventProcessorId EventProcessor => _arguments.EventProcessor;

        /// <summary>
        /// Gets the value indicating whether the stream processors has been fully registered. 
        /// </summary>
        /// <value></value>
        public bool Registered { get; private set; }

        protected IReverseCallDispatcher<TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse> Dispatcher { get; }

        protected CancellationToken CancellationToken { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Register and start the event handler for filtering and processing.
        /// </summary>
        /// <returns>Async <see cref="Task"/>.</returns>
        public abstract Task RegisterAndStart();

        /// <summary>
        /// Creates a connect response.
        /// </summary>
        /// <returns>The <typeparamref name="TConnectResponse"/>.</returns>
        protected abstract TConnectResponse CreateConnectResponse();

        /// <summary>
        /// Creates a failed connect response.
        /// </summary>
        /// <param name="failure">The failure.</param>
        /// <returns>The <typeparamref name="TConnectResponse"/>.</returns>
        protected abstract TConnectResponse CreateConnectResponse(Failure failure);

        protected async Task<bool> RegisterStreamProcessor(
            IStreamDefinition streamDefinition,
            FactoryFor<IEventProcessor> getProcessor,
            Func<Exception, Failure> onException,
            Action<StreamProcessor> onStreamProcessor)
        {
            var streamProcessor = _streamProcessors.TryCreateAndRegister(
                Scope,
                EventProcessor,
                streamDefinition,
                getProcessor,
                _cancellationTokenSource.Token);

            onStreamProcessor(streamProcessor);

            if (!streamProcessor.Success)
            {
                await Fail(onException(streamProcessor.Exception)).ConfigureAwait(false);
            }

            return streamProcessor.Success;
        }

        protected async Task Start(
            Task runningDispatcher,
            Func<Task> preStart,
            Action<Exception> logOnErrorWhileStarting,
            Action<Exception> logOnErrorAfterStarted,
            params StreamProcessor[] streamProcessors)
        {
            var prepare = await TryPrepareStreamProcessors(preStart, streamProcessors).ConfigureAwait(false);
            if (!prepare.Success)
            {
                logOnErrorWhileStarting(prepare.Exception);
            }
            else
            {
                var tasks = streamProcessors.Select(_ => _.Start()).ToList();
                tasks.Add(runningDispatcher);
                await WaitForStartedStreamProcessors(tasks, logOnErrorAfterStarted).ConfigureAwait(false);
            }
        }


        protected Task Fail(FailureId failureId, FailureReason failureReason)
            => Fail(new(failureId, failureReason));

        protected Task Fail(Failure failure)
            => Dispatcher.Reject(CreateConnectResponse(failure), _cancellationTokenSource.Token);

        async Task<Try> TryPrepareStreamProcessors(Func<Task> preStart, IEnumerable<StreamProcessor> streamProcessors)
        {
            try
            {
                foreach (var streamProcessor in streamProcessors)
                {
                    await streamProcessor.Initialize().ConfigureAwait(false);
                }
                await preStart().ConfigureAwait(false);
                return Try.Succeeded();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        async Task WaitForStartedStreamProcessors(
            IEnumerable<Task> tasks,
            Action<Exception> logOnErrorAfterStarted)
        {
            try
            {
                await Task.WhenAny(tasks).ConfigureAwait(false);
                _cancellationTokenSource.Cancel();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logOnErrorAfterStarted(ex);
            }
        }
    }
}
