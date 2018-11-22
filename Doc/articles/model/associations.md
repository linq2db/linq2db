---
title: Associations in LINQ To DB
author: sdanyliv
---
# Associations in LINQ To DB

Association defines how entities relate to each other. It can be representation of foreign key constraint or custom query which can be automaticaly handled by `LINQ To DB`.

Associations is powerful mechanism in `LINQ To DB`, not so limited as in vanilla frameworks. Since `LINQ To DB` do not track changes we can express any idea using associations to simplify writing of LINQ queies.

## In this article

[One-to-one associations](#one-to-one-associations)

[One-to-many associations](#one-to-many-associations)

[Associations as extension methods](#associations-as-extension-methods)

## One-To-One Associations

It is simple relation between two entities and can be defined using `AssociationAttribute`.
In example below we show how to define optional one-to-one association between `Order` and `Employee` entities.

```cs
public partial class Order
{
    [Association(
        ThisKey   = nameof(EmployeeID),
        OtherKey  = nameof(Models.Employee.EmployeeID),
        CanBeNull = true)]
    public Employee Employee { get; set; }
}

```

```cs
var query = from order in db.Orders
   where order.Employee.Address.StartsWith("B")
   select new
   {
      order.OrderID,
      order.OrderDate,
      order.Employee.Address,
   };
```

`ThisKey` and `OtherKey` contain comma-separated names of members of appropriate entities, used in join condition. `CanBeNull = true` means that relation is optional and forces engine to join entities using `LEFT JOIN` instead of `INNER JOIN` for non-optional relations.

## One-To-Many Associations

This association simplifies queries from `Main` entity to `Related` entities. Usually it is a property with `IEnumerable<T>` type, which can be also of `List<T>` or `T[]` type.

```cs
public partial class Order
{
    [Association(
	     ThisKey  = nameof(OrderID),
		  OtherKey = nameof(OrderDetail.OrderID))]
    public IEnumerable<OrderDetail> Details { get; set; }
}
```

It helps to write queries like that:

```cs
   var query = from order in db.Orders
      where order.Details.Any(d => d.Discount > 0.06)
      select new
      {
         order.EmployeeID,
         MaxDicount = order.Details.Max(d => d.Discount)
      };

```

Except joins on specified set of key-fields, you can also define custom join condition. You need to define static function which returns `Expression`-typed join predicate. It should be expression function with two parameters `Main` and `Related` and return `bool` result.

```cs
public partial class Order
{
    [Association(ExpressionPredicate = nameof(DetailsWithBigDiscountFilter))]
    public IEnumerable<OrderDetail> DetailsWithBigDiscount { get; set; }

    private static Expression<Func<Order, OrderDetail, bool>> DetailsWithBigDiscountFilter()
    {
        return (order, detail) => order.OrderID == detail.OrderID && detail.Discount > 0.06;
    }
}
```

In this example `ThisKey` and `OtherKey` were replaced with custom predicate, but if they are defined, predicate will be combined with keys using `AND` condition.

Now query from previous example could be simplified using new association:

```cs
var query = from order in db.Orders
   where order.DetailsWithBigDiscount.Any()
   select new
   {
      order.EmployeeID,
      MaxDiscount = order.DetailsWithBigDiscount.Max(d => d.Discount)
   };
```

## Associations as Extension Methods

There are situations when changing model is not allowed or it is located in separate library, so you cannot add assiciations to model class itself.
For this case `LINQ To DB` has ability to define associations as extension methods.
Previous examples can be rewritten to use extension methods:

```cs
public static class NorthwindExtensions
{
    [Association(
	     ThisKey   = nameof(Order.EmployeeID),
		  OtherKey  = nameof(Models.Employee.EmployeeID),
		  CanBeNull = true)]
    public static Employee Employee(this Order order)
    {
        throw new InvalidOperationException("Called outside of query");
    }

    [Association(
	     ThisKey  = nameof(Order.OrderID),
		  OtherKey = nameof(OrderDetail.OrderID))]
    public static IEnumerable<OrderDetail> Details(this Order order)
    {
        throw new InvalidOperationException("Called outside of query");
    }
}
```

```cs
var query = from order in db.Orders
   where order.Details().Any(d => d.Discount > 0.06)
   select new
   {
      order.EmployeeID,
      MaxDicount = order.Details().Max(d => d.Discount)
   };
```

As we can see from extension methods implemenation - they do not support execution and should be used only as declarative methods in linq query.
But there are cases when it is required to query details exactly from materialized object.

For such cases you can define additional parameter in extension method of `IDataContext`-based type, e.g. `IDataContext`, `DataConnection` or `NotrthwindDB` from this article.

Extension method should return `IQueryable<T>` interface.

```cs
public static class NorthwindExtensions
{
    [Association(ThisKey = nameof(Order.OrderID), OtherKey = nameof(OrderDetail.OrderID))]
    public static IQueryable<OrderDetail> DetailsQuery(this Order order, IDataContext db)
    {
        return db.GetTable<OrderDetail>().Where(d => d.OrderID == order.OrderID);
    }
}
```

The same techinque can be applied to entity methods. (**TODO:clarify what it means**)
