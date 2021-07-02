---
title: Filters
description: The definition of the Runtime Filters service
weight: 10
---


## Register Unpartitioned Private Filter

Registers an unpartitioned callback-type filter, to filter events from the (all tenants) event log to a stream. The runtime will call the client with events to decide if they should be in the stream.

```mermaid
sequenceDiagram
    participant C as Client (SDK)
    participant R as Runtime
    C->>R: FilterRegistrationRequest
    activate R
    alt If registration fails
        R->>C: FilterRegistrationResponse (with Failure)
    else If registration succeeds
        R->>C: FilterRegistrationResponse
        deactivate R
        loop For each event in the event log
            R->>C: FilterEventRequest
            activate C
            C->>R: FilterResponse
            deactivate C
        end
    end
```


### Registration message types

```mermaid
classDiagram
    class FilterRegistrationRequest{
        ReverseCallArgumentsContext callContext
        Uuid scopeId
        Uuid filterId
    }
    class FilterRegistrationResponse{
        Failure failure
    }
    %%
    FilterRegistrationRequest --* ReverseCallArgumentsContext
    FilterRegistrationResponse --o Failure
    %% links
    link ReverseCallArgumentsContext "{{< ref "types" >}}" "Types"
    link Failure "{{< ref "types" >}}" "Types"
```

### Event message types

```mermaid
classDiagram
    class FilterEventRequest{
        ReverseCallRequestContext callContext
        CommittedEvent event
        Uuid scopeId
        RetryProcessingState retryProcessingState
    }
    class FilterResponse{
        ReverseCallResponseContext callContext
        bool isIncluded
        ProcessorFailure failure
    }
    %%
    FilterEventRequest --* ReverseCallRequestContext
    FilterEventRequest --* CommittedEvent
    FilterEventRequest --* RetryProcessingState
    FilterResponse --* ReverseCallResponseContext
    FilterResponse --* ProcessorFailure
    %% links
    link ReverseCallRequestContext "{{< ref "types" >}}" "Types"
    link ReverseCallResponseContext "{{< ref "types" >}}" "Types"
    link RetryProcessingState "{{< ref "types#event-processing-types" >}}" "Types"
    link ProcessorFailure "{{< ref "types#event-processing-types" >}}" "Types"
    link CommittedEvent "{{< ref "event_store#message-types" >}}" "Event Store"
```

## Register Partitioned Private Filter

Registers a partitioned callback-type filter, to filter events from the (all tenants) event log to a stream. The runtime will call the client with events to decide if they should be in the stream, and in what partition.

```mermaid
sequenceDiagram
    participant C as Client (SDK)
    participant R as Runtime
    C->>R: PartitionedFilterRegistrationRequest
    activate R
    alt If registration fails
        R->>C: FilterRegistrationResponse (with Failure)
    else If registration succeeds
        R->>C: FilterRegistrationResponse
        deactivate R
        loop For each event in the event log
            R->>C: FilterEventRequest
            activate C
            C->>R: PartitionedFilterResponse
            deactivate C
        end
    end
```


### Registration message types

```mermaid
classDiagram
    class PartitionedFilterRegistrationRequest{
        ReverseCallArgumentsContext callContext
        Uuid scopeId
        Uuid filterId
    }
    class FilterRegistrationResponse{
        Failure failure
    }
    %%
    PartitionedFilterRegistrationRequest --* ReverseCallArgumentsContext
    FilterRegistrationResponse --o Failure
    %% links
    link ReverseCallArgumentsContext "{{< ref "types" >}}" "Types"
    link Failure "{{< ref "types" >}}" "Types"
```

### Event message types

```mermaid
classDiagram
    class FilterEventRequest{
        ReverseCallRequestContext callContext
        CommittedEvent event
        Uuid scopeId
        RetryProcessingState retryProcessingState
    }
    class PartitionedFilterResponse{
        ReverseCallResponseContext callContext
        bool isIncluded
        Uuid partition
        ProcessorFailure failure
    }
    %%
    FilterEventRequest --* ReverseCallRequestContext
    FilterEventRequest --* CommittedEvent
    FilterEventRequest --* RetryProcessingState
    PartitionedFilterResponse --* ReverseCallResponseContext
    PartitionedFilterResponse --* ProcessorFailure
    %% links
    link ReverseCallRequestContext "{{< ref "types" >}}" "Types"
    link ReverseCallResponseContext "{{< ref "types" >}}" "Types"
    link RetryProcessingState "{{< ref "types#event-processing-types" >}}" "Types"
    link ProcessorFailure "{{< ref "types#event-processing-types" >}}" "Types"
    link CommittedEvent "{{< ref "event_store#message-types" >}}" "Event Store"
```

## Register Public Filter 

Registers a partitioned callback-type filter, to filter public events from the (all tenants) event log to a public stream. The runtime will call the client with public events to decide if they should be in the stream, and in what partition.

```mermaid
sequenceDiagram
    participant C as Client (SDK)
    participant R as Runtime
    C->>R: PublicFilterRegistrationRequest
    activate R
    alt If registration fails
        R->>C: FilterRegistrationResponse (with Failure)
    else If registration succeeds
        R->>C: FilterRegistrationResponse
        deactivate R
        loop For each public event in the event log
            R->>C: FilterEventRequest
            activate C
            C->>R: PartitionedFilterResponse
            deactivate C
        end
    end
```

### Registration message types

```mermaid
classDiagram
    class PublicFilterRegistrationRequest{
        ReverseCallArgumentsContext callContext
        Uuid filterId
    }
    class FilterRegistrationResponse{
        Failure failure
    }
    %%
    PublicFilterRegistrationRequest --* ReverseCallArgumentsContext
    FilterRegistrationResponse --o Failure
    %% links
    link ReverseCallArgumentsContext "{{< ref "types" >}}" "Types"
    link Failure "{{< ref "types" >}}" "Types"
```

### Event message types

```mermaid
classDiagram
    class FilterEventRequest{
        ReverseCallRequestContext callContext
        CommittedEvent event
        Uuid scopeId
        RetryProcessingState retryProcessingState
    }
    class PartitionedFilterResponse{
        ReverseCallResponseContext callContext
        bool isIncluded
        Uuid partition
        ProcessorFailure failure
    }
    %%
    FilterEventRequest --* ReverseCallRequestContext
    FilterEventRequest --* CommittedEvent
    FilterEventRequest --* RetryProcessingState
    PartitionedFilterResponse --* ReverseCallResponseContext
    PartitionedFilterResponse --* ProcessorFailure
    %% links
    link ReverseCallRequestContext "{{< ref "types" >}}" "Types"
    link ReverseCallResponseContext "{{< ref "types" >}}" "Types"
    link RetryProcessingState "{{< ref "types#event-processing-types" >}}" "Types"
    link ProcessorFailure "{{< ref "types#event-processing-types" >}}" "Types"
    link CommittedEvent "{{< ref "event_store#message-types" >}}" "Event Store"
```
