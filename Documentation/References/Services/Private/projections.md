---
title: Projections
description: The definition of the Runtime Projections service
weight: 20
---

## Register Projection

Registers a callback-type projection, to build a read model from specified event types from (all tenants) event log. The Runtime will call the client with events of the specified types and the current read model state. The client returns the updated state, that will be stored in the projection store.

```mermaid
sequenceDiagram
    participant C as Client (SDK)
    participant R as Runtime
    C->>R: ProjectionRegistrationRequest
    activate R
    alt If registration fails
        R->>C: ProjectionRegistrationResponse (with Failure)
    else If registration succeeds
        R->>C: ProjectionRegistrationResponse
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
    class ProjectionRegistrationRequest{
        ReverseCallArgumentsContext callContext
        Uuid scopeId
        Uuid projectionId
        ProjectionEventSelector[] events
    }
    class ProjectionRegistrationResponse{
        Failure failure
    }
    class ProjectionEventSelector{
        <<abstract>>
        Artifact eventType
    }
    class EventSourceIdKeySelector{  
    }
    class PartitionIdKeySelector{
    }
    class EventPropertyKeySelector{
        string propertyName
    }
    %%
    ProjectionRegistrationRequest --* ReverseCallArgumentsContext
    ProjectionRegistrationRequest --* ProjectionEventSelector
    ProjectionEventSelector --* Artifact
    EventSourceIdKeySelector ..|> ProjectionEventSelector
    PartitionIdKeySelector ..|> ProjectionEventSelector
    EventPropertyKeySelector ..|> ProjectionEventSelector
    ProjectionRegistrationResponse --o Failure
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

## Get one ReadModel by key

Retrieves an instance of a read model for a specified projection and its unique key - for one tenant. If the instance does not exist, the Runtime will reply with the initial state for the projection.

```mermaid
sequenceDiagram
    participant C as Client (SDK)
    participant R as Runtime
    C->>R: GetOneRequest
    activate R
    R->>C: GetOneResponse
    deactivate R
```

### Event Message types

## Get all ReadModels

Retrieves all instances of a read model for a specified projection - for one tenant.

```mermaid
sequenceDiagram
    participant C as Client (SDK)
    participant as Runtime
    C->>R: GetAllRequest
    activate R
    R->>C: GetAllResponse
    deactivate R
```
### Event Message types
