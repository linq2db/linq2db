# SQL Extensions

This folder contains SQL extensions for Linq To DB, as well as components to help you build your own extensions.

## `Sql`

The SQL class contains a number of functions implementing common DB functions such as `Between` or `CurrentTimestamp`. [The XMLDocs provide a list of all the provided functions](https://linq2db.github.io/api/LinqToDB.Sql.html).

Note that many of these are used automatically behind the scenes. For example, The provider will automatically translate something such as `table.SomeDateTimeField.Year` into the appropriate `DatePart` call.

At the same time, some people prefer having LINQ (or even lambda) expressions that read 'closer' to what the SQL will look like. Additionally, there are some constructs in many SQL dialects that do not have an equivalent operator/function in .NET.

### Useful built in Expressions/Functions (That have no direct equivalent in .NET)

- `Sql.CurrentTimestamp` : Normally, `DateTime` instances passed into queries are parameterized on the client side. This includes `DateTime.Now`. `Sql.CurrentTimestamp` on the other hand will use the database server's current time instead of the time on the .NET server executing the query.
- `Sql.Between` : Allows you to use `Between(myTable.MyColumn, 1, 2)` instead of an expression such as `(myTable.MyColumn >= 1 && myTable.MyColumn <=2)`.
  - `Sql.NotBetween` : The inverse of `Sql.Between`
- `Sql.Reverse` : Reverses a string.
- `Sql.Stuff` : Places a string inside another string, at the specified position. Please read the [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/t-sql/functions/stuff-transact-sql?view=sql-server-2017) for an example of how this function may be used.

## `Sql.ExpressionAttribute`

There are times where you may wish to add a more custom expression into the LINQ provider. Consider the example of NullIf: While you can often use an expression such as `(table.SomeProperty == someValue ? table.SomeProperty : null)`, this would typically become a `CASE` statement, whereas some people may prefer being able to use the database server's built in `NullIf` function via a call like `SqlExpr.NullIf(table.SomeProperty,someValue)`. To do so, they merely would need to have a class such as this:

```cs
using LinqToDB;

namespace MyProject
{
    public static partial class SqlExpr
    {
        [Sql.Expression("NULLIF({0}, {1})", PreferServerSide = true)]
        public static T NullIf<T>(T value, T compareTo) where T: class, IComparable<T>
        {
            return value.HasValue && value.Value.CompareTo(compareTo) == 0 ? null : value;
        }

        [Sql.Expression("NULLIF({0}, {1})", PreferServerSide = true)]
        public static T? NullIf<T>(T? value, T compareTo) where T: struct, IComparable<T>
        {
            return value.HasValue && value.Value.CompareTo(compareTo) == 0 ? null : value;
        }
    }
}
```

Additionally, you may write server-specific patterns as needed. Take the example of checking if a VARCHAR value is convertable to a positive integer and is not a decimal value. In SQL Server, you can do this with some clever use of `ISNUMERIC`. However, in SQLite, the only reliable way to do so is with GLOB. The below example shows how you can specify provider-specifc usages.

```cs
using LinqToDB;

namespace MyProject
{
    public static partial class SqlExpr
    {
        [Sql.Expression(ProviderName.SqlServer, "ISNUMERIC('-' + {0} + '.0e0')", PreferServerSide = true)]
        [Sql.Expression(ProviderName.SQLite, "NOT {0} GLOB '*[^0-9]*' AND {0} LIKE '_%'", PreferServerSide = true)]
        public static bool IsPositiveInteger<T>(T value)
        {
            int checkDecimal = 0;
            if (int.TryParse(value.ToString(), out checkDecimal))
            {
                return checkDecimal.ToString() == value.ToString();
            }
            return false;
        }

    }
}
```

### Useful Attribute Properties

- `PreferServerSide` : This will tell linq2db that you would prefer for the expression to be evaluated on the server if possible.
- `ServerSideOnly` : This will tell linq2db that if the expression cannot be executed on the server (For example, it is inside a another method call that cannot be translated) that an exception should be thrown. This is useful when attempting to guarantee that filtering is done on the server (avoiding transferring large amounts of data.)
- `InlineParameters` : Normally linq2db will parametetrize any variables passed into the expression. If this is set to true however, the values will instead be passed as (escaped) literals. Please note scalar values passed in will always be inlined, regardless of this parameter.
- `IsPredicate` : In the case of expressions that are boolean, linq2db will normally add a `= 1` when the server requires it (such as SqlServer). If this is set to `true`, linq2db will not add this.

## `Sql.FunctionAttribute`

`Sql.FunctionAttribute` is a more specific case of the `ExpressionAttribute`: consider it a convenience method when calling a server function with known parameters. For example, if you wanted to use IsNumeric in a more normal fashion:

```cs
using LinqToDB;

namespace MyProject
{
    public static partial class SqlExpr
    {
        [Sql.Function("IsNumeric", PreferServerSide = true)]
        public static bool IsNumeric(string s)
        {
            double checkDecimal = 0;
            if (double.TryParse(value.ToString(), out checkDecimal))
            {
                return true;
            }
            return false;
        }
    }
}
```
