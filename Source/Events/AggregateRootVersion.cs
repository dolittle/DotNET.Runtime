﻿// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.Concepts;
using Dolittle.Events;

namespace Dolittle.Runtime.Events
{
    /// <summary>
    /// Represents a version of an <see cref="AggregateRoot"/> as a natural number, corresponding to the number of events the Aggregate Root has applied to an Event Source.
    /// </summary>
    public class AggregateRootVersion : ConceptAs<uint>
    {
        /// <summary>
        /// The inital version of an <see cref="AggregateRoot"/> that has applied no events.
        /// </summary>
        public static readonly AggregateRootVersion Initial = 0;

        /// <summary>
        /// Implicitly convert a <see cref="uint"/> to an <see cref="AggregateRootVersion"/>.
        /// </summary>
        /// <param name="number">The number.</param>
        public static implicit operator AggregateRootVersion(uint number) => new AggregateRootVersion { Value = number };
    }
}