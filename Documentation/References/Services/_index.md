---
title: Services
description: The definition of the Runtime services
weight: 1
---

These pages describe and define the services that the Runtime offers.
A service is a grouping of the features offered by the Runtime, e.g. the _Event Store_ is a service that can be used to commit events.
The services are exposed as gRPC services, defined in the [Dolittle Contracts](https://github.com/dolittle/Contracts).
A lot of the services offered by the Runtime uses a custom protocol that we have termed _Reverse Call_, where the Client registers a handler with the Runtime, and while registered the Runtime will make requests to the Client over the same connection.
The nature of these protocols are not obvious from the gRPC definitions, and are more easily understood by the sequence diagrams on these pages.

These services are not ment to be used directly through gRPC but rather through one of the [Dolittle SDKs](!!LINK_TO_SDKS!!), so this documentation is mostly here for reference for someone implementing an SDK or wanting to understand the deep inner workings of the Runtime.
