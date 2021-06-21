// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReverseCallDispatcherType = Dolittle.Runtime.Services.IReverseCallDispatcher<
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerClientToRuntimeMessage,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerRuntimeToClientMessage,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerRegistrationRequest,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerRegistrationResponse,
                                    Dolittle.Runtime.Events.Processing.Contracts.HandleEventRequest,
                                    Dolittle.Runtime.Events.Processing.Contracts.EventHandlerResponse>;

namespace Dolittle.Runtime.Events.Processing.EventHandlers
{
    /// <summary>
    /// Defines a system for holding all current event
    /// </summary>
    public interface IEventHandlers
    {
        /// <summary>
        /// Gets all the registered event handlers for a specific.
        /// </summary>
        IEnumerable<EventHandler> All { get; }

        /// <summary>
        /// Register an <see cref="EventHandler"/>.
        /// </summary>
        /// <param name="dispatcher">The <see cref="ReverseCallDispatcherType"/> for calling back to the client.</param>
        /// <param name="arguments">The incoming <see cref="EventHandlerRegistrationArguments">arguments</see>.</param>
        /// <param name="cancellationToken">Cancellation token that can cancel the hierarchy.</param>
        Task<EventHandler> Register(ReverseCallDispatcherType dispatcher, EventHandlerRegistrationArguments arguments, CancellationToken cancellationToken);

        /// <summary>
        /// Register an <see cref="EventHandler"/>.
        /// </summary>
        /// <param name="eventHandler"><see cref="EventHandler"/> to register.</param>
        void Unregister(EventHandler eventHandler);
    }
}
