---
uid: joins
---
# INNER JOIN On Single Column

C#

```c#
using (var db = new NorthwindDB())
{
	var query =
		from c in db.Category
		join p in db.Product on c.CategoryID equals p.CategoryID
		where !p.Discontinued
		select c;

	foreach (var category in query)
		Console.WriteLine(category.CategoryID);
}
```

SQL

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

# INNER JOIN On Multiple Columns

C#

```c#
using (var db = new NorthwindDB())
{
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

	foreach (var item in query)
		Console.WriteLine(item);
}
```

SQL

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