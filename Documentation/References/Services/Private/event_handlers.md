---
title: Event Handlers
description: The definition of the Runtime Event Handlers service
weight: 5
---

## Register Event Handler

Registers a callback-type event handler, to perform an action on specified event types from (all tenants) event log. The Runtime will call the client with events of the specified types to perform some action.

```mermaid
sequenceDiagram
    participant C as Client (SDK)
    participant R as Runtime
    C->>R: EventHandlerRegistrationRequest
    activate R
    alt If registration fails
        R->>C: EventHandlerRegistrationResponse (with Failure)
    else If registration succeeds
        R->>C: EventHandlerRegistrationResponse
        deactivate R
        loop For each event of the specified types in the event log
            R->>C: HandleEventRequest
            activate C
            C->>R: EventHandlerResponse
            deactivate C
        end
    end
```


### Registration message types

```mermaid
classDiagram
    class EventHandlerRegistrationRequest{
        ReverseCallArgumentsContext callContext
        Uuid scopeId
        Uuid eventHandlerId
        Artifact[] types
        bool partitioned
    }
    class EventHandlerRegistrationResponse{
        Failure failure
    }
    %%
    EventHandlerRegistrationRequest --* ReverseCallArgumentsContext
    EventHandlerRegistrationRequest --* Artifact
    EventHandlerRegistrationResponse --o Failure
    %% links
    link ReverseCallArgumentsContext "{{< ref "types" >}}" "Types"
    link Artifact "{{< ref "types" >}}" "Types"
    link Failure "{{< ref "types" >}}" "Types"
```

### Event message types

```mermaid
classDiagram
    class HandleEventRequest{
        ReverseCallRequestContext callContext
        StreamEvent event
        RetryProcessingState retryProcessingState
    }
    class EventHandlerResponse{
        ReverseCallResponseContext callContext
        ProcessorFailure failure
    }
    %%
    HandleEventRequest --* ReverseCallRequestContext
    HandleEventRequest --* StreamEvent
    HandleEventRequest --* RetryProcessingState
    EventHandlerResponse --* ReverseCallResponseContext
    EventHandlerResponse --* ProcessorFailure
    %% links
    link ReverseCallRequestContext "{{< ref "types" >}}" "Types"
    link ReverseCallResponseContext "{{< ref "types" >}}" "Types"
    link RetryProcessingState "{{< ref "types#event-processing-types" >}}" "Types"
    link ProcessorFailure "{{< ref "types#event-processing-types" >}}" "Types"
    link StreamEvent "{{< ref "types#event-processing-types" >}}" "Event Store"
```
