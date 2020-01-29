// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Machine.Specifications;
using It = Machine.Specifications.It;

namespace Dolittle.Runtime.TimeSeries.Identity.for_TimeSeriesMapIdentifier
{
    public class when_identifying_timeseries_for_non_existing_tag_in_existing_source
    {
        const string source = "MySource";
        const string tag = "MyTag";
        static Exception result;
        static TimeSeriesMapIdentifier identifier;

        Establish context = () =>
        {
            identifier = new TimeSeriesMapIdentifier(new TimeSeriesMap(
                new Dictionary<Source, TimeSeriesByTag>
                {
                    { source, new TimeSeriesByTag(new Dictionary<Tag, TimeSeriesId>()) }
                }));
        };

        Because of = () => result = Catch.Exception(() => identifier.Identify(source, tag));

        It should_throw_missing_tag_in_system = () => result.ShouldBeOfExactType<MissingTagInSource>();
    }
}