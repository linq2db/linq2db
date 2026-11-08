# How to teach LINQ to DB to convert custom .NET methods and objects to SQL

You may run into a situation where LINQ to DB does not know how to convert some .NET method, property or object to SQL. But that is not a problem because LINQ to DB likes to learn. Just teach it :). In another article we wrote about [Using this MapValueAttribute to control mapping with Linq To DB](xref:mapvalue). In this article we will go a little bit deeper.

There are multiple ways to teach LINQ to DB how to convert custom properties and methods into SQL, but the primary ones are:

- [LinqToDB.Sql.ExpressionAttribute](#sqlexpression) and [LinqToDB.Sql.FunctionAttribute](#sqlfunction-attribute)
- [LinqToDB.ExpressionMethodAttribute](#expressionmethod)
- [LinqToDB.Linq.Expressions.MapMember()](#mapmember) method
- [LinqToDB.Mapping.MappingSchema.SetValueToSqlConverter()](#setvaluetosqlconverter) method

Let's see how to use each of these methods.

## Sql.Expression

Let's say you love SQL's BETWEEN operator and you find out that LINQ to DB does not have `Between()` method (only as example, as we have it, check `Sql.Between` API) out of the box, so you have to write something like this:

```cs
var query = db.Customers.Where(c => c.ID >= 1000 && c.ID <= 2000);
```

Here is how the `Sql.Expression` attribute can help us bring Between to .NET:

```cs
[Sql.Expression("{0} BETWEEN {1} AND {2}", PreferServerSide = true)]
public static bool Between<T>(this T x, T low, T high) where T : IComparable<T>
{
    // x >= low && x <= high
    return x.CompareTo(low) >= 0 && x.CompareTo(high) <= 0;
}
```

Let's test it:

```cs
[Test]
public void SqlExpressionAttributeTest()
{
    using (var db = new DataModel())
    {
        db.InlineParameters = true; // inlined parameters can be helpful when debugging
        db.Customers.Where(c => c.DateOfBirth.Between(new DateTime(2000, 1, 1), new DateTime(2000, 12, 31))).ToList();
    }
}
```

The SQL generated for SQL Server 2012 is:

```sql
SELECT
    [t1].[ID],
    [t1].[DateOfBirth],
    [t1].[FirstName],
    [t1].[LastName],
    [t1].[Email]
FROM
    [dbo].[Customer] [t1]
WHERE
    [t1].[DateOfBirth] BETWEEN '2000-01-01' AND '2000-12-31'
 ```

Notice the use of the `Sql.ExpressionAttribute.PreferServerSide` property set to true. `PreferServerSide = true` tells LINQ to DB to convert the method to SQL if possible, and if it's not possible for some reason - then to execute the method locally.

There is another similar property - `ServerSideOnly`. If it's set to True, LINQ to DB will throw an exception if it can't convert a method to SQL. It can be set to true when you can't, don't need or don't want to write a client-side implementation.

You may have a valid question: When can't LINQ to DB generate SQL? How is that possible if we show LINQ to DB what we want to generate? Here is a simple example:

```cs
var q =
    from c in db.Customers
    select
        SomeServerSideOnlyMethod(SomeLocalApplicationMethod(c.ID));
```

Let's say `SomeServerSideOnlyMethod()` is a method with the `Sql.Expression` attribute and `ServerSideOnly = true`, and `SomeLocalApplicationMethod()` is an ordinary .NET method that can only be executed locally.

Since `SomeLocalApplicationMethod()` must be executed locally, LINQ to DB has to first read the `Customer.ID` field values from the table to pass them to `SomeLocalApplicationMethod()` on the client side. From this moment the query, including the call to `SomeServerSideOnlyMethod()`, will have to be executed locally. But considering that `SomeServerSideOnlyMethod()` is marked as `ServerSideOnly = true`, LINQ to DB will throw an exception.

## Sql.Function attribute

Presume that we are using SQL Server and we want to check if a string contains a representation of a numeric value. SQL Server has the `IsNumeric()` function, but LINQ to DB does not support it out of the box. It's easy to fix:

```cs
[Sql.Function("IsNumeric", ServerSideOnly = true)]
public static bool IsNumeric(string s)
{
    throw new InvalidOperationException();
}

[Test]
public void SqlFunctionAttributeTest()
{
    using (var db = new DataModel())
    {
        db.InlineParameters = true;
        db.Customers.Where(c => SqlFunctions.IsNumeric(c.LastName)).ToList();
    }
}
```

The generated SQL:

```sql
SELECT
    [t1].[ID],
    [t1].[DateOfBirth],
    [t1].[FirstName],
    [t1].[LastName],
    [t1].[Email]
FROM
    [dbo].[Customer] [t1]
WHERE
    IsNumeric([t1].[LastName]) = 1
 ```

Please note, that you may omit specifying the function name in the attribute explicitly - in this case the method name (that the attribute is applied to) will be used as a function name.

## ExpressionMethod

Let us now examine the next attribute - `LinqToDB.ExpressionMethodAttribute`, a very powerful one. The `ExpressionMethodAttribute` allows specifying an expression that LINQ to DB will translate into SQL.

For those of us who are fans of the SQL's `IN` operator, let's show how we can make LINQ to DB support it:

```cs
[ExpressionMethod("InImpl")]
public static bool In<T>(this T item, IEnumerable<T> items)
{
    return items.Contains(item); // this code will run if we execute the method locally
}
 
public static Expression<Func<T, IEnumerable<T>, bool>> InImpl<T>()
{
    // LINQ to DB will translate this expression into SQL
    // (it knows out of the box how to translate Contains()
    return (item, items) => items.Contains(item); 
}
```

Here we are using the `ExpressionMethod` attribute to specify a method that will return an `Expression`, and LINQ to DB will convert that `Expression` into SQL (basically, LINQ to DB uses the expression tree returned by the method specified with the `ExpressionMethod` attribute to replace a part of a bigger expression tree that will later be converted to SQL). The generic type parameter of the `Expression` should be a `Func<T>` delegate, representing a function that takes the same parameters and returns the same type as a local method. For example, if a local method has this declaration:

```cs
T1 MyMethod(T2, T3)
```

Then the `ExpressionMethod` attribute should point to a method with the following declaration:

```cs
Expression<Func<T2, T3, T1>> MyMethodImpl()
```

The test:

```cs
[Test]
public void InTest()
{
    using (var db = new DataModel())
    {
        var customers = db.Customers.Where(c => c.FirstName.In(new[] {"Pavel", "John", "Jack"})).ToList();
    }
}
```

This will generate the following SQL:

```sql
SELECT
    [t1].[ID],
    [t1].[DateOfBirth],
    [t1].[FirstName],
    [t1].[LastName],
    [t1].[Email]
FROM
    [dbo].[Customer] [t1]
WHERE
    [t1].[FirstName] IN (N'Pavel', N'John', N'Jack')
 ```

Another example, showing that `ExpressionMethod` can be applied to properties:

```cs
public partial class Issue
{
    [ExpressionMethod("GetAgeExpression")]
    public double AgeInDays
    {
        get { return (DateTime.Now - CreatedOn).TotalDays; }
    }

    private static Expression<Func<Issue, double>> GetAgeExpression()
    {
        return issue => (Sql.CurrentTimestamp - issue.CreatedOn).TotalDays;
    }
}
```

Test:

```cs
[Test]
public void ExpressionMethodTest2()
{
    using (var db = new DataModel())
    {
        var issues = db.Issues.Where(issue => issue.AgeInDays > 30).ToList();
    }
}
```

The generated SQL:

```sql
SELECT
    [t1].[ID],
    [t1].[Subject],
    [t1].[Description],
    [t1].[Status],
    [t1].[CreatedOn]
FROM
    [dbo].[Issue] [t1]
WHERE
    DateDiff(Day, [t1].[CreatedOn], CURRENT_TIMESTAMP) > 30
 ```

You can find more examples of ExpressionMethod usage (including a possible `LeftJoin()` implementation that may be of interest to you) here: [ExpressionTests.cs](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/ExpressionsTests.cs)

## MapMember()

The next method we will discuss is the `LinqToDB.Linq.Expressions.MapMember()` method (having numerous overloads). It allows you to specify how to convert existing methods and properties. Basically, you provide the original method or property and the corresponding `Expression` that will be used by LINQ to DB instead of the original method. Internally LINQ to DB uses `MapMember()` to map hundreds of standard .NET framework methods and properties.

For example, we would like to make LINQ to DB support the `String.IsNullOrWhitespace()` method and we can't add the `ExpressionMethod` attribute to `IsNullOrWhitespace()` because it's a framework's method and we can't change it.

The `MapMember()` method comes to the rescue!

```cs
public partial class DataModel
{
    static DataModel()
    {
        LinqToDB.Linq.Expressions.MapMember((string s) => string.IsNullOrWhiteSpace(s), s => s == null || s.TrimEnd() == string.Empty);
    }
}

[Test]
public void MapMemberTest()
{
    using (var db = new DataModel())
    {
        var customers = db.Customers.Where(c => string.IsNullOrWhiteSpace(c.Email)).ToList();
    }
}
```

The generated SQL:

```sql
SELECT
    [t1].[ID],
    [t1].[DateOfBirth],
    [t1].[FirstName],
    [t1].[LastName],
    [t1].[Email]
FROM
    [dbo].[Customer] [t1]
WHERE
    [t1].[Email] IS NULL OR RTrim([t1].[Email]) = N''
```

## SetValueToSqlConverter()

The last method we will examine is `LinqToDB.Mapping.MappingSchema.SetValueToSqlConverter()`. It is used to control exactly how a value will be converted to SQL. The two primary use cases for this method are:

1. When adding support for a new database provider. For example, when working with the `Boolean` data type in Informix RDBMS, `t` represents the logical value TRUE and `f` represents FALSE. Here is how this is implemented in LinqToDB as a part of its Informix support:

```cs
public class InformixMappingSchema : MappingSchema
{
    protected InformixMappingSchema(string configuration) : base(configuration)
    {
        SetValueToSqlConverter(typeof(bool), (sb,dt,v) => sb.Append("'").Append((bool)v ? 't' : 'f').Append("'"));
    }
} 
```

2. When adding support for a new data type. For example, here is how to teach LINQ to DB to consider the `SqlDecimal.IsNull` property and correctly convert `SqlDecimal` objects to SQL:

```cs
MappingSchema.Default.SetValueToSqlConverter(
    typeof(SqlDecimal),
    (sb, dt, v) =>
    {
        var value = (SqlDecimal)v;

        if (value.IsNull)
            sb.Append("NULL");
        else
            sb.Append(v);
    });
```
