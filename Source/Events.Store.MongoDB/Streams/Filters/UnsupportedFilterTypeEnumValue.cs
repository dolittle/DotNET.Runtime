// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dolittle.Runtime.Events.Store.MongoDB.Streams.Filters
{
    /// <summary>
    /// Exception that gets thrown when a <see cref="AbstractFilterDefinition"/> document has an unsupported Type field.
    /// </summary>
    public class UnsupportedFilterTypeEnumValue : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedFilterTypeEnumValue"/> class.
        /// </summary>
        /// <param name="filterType">string of the given FilterType.</param>
        /// <param name="id">Id of the document.</param>
        public UnsupportedFilterTypeEnumValue(FilterDefinitionDiscriminatorConvention.FilterType filterType, Guid id)
            : base($"Document id: {id} has an unsupported FilterType: {filterType}. Check supportd types from {typeof(FilterDefinitionDiscriminatorConvention)}.")
        {
        }
    }
}