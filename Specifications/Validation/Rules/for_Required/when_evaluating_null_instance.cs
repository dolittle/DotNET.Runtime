﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dolittle.Rules;
using Dolittle.Validation.Rules;
using Machine.Specifications;
using Moq;
using It = Machine.Specifications.It;

namespace Dolittle.Specs.Validation.Rules.for_Required
{
    public class when_evaluating_null_instance
    {
        static Required rule;
        static Mock<IRuleContext> rule_context_mock;

        Establish context = () =>
        {
            rule = new Required(null);
            rule_context_mock = new Mock<IRuleContext>();
        };

        Because of = () => rule.Evaluate(rule_context_mock.Object, null);

        It should_fail_with_value_is_null_as_reason = () => rule_context_mock.Verify(r => r.Fail(rule, null, Moq.It.Is<Cause>(_ => _.Reason == Required.ValueIsNull)), Moq.Times.Once());
    }
}
