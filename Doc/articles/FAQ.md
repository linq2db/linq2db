# Mapping

## Do I need to use Attribute and/or Code first Mapping?

Not Strictly. It is possible to use Linq2Db with simple, non-attributed POCOs, however there will be specific limitations. 
 - The biggest of these is that the `string` type is nullable by default in .NET, and unlike with `int` or `double` there is no way for Linq2Db to infer nullability. This can cause problems in certain cases, such as if you are ever required to join two `VARCHAR` fields together.

 - Table and Column Names will have to match the Class and Property names.
   - You can get around this for the Class itself by using the `.TableName()` Method after your `GetTable<>` call (e.x.  `conn.GetTable<MyCleanClassName>().TableName("my_not_so_clean_table_name")` )

 - Unless using the Explicit Insert/Update syntax (i.e. `.Value()`/`.Set()`), all columns will be written off the supplied POCO.

## How can I use calculated fields?

You need to mark them to be ignored during insert or update operations, e.g. using `ColumnAttribute` attribute:
```cs
public class MyEntity
{
    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public int CalculatedField { get; set; }
}
```