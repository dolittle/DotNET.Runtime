// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dolittle.Runtime.Rudimentary
{
    /// <summary>
    /// Represents an implementation <see cref="ISynchronizeTasks" />.
    /// </summary>
    public class SynchronizeTasks : ISynchronizeTasks
    {
        /// <inheritdoc/>
        public async Task SynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Func<Task> preSynchronize)
        {
            await Task.WhenAny(tasks).ConfigureAwait(false);
            await preSynchronize().ConfigureAwait(false);
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task SynchronizeWhenAnyComplete(Func<CancellationToken, IEnumerable<Task>> createTasks)
        {
            using var cts = new CancellationTokenSource();
            return SynchronizeWhenAnyComplete(createTasks(cts.Token), () =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            });
        }

        public Task SynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Action preSynchronize)
            => SynchronizeWhenAnyComplete(tasks, () =>
            {
                preSynchronize();
                return Task.CompletedTask;
            });

        /// <inheritdoc/>
        public async Task<Try<bool>> TrySynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Func<Task> preSynchronize)
        {
            try
            {
                await SynchronizeWhenAnyComplete(tasks, preSynchronize).ConfigureAwait(false);
                return new Try<bool>(true, true);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <inheritdoc/>
        public async Task<Try<bool>> TrySynchronizeWhenAnyComplete(Func<CancellationToken, IEnumerable<Task>> createTasks)
        {
            try
            {
                await SynchronizeWhenAnyComplete(createTasks).ConfigureAwait(false);
                return new Try<bool>(true, true);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public Task<Try<bool>> TrySynchronizeWhenAnyComplete(IEnumerable<Task> tasks, Action preSynchronize)
            => TrySynchronizeWhenAnyComplete(tasks, () =>
            {
                preSynchronize();
                return Task.CompletedTask;
            });
    }
}