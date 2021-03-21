// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Execution;
using Microsoft.Extensions.Logging;
using Dolittle.Runtime.Protobuf;
using Dolittle.Services.Contracts;
using Google.Protobuf;
using Grpc.Core;

namespace Dolittle.Runtime.Services
{
    /// <summary>
    /// Represents an implementation of <see cref="IReverseCallDispatcher{TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse}"/>.
    /// </summary>
    /// <typeparam name="TClientMessage">Type of the <see cref="IMessage">messages</see> that is sent from the client to the server.</typeparam>
    /// <typeparam name="TServerMessage">Type of the <see cref="IMessage">messages</see> that is sent from the server to the client.</typeparam>
    /// <typeparam name="TConnectArguments">Type of the arguments that are sent along with the initial Connect call.</typeparam>
    /// <typeparam name="TConnectResponse">Type of the response that is received after the initial Connect call.</typeparam>
    /// <typeparam name="TRequest">Type of the requests sent from the server to the client using <see cref="IReverseCallDispatcher{TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse}.Call"/>.</typeparam>
    /// <typeparam name="TResponse">Type of the responses received from the client using <see cref="IReverseCallDispatcher{TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse}.Call"/>.</typeparam>
    public class ReverseCallDispatcher<TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse>
        : IDisposable, IReverseCallDispatcher<TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse>
        where TClientMessage : IMessage, new()
        where TServerMessage : IMessage, new()
        where TConnectArguments : class
        where TConnectResponse : class
        where TRequest : class
        where TResponse : class
    {
        readonly SemaphoreSlim _writeSemaphore = new(1);
        readonly ConcurrentDictionary<ReverseCallId, TaskCompletionSource<TResponse>> _calls = new();
        readonly IAsyncStreamReader<TClientMessage> _clientStream;
        readonly IServerStreamWriter<TServerMessage> _serverStream;
        readonly ServerCallContext _context;
        readonly Func<TClientMessage, TConnectArguments> _getConnectArguments;
        readonly Action<TServerMessage, TConnectResponse> _setConnectResponse;
        readonly Action<TServerMessage, TRequest> _setMessageRequest;
        readonly Func<TClientMessage, TResponse> _getMessageResponse;
        readonly Func<TConnectArguments, ReverseCallArgumentsContext> _getArgumentsContext;
        readonly Action<TRequest, ReverseCallRequestContext> _setRequestContext;
        readonly Func<TResponse, ReverseCallResponseContext> _getResponseContex;
        readonly Action<TServerMessage, Ping> _setPing;
        readonly Func<TClientMessage, Pong> _getPong;
        readonly IExecutionContextManager _executionContextManager;
        readonly ILogger _logger;

        readonly object _receiveArgumentsLock = new();
        readonly object _respondLock = new();
        TimeSpan _pingInterval = TimeSpan.FromSeconds(5);
        bool _completed;
        bool _disposed;

        bool _receivingArguments;
        bool _accepted;
        bool _rejected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReverseCallDispatcher{TClientMessage, TServerMessage, TConnectArguments, TConnectResponse, TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="clientStream">The <see cref="IAsyncStreamReader{TClientMessage}"/> to read client messages from.</param>
        /// <param name="serverStream">The <see cref="IServerStreamWriter{TServerMessage}"/> to write server messages to.</param>
        /// <param name="context">The connection <see cref="ServerCallContext">context</see>.</param>
        /// <param name="getConnectArguments">A delegate to get the <typeparamref name="TConnectArguments"/> from a <typeparamref name="TClientMessage"/>.</param>
        /// <param name="setConnectResponse">A delegate to set the <typeparamref name="TConnectResponse"/> on a <typeparamref name="TServerMessage"/>.</param>
        /// <param name="setMessageRequest">A delegate to set the <typeparamref name="TRequest"/> on a <typeparamref name="TServerMessage"/>.</param>
        /// <param name="getMessageResponse">A delegate to get the <typeparamref name="TResponse"/> from a <typeparamref name="TClientMessage"/>.</param>
        /// <param name="getArgumentsContext">A delegate to get the <see cref="ReverseCallArgumentsContext"/> from a <typeparamref name="TConnectArguments"/>.</param>
        /// <param name="setRequestContext">A delegate to set the <see cref="ReverseCallRequestContext"/> on a <typeparamref name="TRequest"/>.</param>
        /// <param name="getResponseContex">A delegate to get the <see cref="ReverseCallResponseContext"/> from a <typeparamref name="TResponse"/>.</param>
        /// <param name="setPing">A delegate to set the <see cref="Ping" /> on a <typeparamref name="TServerMessage" />.</param>
        /// <param name="getPong">A delegate to get the <see cref="Pong" /> from a <typeparamref name="TClientMessage" />.</param>
        /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/> to use.</param>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        public ReverseCallDispatcher(
            IAsyncStreamReader<TClientMessage> clientStream,
            IServerStreamWriter<TServerMessage> serverStream,
            ServerCallContext context,
            Func<TClientMessage, TConnectArguments> getConnectArguments,
            Action<TServerMessage, TConnectResponse> setConnectResponse,
            Action<TServerMessage, TRequest> setMessageRequest,
            Func<TClientMessage, TResponse> getMessageResponse,
            Func<TConnectArguments, ReverseCallArgumentsContext> getArgumentsContext,
            Action<TRequest, ReverseCallRequestContext> setRequestContext,
            Func<TResponse, ReverseCallResponseContext> getResponseContex,
            Action<TServerMessage, Ping> setPing,
            Func<TClientMessage, Pong> getPong,
            IExecutionContextManager executionContextManager,
            ILogger logger)
        {
            _clientStream = clientStream;
            _serverStream = serverStream;
            _context = context;
            _getConnectArguments = getConnectArguments;
            _setConnectResponse = setConnectResponse;
            _setMessageRequest = setMessageRequest;
            _getMessageResponse = getMessageResponse;
            _getArgumentsContext = getArgumentsContext;
            _setRequestContext = setRequestContext;
            _getResponseContex = getResponseContex;
            _setPing = setPing;
            _getPong = getPong;
            _executionContextManager = executionContextManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public TConnectArguments Arguments { get; private set; }

        /// <inheritdoc/>
        public async Task<bool> ReceiveArguments(CancellationToken cancellationToken)
        {
            ThrowIfReceivingArguments();
            lock (_receiveArgumentsLock)
            {
                ThrowIfReceivingArguments();
                _receivingArguments = true;
            }

            if (await _clientStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var arguments = _getConnectArguments(_clientStream.Current);
                if (arguments != null)
                {
                    var callContext = _getArgumentsContext(arguments);
                    if (callContext?.PingInterval == null)
                    {
                        _logger.LogWarning("Received arguments, but ping interval was not set");
                        return false;
                    }

                    var interval = callContext.PingInterval.ToTimeSpan();
                    if (interval.TotalMilliseconds <= 0)
                    {
                        _logger.LogWarning("Received arguments, but ping interval is less than or equal to zero milliseconds");
                        return false;
                    }

                    _pingInterval = callContext.PingInterval.ToTimeSpan();

                    if (callContext?.ExecutionContext != null)
                    {
                        _executionContextManager.CurrentFor(callContext.ExecutionContext.ToExecutionContext());
                        Arguments = arguments;
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Received arguments, but call execution context was not set.");
                    }
                }
                else
                {
                    _logger.LogWarning("Received initial message from client, but arguments was not set.");
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task Accept(TConnectResponse response, CancellationToken cancellationToken)
        {
            ThrowIfResponded();
            lock (_respondLock)
            {
                ThrowIfResponded();
                _accepted = true;
            }

            var message = new TServerMessage();
            _setConnectResponse(message, response);
            await _serverStream.WriteAsync(message).ConfigureAwait(false);
            _ = Task.Run(() => StartPinging(cancellationToken));
            await HandleClientMessages(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task Reject(TConnectResponse response, CancellationToken cancellationToken)
        {
            ThrowIfResponded();
            lock (_respondLock)
            {
                ThrowIfResponded();
                _rejected = true;
            }

            var message = new TServerMessage();
            _setConnectResponse(message, response);
            return _serverStream.WriteAsync(message);
        }

        /// <inheritdoc/>
        public async Task<TResponse> Call(TRequest request, CancellationToken cancellationToken)
        {
            ThrowIfCompletedCall();

            var completionSource = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callId = ReverseCallId.New();
            while (!_calls.TryAdd(callId, completionSource))
            {
                callId = ReverseCallId.New();
            }

            try
            {
                await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var callContext = new ReverseCallRequestContext
                    {
                        CallId = callId.ToProtobuf(),
                        ExecutionContext = _executionContextManager.Current.ToProtobuf(),
                    };
                    _setRequestContext(request, callContext);

                    var message = new TServerMessage();
                    _setMessageRequest(message, request);
                    _logger.LogTrace("Writing request with CallId: {CallId}", callId);
                    _logger.WritingRequest(callId);
                    await _serverStream.WriteAsync(message).ConfigureAwait(false);
                }
                finally
                {
                    _writeSemaphore.Release();
                }

                return await completionSource.Task.ConfigureAwait(false);
            }
            catch
            {
                _calls.TryRemove(callId, out _);
                throw;
            }
        }

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
            if (!_disposed)
            {
                if (disposing)
                {
                    _writeSemaphore.Dispose();
                }

                _disposed = true;
            }
        }

        async Task StartPinging(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_pingInterval).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogDebug("Stopping pinging");
                        return;
                    }

                    await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        var message = new TServerMessage();
                        _setPing(message, new Ping());
                        _logger.LogTrace("Writing ping");
                        await _serverStream.WriteAsync(message).ConfigureAwait(false);
                    }
                    finally
                    {
                        _writeSemaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "An error occurred while pinging");
                }
            }
        }

        async Task HandleClientMessages(CancellationToken cancellationToken)
        {
            using var jointCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            jointCts.CancelAfter(_pingInterval.Multiply(3));
            try
            {
                while (!jointCts.IsCancellationRequested && await _clientStream.MoveNext(jointCts.Token).ConfigureAwait(false))
                {
                    var message = _clientStream.Current;
                    var pong = _getPong(message);
                    var response = _getMessageResponse(_clientStream.Current);
                    if (pong != null)
                    {
                        _logger.LogTrace("Received pong");
                    }
                    else if (response != null)
                    {
                        _logger.LogTrace("Received response");
                        var callContext = _getResponseContex(response);
                        if (callContext?.CallId != null)
                        {
                            ReverseCallId callId = callContext.CallId.ToGuid();
                            if (_calls.TryRemove(callId, out var completionSource))
                            {
                                completionSource.SetResult(response);
                            }
                            else
                            {
                                _logger.LogWarning("Could not find the call id from the received response from the client. The message will be ignored.");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Received response from reverse call client, but the call context was not set.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Received message from reverse call client, but it did not contain a response.");
                    }

                    jointCts.CancelAfter(_pingInterval.Multiply(3));
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "An error occurred during handling of client messages");
                }
            }
            finally
            {
                _completed = true;
                if (jointCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Ping timed out");
                }

                foreach ((_, var completionSource) in _calls)
                {
                    try
                    {
                        completionSource.SetCanceled();
                    }
                    catch
                    {
                    }
                }
            }
        }

        void ThrowIfReceivingArguments()
        {
            if (_receivingArguments)
            {
                throw new ReverseCallDispatcherAlreadyTriedToReceiveArguments();
            }
        }

        void ThrowIfResponded()
        {
            if (_accepted)
            {
                throw new ReverseCallDispatcherAlreadyAccepted();
            }
            else if (_rejected)
            {
                throw new ReverseCallDispatcherAlreadyRejected();
            }
        }

        void ThrowIfCompletedCall()
        {
            if (_completed)
            {
                throw new CannotPerformCallOnCompletedReverseCallConnection();
            }
        }
    }
}
