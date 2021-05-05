// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Embeddings.Store;
using Dolittle.Runtime.Events.Store;
using Dolittle.Runtime.Projections.Store.State;
using Dolittle.Runtime.Rudimentary;
using Machine.Specifications;

namespace Dolittle.Runtime.Embeddings.Processing.for_StateTransitionEventsCalculator.when_converging
{
    public class and_projecting_events_partially_succeeds : given.all_dependencies
    {
        static EmbeddingCurrentState current_state;
        static ProjectionState desired_state;
        static CouldNotProjectAllEvents exception;
        static UncommittedEvents events;

        Establish context = () =>
        {
            exception = new CouldNotProjectAllEvents(identifier);
            current_state = new EmbeddingCurrentState(0, EmbeddingCurrentStateType.CreatedFromInitialState, "current state", "");
            desired_state = "desired state";
            events = new UncommittedEvents(Array.Empty<UncommittedEvent>());
            embedding
                .Setup(_ => _.TryCompare(current_state, desired_state, cancellation))
                .Returns(Task.FromResult(Try<UncommittedEvents>.Succeeded(events)));
            loop_detector
                .Setup(_ => _.TryCheckEventLoops(new[] { events }))
                .Returns(Task.FromResult(Try<bool>.Succeeded(false)));
            project_many_events
                .Setup(_ => _.TryProject(current_state, events, cancellation))
                .Returns(Task.FromResult(Partial<EmbeddingCurrentState>.PartialSuccess(
                    new EmbeddingCurrentState(1, EmbeddingCurrentStateType.Persisted, "new state", ""),
                    exception)));
        };

        static Try<UncommittedAggregateEvents> result;
        Because of = () => result = calculator.TryConverge(current_state, desired_state, cancellation).GetAwaiter().GetResult();

        It should_return_a_failure = () => result.Success.ShouldBeFalse();
        It should_have_an_exception = () => result.HasException.ShouldBeTrue();
        It should_fail_because_detecting_loop_failed = () => result.Exception.ShouldEqual(exception);
        It should_only_compare_once = () => embedding.Verify(_ => _.TryCompare(current_state, desired_state, cancellation), Moq.Times.Once);
        It should_not_do_anything_more_with_embedding = () => embedding.VerifyNoOtherCalls();
        It should_project_events = () => project_many_events.Verify(_ => _.TryProject(current_state, events, cancellation), Moq.Times.Once);
        It should_not_project_anything_else = () => project_many_events.VerifyNoOtherCalls();
    }
}