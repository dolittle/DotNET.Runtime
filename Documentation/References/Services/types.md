---
title: Types
description: The definition of the common types shared between Runtime services
weight: 100
---

## Fundamental types

<div class="mermaid">
classDiagram
    class ReverseCallArgumentsContext{
        ExecutionContext executionContext
        Uuid headId
        Duration pingInterval
    }
    class ReverseCallRequestContext{
        Uuid callId
        ExecutionContext executionContext
    }
    class ReverseCallResponseContext{
        Uuid callId
    }
    class CallRequestContext{
        ExecutionContext executionContext
        Uuid headId
    }
    class ExecutionContext{
        Uuid microserviceId
        Uuid tenantId
        Version version
        Uuid correlationid
        Claim[] claims
        string environment
    }
    class Version{
        int32 major
        int32 minor
        int32 patch
        int32 build
        string preReleaseString
    }
    class Claim{
        string key
        string value
        string valueType
    }
    class Failure{
        Uuid id
        string reason
    }
    class Artifact{
        Uuid id
        uint32 generation
    }
    ReverseCallArgumentsContext --* ExecutionContext
    ReverseCallRequestContext --* ExecutionContext
    CallRequestContext --* ExecutionContext
    ExecutionContext --o Version
    ExecutionContext --o Claim
</div>

## Event processing types
<div class="mermaid">
classDiagram
    class ProcessorFailure {
        string reason
        bool retry
        Duration retryTimeout
    }
    class RetryProcessingState {
        string failureReason
        uint32 retryCount
    }
    class StreamEvent {
        CommittedEvent event
        Uuid partitionId
        Uuid scopeId
        bool partitioned
    }
    %%
    StreamEvent --* CommittedEvent
    %% links
    link CommittedEvent "{{< ref "private/event_store#message-types" >}}" "Event Store"
</div>
