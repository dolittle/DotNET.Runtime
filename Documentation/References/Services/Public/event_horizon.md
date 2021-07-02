---
title: Event Horizon
description: The definition of the Runtime Event Horizon service
weight: 10
---

## Get Events from public stream

Get all events (can be multiple types) from a partition in a public stream of a tenant. The Runtime will call the client with the events in the specified stream, to store a copy in a scoped event log.

```mermaid
sequenceDiagram
    participant C as Client (Runtime Consumer)
    participant R as Runtime (Producer)
    C->>R: ConsumerSubscriptionRequest
    activate R
    alt If no consent exists
        R->>C: SubscriptionResponse (with Failure)
    else If consent exists
        R->>C: SubscriptionResponse
        deactivate R
        loop for each event in the tenants public stream and partition at or after the requested stream position
            R->>C: ConsumerRequest
            activate C
            C->>R: ConsumerResponse
            deactivate C
        end
    end
```


### Registration message types

```mermaid
classDiagram
    class ConsumerSubscriptionRequest{
        ReverseCallArgumentsContext callContext
        Uuid tenantId
        Uuid streamId
        Uuid partitionId
        uint64 streamPosition
    }
    class SubscriptionResponse{
        Failure failure
        Uuid consentId
    }
    %%
    ConsumerSubscriptionRequest --* ReverseCallArgumentsContext
    SubscriptionResponse--o Failure
    %% links
    link ReverseCallArgumentsContext "{{< ref "types" >}}" "Types"
    link Failure "{{< ref "types" >}}" "Types"
```

### Event message types

```mermaid
classDiagram
    class ConsumerRequest{
        ReverseCallRequestContext callContext
        EventHorizonEvent event
    }
    class EventHorizonEvent{
        uint64 streamSequenceNumber
        CommittedEvent event
    }
    class ConsumerResponse{
        ReverseCallResponseContext callContext
        bool isIncluded
        ProcessorFailure failure
    }
    %%
    ConsumerRequest --* EventHorizonEvent
    ConsumerRequest --* ReverseCallRequestContext
    EventHorizonEvent --* CommittedEvent
    ConsumerResponse --* ReverseCallResponseContext
    ConsumerResponse --* ProcessorFailure
    %% links
    link ReverseCallRequestContext "{{< ref "types" >}}" "Types"
    link ReverseCallResponseContext "{{< ref "types" >}}" "Types"
    link ProcessorFailure "{{< ref "types#event-processing-types" >}}" "Types"
    link CommittedEvent "{{< ref "../Private/event_store#message-types" >}}" "Event Store"
```
