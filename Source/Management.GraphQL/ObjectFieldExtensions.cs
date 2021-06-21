// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Dolittle.Runtime.Management.GraphQL
{
    /// <summary>
    /// Extension methods for <see cref="IObjectTypeDescriptor"/> and fields.
    /// </summary>
    public static class ObjectFieldExtensions
    {
        public static IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Add a sub field on a type, such as a Query / Mutation root object with a given name that will resolve to the given type.
        /// </summary>
        /// <param name="parent">Parent type (Query/Mutation).</param>
        /// <param name="name">Name of the field.</param>
        /// <typeparam name="T">Type that the field should resolve to.</typeparam>
        /// <returns><see cref="IObjectTypeDescriptor"/> for continuation.</returns>
        public static IObjectTypeDescriptor AddSubObject<T>(this IObjectTypeDescriptor parent, string name)
        {
            parent.Field(name).Type(typeof(EventHandlers.EventHandlers)).Resolve(_ => ServiceProvider.GetService<T>());
            return parent;
        }
    }
}