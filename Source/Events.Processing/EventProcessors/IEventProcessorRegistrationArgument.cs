// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Execution;

namespace Dolittle.Runtime.Events.Processing.EventProcessors
{
    /// <summary>
    /// Defines the basis of the registration arguments for an event processor.
    /// </summary>
    public interface IEventProcessorRegistrationArgument
    {
        /// <summary>
        /// Gets the scope that the event processor should be registered in.
        /// </summary>
        ScopeId Scope { get; }

        /// <summary>
        /// Gets the event processor identifier.
        /// </summary>
        EventProcessorId EventProcessor { get; }

        /// <summary>
        /// Gets the execution context.
        /// </summary>
        ExecutionContext ExecutionContext { get; }
    }
}
