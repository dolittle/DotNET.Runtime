// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dolittle.Applications;
using Dolittle.Artifacts;
using Dolittle.Execution;
using Dolittle.Tenancy;

namespace Dolittle.Runtime.Events.Processing
{
    public static class committed_events
    {
        public static Store.CommittedEvent single() => new Store.CommittedEvent(
                EventLogVersion.Initial,
                DateTimeOffset.UtcNow,
                EventSourceId.New(),
                CorrelationId.New(),
                Microservice.New(),
                TenantId.Development,
                new Cause(CauseType.Command, 0),
                new Artifact(ArtifactId.New(), ArtifactGeneration.First),
                "{\"something\":42}");
    }
}