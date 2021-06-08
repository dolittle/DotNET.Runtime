using System.Threading;
// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Machine.Specifications;

namespace Dolittle.Runtime.Rudimentary.for_SynchronizeTasks.when_synchronizing.with_tasks
{
    public class and_tasks_do_not_finish
    {
        static IEnumerable<Task> tasks;
        static CancellationTokenSource cts;
        static Action pre_synchronize = () => cts.Cancel();

        Establish context = () =>
        {
            var first_tcs = new TaskCompletionSource();
            var second_tcs = new TaskCompletionSource();
            cts.Token.Register(() =>
            {
            tcs.SetResult());
            tasks = new[] {

            };
        };
        Because of = () => new SynchronizeTasks().SynchronizeWhenAnyComplete(tasks)


    }
}