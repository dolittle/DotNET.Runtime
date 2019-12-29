﻿// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dolittle.Runtime.Events.Migration.Specs.Fakes.v2;
using Machine.Specifications;

namespace Dolittle.Runtime.Events.Migration.Specs.for_EventMigrationHierarchy
{
    public class when_adding_the_same_migration_type_twice : given.an_initialized_event_migration_hierarchy
    {
        static Exception Exception;

        Because of = () =>
                         {
                             event_migration_hierarchy.AddMigrationLevel(typeof(SimpleEvent));
                             Exception = Catch.Exception(() => event_migration_hierarchy.AddMigrationLevel(typeof(SimpleEvent)));
                         };

        It should_throw_a_duplicate_in_event_migration_hierarchy_exception = () => Exception.ShouldBeOfExactType(typeof(DuplicateInEventMigrationHierarchy));
    }
}