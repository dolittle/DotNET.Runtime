---
title: Aggregates
description: Overview of Aggregates
weight: 70
---

An Aggregate is [Domain-driven design](https://en.wikipedia.org/wiki/Domain-driven_design) (DDD) term coined by Eric Evans. An aggregate is a collection of objects and it represents a concept in your domain, it's not a container for items. It's bound together by an Aggregate Root, which upholds the rules (invariants) to keep the aggregate consistent. It encapsulates the domain objects, enforces business rules, and ensures that the aggregate can't be put into an invalid state.


## Example

For example, in the domain of a restaurant, a `Kitchen` could be an aggregate, where it has domain objects like `Chefs`, `Inventory` and `Menu` and an operation `PrepareDish`.

The kitchen would make sure that:
- A `Dish` has to be on the `Menu` for it to be ordered
- The `Inventory` needs to have enough ingredients to make the `Dish`
- The `Dish` gets assigned to an available `Chef`

Here's a simple C#_ish_ example of what this aggregate root could look like:
```csharp
public class Kitchen
{
    Chefs _chefs;
    Inventory _inventory;
    Menu _menu;

    public void PrepareDish(Dish dish)
    {
        if (!_menu.Contains(dish))
        {
            throw new DishNotOnMenu(dish);
        }
        foreach (var ingredient in dish.ingredients)
        {
            var foundIngredient = _inventory
                .GetIngredient(ingredient.Name);
            if (!foundIngredient)
            {
                throw new IngredientNotInInventory(ingredient);
            }

            if (foundIngredient.Amount < ingredient.Amount)
            {
                throw new InventoryOutOfIngredient(foundIngredient);
            }
        }
        var availableChef = _chefs.GetAvailableChef();
        if (!availableChef)
        {
            throw new NoAvailableChefs();
        }
        availableChef.IsAvailable = false;
    }
}
```

## Aggregates in Dolittle

With [Event Sourcing]({{< ref "event_sourcing" >}}) the aggregates are the key components to enforcing the business rules and the state of domain objects. Dolittle has a concept called `AggregateRoot` in the [Event Store]({{< ref "event_store" >}}) that acts as an aggregate root to the `AggregateEvents` _applied_ to it. The root holds a reference to all the aggregate events applied to it and it can fetch all of them.

### Structure of an `AggregateRoot`

This is a simplified structure of the main parts of an aggregate root.
```csharp
AggregateRoot {
    AggregateRootId Guid
    EventSourceId Guid
    Version int
    AggregateEvents AggregateEvent[] {
        EventSourceId Guid
        AggregateRootId Guid
        // normal Event properties also included
        ...
    }
}
```

##### `AggregateRootId`

Identifies this specific type of aggregate root. In the kitchen example this would a unique id given to the `Kitchen` class to identify it from other aggregate roots.

#### `EventSourceId`

`EventSourceId` represents the source of the event like a "primary key" in a traditional database.  In the kitchen example this would be the unique id for each instance of the `Kitchen` aggregate root.

#### `Version`

`Version` is the position of the next `AggregateEvent` to be processed. It's incremented after each `AggregateEvent` has been applied by the `AggregateRoot`. This ensures that the root will always apply the events in the correct order.

### `AggregateEvents`
The list holds the reference ids to the actual `AggregateEvent` instances that are stored in the [Event Log]({{< ref "event_store#event-log" >}}). With this list the root can ask the Runtime to fetch all of the events with matching `EventSourceId` and `AggregateRootId`.


## Designing aggregates

When building your aggregates, roots and rules, it is helpful to ask yourself these questions:
- _"What is the impact of breaking this rule?"_
- _"What happens in the domain if this rule is broken?"_
- _"Am I modelling a domain concern or a technical concern?"_
- _"Can this rule be broken for a moment or does it need to be enforced immediately?"_
- _"Do these rules and domain objects break together or can they be split into another aggregate?"_

## Further reading

- [Aggregates by Martin Fowler](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Aggregates in Domain Driven Design](https://medium.com/ingeniouslysimple/aggregates-in-domain-driven-design-5aab3ef9901d)
- [Uncovering Hidden Business Rules with DDD Aggregates by Nick Tune](https://medium.com/nick-tune-tech-strategy-blog/uncovering-hidden-business-rules-with-ddd-aggregates-67fb02abc4b)
