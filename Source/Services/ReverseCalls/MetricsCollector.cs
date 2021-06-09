// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Dolittle.Runtime.Lifecycle;
using Dolittle.Runtime.Metrics;
using Prometheus;

namespace Dolittle.Runtime.Services.ReverseCalls
{
    /// <summary>
    /// Represents an implementatino of <see cref="IMetricsCollector"/>.
    /// </summary>
    [Singleton]
    public class MetricsCollector : ICanProvideMetrics, IMetricsCollector
    {
        Counter _totalStreamWriteTime;
        Counter _totalStreamWriteWaitTime;
        Counter _totalFirstMessageWaitTime;
        Gauge _currentPendingStreamWrites;
        Counter _totalKeepaliveResets;
        Counter _totalKeepaliveTimeouts;
        Counter _totalPingsSent;
        Counter _totalPongsReceived;
        Counter _totalStreamWrites;
        Counter _totalStreamWriteBytes;
        Counter _totalStreamReads;
        Counter _totalStreamReadBytes;

        /// <inheritdoc/>
        public IEnumerable<Collector> Provide(IMetricFactory metricFactory)
        {
            _totalStreamWriteTime = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_stream_write_seconds_total",
                "ReverseCall total time spent writing to streams");

            _totalStreamWriteWaitTime = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_stream_write_wait_seconds_total",
                "ReverseCall total time spent waiting to write to streams");

            _totalFirstMessageWaitTime = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_first_message_wait_seconds_total",
                "ReverseCall total time spent waiting for first message");

            _currentPendingStreamWrites = metricFactory.Gauge(
                "dolittle_system_runtime_services_reversecalls_pending_writes",
                "ReverseCall current pending stream writes waiting");

            _totalKeepaliveResets = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_keepalive_resets_total",
                "ReverseCall total number of times ping keepalive tokens have been reset");

            _totalKeepaliveTimeouts = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_keepalive_timeouts_total",
                "ReverseCall total number of times ping keepalive tokens have timed out");

            _totalPingsSent = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_pings_sent_total",
                "ReverseCall total number of pings sent");

            _totalPongsReceived = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_pongs_received_total",
                "ReverseCall total number of pongs received");

            _totalStreamWrites = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_stream_writes_total",
                "ReverseCall total number of writes to streams");

            _totalStreamWriteBytes = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_stream_write_bytes_total",
                "ReverseCall total number of bytes written to streams");

            _totalStreamReads = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_stream_reads_total",
                "ReverseCall total number of reads from streams");

            _totalStreamReadBytes = metricFactory.Counter(
                "dolittle_system_runtime_services_reversecalls_stream_read_bytes_total",
                "ReverseCall total number of bytes read from streams");
 
            return new Collector[]
            {
                _totalStreamWriteTime,
                _totalStreamWriteWaitTime,
                _totalFirstMessageWaitTime,
                _currentPendingStreamWrites,
                _totalKeepaliveResets,
                _totalKeepaliveTimeouts,
                _totalPingsSent,
                _totalPongsReceived,
                _totalStreamWrites,
                _totalStreamWriteBytes,
                _totalStreamReads,
                _totalStreamReadBytes
            };
        }

        /// <inheritdoc/>
        public void AddToTotalStreamWriteTime(TimeSpan writeTime)
            => _totalStreamWriteTime.Inc(writeTime.TotalSeconds);

        /// <inheritdoc/>
        public void AddToTotalStreamWriteWaitTime(TimeSpan waitTime)
            => _totalStreamWriteWaitTime.Inc(waitTime.TotalSeconds);

        /// <inheritdoc/>
        public void AddToTotalWaitForFirstMessageTime(TimeSpan waitTime)
            => _totalFirstMessageWaitTime.Inc(waitTime.TotalSeconds);

        /// <inheritdoc/>
        public void IncrementPendingStreamWrites()
            => _currentPendingStreamWrites.Inc();

        /// <inheritdoc/>
        public void DecrementPendingStreamWrites()
            => _currentPendingStreamWrites.Dec();

        /// <inheritdoc/>
        public void IncrementTotalKeepaliveTokenResets()
            => _totalKeepaliveResets.Inc();

        /// <inheritdoc/>
        public void IncrementTotalKeepaliveTimeouts()
            => _totalKeepaliveTimeouts.Inc();

        /// <inheritdoc/>
        public void IncrementTotalPingsSent()
            => _totalPingsSent.Inc();

        /// <inheritdoc/>
        public void IncrementTotalPongsReceived()
            => _totalPongsReceived.Inc();

        /// <inheritdoc/>
        public void IncrementTotalStreamReads()
            => _totalStreamReads.Inc();

        /// <inheritdoc/>
        public void IncrementTotalStreamReadBytes(int writtenBytes)
            => _totalStreamReadBytes.Inc(writtenBytes);

        /// <inheritdoc/>
        public void IncrementTotalStreamWrites()
            => _totalStreamWrites.Inc();

        /// <inheritdoc/>
        public void IncrementTotalStreamWriteBytes(int writtenBytes)
            => _totalStreamWriteBytes.Inc();
    }
}
