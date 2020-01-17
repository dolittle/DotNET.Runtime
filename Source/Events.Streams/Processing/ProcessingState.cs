// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dolittle.Runtime.Events.Streams.Processing
{
    /// <summary>
    /// The state of an event processor.
    /// </summary>
    public enum ProcessingState
    {
        /// <summary>A null state of an event processor when it has been initialized.</summary>
        NullState = -1,

        /// <summary>The state of an event processor when it has successfully processed an event.</summary>
        Ok = 0,

        /// <summary>The state of an event processor when it has unsuccessfully processed an event and it has to stop processing.</summary>
        Stop,

        /// <summary>The state of an event processor when it has unsuccessfully processed an event, but it will retry later at some point later.</summary>
        Retry,

        /// <summary>The state of an event processor when it has skipped the processing of an event.</summary>
        Ignore
    }
}