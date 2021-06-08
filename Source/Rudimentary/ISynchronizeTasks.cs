// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dolittle.Runtime.Rudimentary
{
    public interface ISynchronizeTasks
    {
        /// <summary>
        /// Synchronizes the running tasks by waiting for one of them to complete, run the pre synchronize task
        /// and then wait for all the tasks to complete.
        /// </summary>
        /// <remarks>
        /// If any of the tasks failed with an <see cref="Exception" />, this method will throw that <see cref="Exception" />.
        /// </remarks>
        /// <param name="tasks">The running tasks that needs to be synchronized.</param>
        /// <param name="preSynchronize">The callback that will be executed after one of the tasks complete.</param>
        /// <returns>A task representing the operation.</returns>
        Task SynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Func<Task> preSynchronize);

        /// <summary>
        /// Synchronizes the running tasks by waiting for one of them to complete, run the pre synchronize task
        /// and then wait for all the tasks to complete.
        /// </summary>
        /// <remarks>
        /// If any of the tasks failed with an <see cref="Exception" />, this method will throw that <see cref="Exception" />.
        /// </remarks>
        /// <param name="tasks">The running tasks that needs to be synchronized.</param>
        /// <param name="preSynchronize">The callback that will be executed after one of the tasks complete.</param>
        /// <returns>A task representing the operation.</returns>
        Task SynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Action preSynchronize);

        /// <summary>
        /// Synchronizes the running tasks by waiting for one of them to complete, cancel the <see cref="CancellationToken" /> used for creating the tasks
        /// and then wait for all the tasks to complete.
        /// </summary>
        /// <remarks>
        /// If any of the tasks failed with an <see cref="Exception" />, this method will throw that <see cref="Exception" />.
        /// </remarks>
        /// <param name="createTasks">A <see cref="Func{T, TResult}" /> that takes in an internall managed <see cref="CancellationToken" /> that returns the tasks to be synchronized.</param>
        /// <returns>A task representing the operation.</returns>
        Task SynchronizeWhenAnyComplete(Func<CancellationToken, IEnumerable<Task>> createTasks);

        /// <summary>
        /// Synchronizes the running tasks by waiting for one of them to complete, run the pre synchronize task
        /// and then wait for all the tasks to complete.
        /// </summary>
        /// <remarks>
        /// If any of the tasks failed with an <see cref="Exception" />, this method will throw that <see cref="Exception" />.
        /// </remarks>
        /// <param name="tasks">The running tasks that needs to be synchronized.</param>
        /// <param name="preSynchronize">The callback that will be executed after one of the tasks complete.</param>
        /// <returns>A task representing the operation.</returns>
        Task<Try<bool>> TrySynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Func<Task> preSynchronize);

        /// <summary>
        /// Synchronizes the running tasks by waiting for one of them to complete, run the pre synchronize task
        /// and then wait for all the tasks to complete.
        /// </summary>
        /// <remarks>
        /// If any of the tasks failed with an <see cref="Exception" />, this method will throw that <see cref="Exception" />.
        /// </remarks>
        /// <param name="tasks">The running tasks that needs to be synchronized.</param>
        /// <param name="preSynchronize">The callback that will be executed after one of the tasks complete.</param>
        /// <returns>A task representing the operation.</returns>
        Task<Try<bool>> TrySynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Action preSynchronize);

        /// <summary>
        /// Synchronizes the running tasks by waiting for one of them to complete, cancel the <see cref="CancellationToken" /> used for creating the tasks
        /// and then wait for all the tasks to complete.
        /// </summary>
        /// <remarks>
        /// If any of the tasks failed with an <see cref="Exception" />, this method will throw that <see cref="Exception" />.
        /// </remarks>
        /// <param name="createTasks">A <see cref="Func{T, TResult}" /> that takes in an internall managed <see cref="CancellationToken" /> that returns the tasks to be synchronized.</param>
        /// <returns>A task representing the operation.</returns>
        Task<Try<bool>> TrySynchronizeWhenAnyComplete(Func<CancellationToken, IEnumerable<Task>> createTasks);
    }
}
