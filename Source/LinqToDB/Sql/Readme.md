# SQL Extensions

This Folder Contains SQL Extensions for Linq2Db, as well as components to help you build your own Extensions.

## `Sql`

The SQL Class contains a number of functions implementing common DB Functions such as `Between` or `CurrentTimestamp`. [The XMLDocs provide a list of all the provided functions](https://linq2db.github.io/api/LinqToDB.Sql.html).

Note that many of these are used automatically behind the scenes. For example, The provider will automatically translate something such as `table.SomeDateTimeField.Year` into the appropriate `DatePart` call.

At the same time, some people prefer having LINQ (or even Lambda expressions) that read 'closer' to what the SQL will look like. And there are some constructs in many SQL dialects that do not have an equivalent operator/function in .NET.

### Useful Built in Expressions/Functions:

 - `Sql.CurrentTimestamp` : Normally, `DateTime` instances passed in are parameterized. This includes `DateTime.Now`. `Sql.CurrentTimestamp` on the other hand will use the SQL Server's current time instead of the time on the .NET server executing the query.

 - `Sql.Between` : Allows you to use `Between(myTable.MyCol1,1,2)` instead of an expression such as `(myTable.MyCol1 >= 1 && myTable.MyCol1 <=2)`.
   - `Sql.NotBetween` : The inverse of `Sql.Between`
 
 - `Sql.Reverse` : Reverses a string.

 - `Sql.Stuff` : Places a string inside another string, at the specified position. Please read the [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/t-sql/functions/stuff-transact-sql?view=sql-server-2017) for an example of how this function may be used.
 

## `Sql.ExpressionAttribute`

There are times where you may wish to add a more custom Expression into the LINQ provider. Consider the example of NullIf: While you can often use an expression such as `(table.SomeProperty == someValue ? table.SomeProperty : null)`, some people may prefer being able to write `SqlExpr.NullIf(table.SomeProperty)`. To do so, they merely would need to have a class such as this:

```
using Linq2db;
namespace MyProject
{
    public static partial class SqlExpr
    {
        [Sql.Expression("NULLIF({0},{1})", PreferServerSide = true)]
        public static T NullIf<T>(T value, T compareTo) where T: class, IComparable<T>
        {
            return value.HasValue && value.Value.CompareTo(compareTo) == 0 ? null : value;
        }

        [Sql.Expression("NULLIF({0},{1})", PreferServerSide = true)]
        public static T? NullIf<T>(T? value, T compareTo) where T: struct, IComparable<T>
        {
            return value.HasValue && value.Value.CompareTo(compareTo) == 0 ? null : value;
        }
    }
}
```

Additionally, you may write Server-Specific patterns as needed. Take the example of checking if a VARCHAR value is convertable to a positive Integer and is not a decimal value. In SQL Server, you can do this with some clever use of `ISNUMERIC`. However, in SQLite, the only reliable way to do so is with GLOB. The below example shows how you can specify provider-specifc usages.

```
using Linq2db;
namespace MyProject
{
    public static partial class SqlExpr
    {
        [Sql.Expression("SqlServer","ISNUMERIC('-' + {0} + '.0e0')", PreferServerSide = true)]
        [Sql.Expression("SQLite","NOT {0} GLOB '*[^0-9]*' AND {0} LIKE '_%'", PreferServerSide = true)]
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

### Useful Attribute Properties:

 - `PreferServerSide` : This will tell Linq2Db that you would prefer for the expression to be evaluated on the server if possible.

 - `ServerSideOnly` : This will tell Linq2Db that if the expression cannot be executed on the server (For example, it is inside a another method call that cannot be translated) that an exception should be thrown. This is useful when attempting to guarantee that filtering is done on the server (avoiding transferring large amounts of data.)

 - `InlineParameters` : Normally Linq2Db will parametetrize any variables passed into the expression. If this is set to true however, the values will instead be passed as (escaped) literals. Please note scalar values passed in will always be inlined, regardless of this parameter.

 - `IsPredicate` : In the case of Expressions that are boolean, Linq2Db will normally add a `= 1` when the server requires it (such as SqlServer). If this is set to `true`, Linq2Db will not add this.  

## `Sql.FunctionAttribute`

`Sql.FunctionAttribute` is a more specific case of Expression: consider it a convenience method when calling a server function with known paarameters. For example, if you wanted to use IsNumeric in a more normal Fashion:

```
using Linq2db;
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