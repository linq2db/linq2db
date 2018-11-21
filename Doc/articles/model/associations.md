---
title: Associations in LINQ To DB
author: sdanyliv
---
# Associations in LINQ To DB

An Association defines how entities relate to each other. It can be representation of foreign key constraint or custom query which can be automaticaly handled by `LINQ To DB`.

Associations is powerful mechanism in `LINQ To DB` and it is not so limited as in vanilla frameworks. Since `LINQ To DB` do not track changes we can express any idea using associations to simplify writing LINQ queies.

## In this article

One-To-One Associations
One-To-Many Associations
Associations as Extension Methods

## One-To-One Associations

It is simple relation between two entities and can be defined using AssociationAttribute.
In the following example shown how to define One-To-One association betwwen `Order` and `Employee` entities.

```cs
public partial class Order
{
   [Association(ThisKey = nameof(EmployeeID), OtherKey = nameof(Models.Employee.EmployeeID), CanBeNull = true)]
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

`ThisKey` and `OtherKey` is a lists of comma separated names of members appropriate entities. `CanBeNull = true` forces engine to join entities using `LEFT JOIN`, otherwise it will `INNER JOIN`.

## One-To-Many Associations

This association simplifies queries from `Main` entity to `Related` entities. Usually it is property with `IEnumerable` type. It also can be `List` or `Array`.

```cs
public partial class Order
{
   [Association(ThisKey = nameof(OrderID), OtherKey = nameof(OrderDetail.OrderID))]
   public IEnumerable<OrderDetail> Details { get; set; }
}
```

It helps write the following kind of queries

```cs
   var query = from order in db.Orders
      where order.Details.Any(d => d.Discount > 0.06)
      select new
      {
         order.EmployeeID,
         MaxDicount = order.Details.Max(d => d.Discount)
      };

```

Asscociations not only selects entities using appropriate keys, but there is possiblity to make custom predicate. It is needed to define satic filter function. Function should return Expression which shows how entities related to each other. It should be expression with two parameters `Main` and `Related` and returns `bool`.

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

In this sample `ThisKey` and `OtherKey` has beed removed but, if they are defined, predicate will be extended with their equality.

And previous query can be rewtitten using this new property

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

There are situations when changing model is not allowed and it is located in separate library and it is not possible to extend class using `partial` definition.
In this case `LINQ To DB` has ability to define associations as Extension methods.
Previous samples can be rewritten to use Extension methods.

```cs
public static class NorthwindExtensions
{
   [Association(ThisKey = nameof(Order.EmployeeID), OtherKey = nameof(Models.Employee.EmployeeID), CanBeNull = true)]
   public static Employee Employee(this Order order)
   {
      throw new InvalidOperationException("Used only as Association helper");
   }

   [Association(ThisKey = nameof(Order.OrderID), OtherKey = nameof(OrderDetail.OrderID))]
   public static IEnumerable<OrderDetail> Details(this Order order)
   {
      throw new InvalidOperationException("Used only as Association helper");
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

As we can see from extension methods implemenation - they do not allow invocation and used only as declarative methods in linq query.
But there are cases when it is required to query details exactly from materialized object.

There is another approach, when we pass in extension method additional parameter, it can be `IDataContext`, `DataConnection` or `NotrthwindDB` from this article.

Extension method should return `IQueryable` interface which is built using properties of the extended entity.

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

The same techinque can be applied to entity methods.