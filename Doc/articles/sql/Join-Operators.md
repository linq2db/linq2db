# Joins

`LINQ To DB` supports full set of joins: INNER, LEFT, FULL, RIGHT, CROSS JOIN.

## INNER JOIN

### Join operator on single column

```c#
var query =
    from c in db.Category
    join p in db.Product on c.CategoryID equals p.CategoryID
    where !p.Discontinued
    select c;
```

### Using "Where" condition

```c#
var query =
    from c in db.Category
    from p in db.Product.Where(pr => pr.CategoryID == c.CategoryID)
    where !p.Discontinued
    select c;
```

### Using "InnerJoin" function

```c#
var query =
    from c in db.Category
    from p in db.Product.InnerJoin(pr => pr.CategoryID == c.CategoryID)
    where !p.Discontinued
    select c;
```

### Result SQL

```sql
SELECT
    [c].[CategoryID],
    [c].[CategoryName],
    [c].[Description],
    [c].[Picture]
FROM
    [Categories] [c]
        INNER JOIN [Products] [p] ON [c].[CategoryID] = [p].[CategoryID]
WHERE
    [p].[Discontinued] <> 1
```

### Join operator on multiple columns

```cs
var query =
    from p in db.Product
    from o in db.Order
    join d in db.OrderDetail
        on     new { p.ProductID, o.OrderID }
        equals new { d.ProductID, d.OrderID }
    where !p.Discontinued
    select new
    {
        p.ProductID,
        o.OrderID,
    };
```

### Result SQL

```sql
SELECT
    [t3].[ProductID] as [ProductID1],
    [t3].[OrderID] as [OrderID1]
FROM
    (
        SELECT
            [t1].[ProductID],
            [t2].[OrderID],
            [t1].[Discontinued]
        FROM
            [Products] [t1],
            [Orders] [t2]
    ) [t3]
        INNER JOIN [Order Details] [d] ON [t3].[ProductID] = [d].[ProductID] AND [t3].[OrderID] = [d].[OrderID]
WHERE
    [t3].[Discontinued] <> 1
```

## LEFT JOIN

### Join operator on single column

```cs
var query =
    from c in db.Category
    join p in db.Product on c.CategoryID equals p.CategoryID into lj
    from lp in lj.DefaultIfEmpty()
    where !lp.Discontinued
    select c;
```

### Using "Where" condition

```cs
var query =
    from c in db.Category
    from lp in db.Product.Where(p => p.CategoryID == c.CategoryID).DefaultIfEmpty()
    where !lp.Discontinued
    select c;
```

### Using "LeftJoin" function

```cs
var query =
    from c in db.Category
    from p in db.Product.LeftJoin(pr => pr.CategoryID == c.CategoryID)
    where !p.Discontinued
    select c;
``````

### Result SQL

```sql
SELECT
    [c1].[CategoryID],
    [c1].[CategoryName],
    [c1].[Description],
    [c1].[Picture]
FROM
    [Categories] [c1]
        LEFT JOIN [Products] [lj] ON [c1].[CategoryID] = [lj].[CategoryID]
WHERE
    1 <> [lj].[Discontinued]
```

## RIGHT JOIN

### Using "RightJoin" function

```cs
var query =
    from c in db.Category
    from p in db.Product.RightJoin(pr => pr.CategoryID == c.CategoryID)
    where !p.Discontinued
    select c;
```

### Result SQL

```sql
SELECT
    [t2].[CategoryID],
    [t2].[CategoryName],
    [t2].[Description],
    [t2].[Picture]
FROM
    [Categories] [t2]
        RIGHT JOIN [Products] [t1] ON [t1].[CategoryID] = [t2].[CategoryID]
WHERE
    1 <> [t1].[Discontinued]
```

## FULL JOIN

### Using "FullJoin" function

```cs
var query =
    from c in db.Category
    from p in db.Product.FullJoin(pr => pr.CategoryID == c.CategoryID)
    where !p.Discontinued
    select c;
```

### Result SQL

```sql
SELECT
    [t2].[CategoryID],
    [t2].[CategoryName],
    [t2].[Description],
    [t2].[Picture]
FROM
    [Categories] [t2]
        FULL JOIN [Products] [t1] ON [t1].[CategoryID] = [t2].[CategoryID]
WHERE
    1 <> [t1].[Discontinued]
```

## CROSS JOIN

### Using SelectMany

```cs
var query =
    from c in db.Category
    from p in db.Product
    where !p.Discontinued
    select new {c, p};
```

### Result SQL

```sql
SELECT
    [t1].[CategoryID],
    [t1].[CategoryName],
    [t1].[Description],
    [t1].[Picture],
    [t2].[ProductID],
    [t2].[ProductName],
    [t2].[SupplierID],
    [t2].[CategoryID] as [CategoryID1],
    [t2].[QuantityPerUnit],
    [t2].[UnitPrice],
    [t2].[UnitsInStock],
    [t2].[UnitsOnOrder],
    [t2].[ReorderLevel],
    [t2].[Discontinued]
FROM
    [Categories] [t1],
    [Products] [t2]
WHERE
    1 <> [t2].[Discontinued]
```
