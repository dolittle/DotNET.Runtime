// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Machine.Specifications;

namespace Dolittle.Runtime.Events.Processing.for_ProcessingResult
{
    public class when_creating_succeeded_processing_result
    {
        static SucceededProcessingResult result;

        Because of = () => result = new SucceededProcessingResult();

        It should_have_succeeded_result_value = () => result.Value.ShouldEqual(ProcessingResultValue.Succeeded);
    }
}