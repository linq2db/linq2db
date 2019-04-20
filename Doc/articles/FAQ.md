# Mapping

## Do I need to use Attribute and/or Code first Mapping?

Not strictly. It is possible to use linq2db with simple, non-attributed POCOs, however there will be specific limitations. 
 - The biggest of these is that the `string` type is nullable by default in .NET, and unlike with `int` or `double` there is no way for linq2db to infer nullability. This can cause problems in certain cases, such as if you are ever required to join two `VARCHAR` fields together.

 - Table and column names will have to match the class and property names.
   - You can get around this for the class itself by using the `.TableName()` Method after your `GetTable<>` call (e.x.  `conn.GetTable<MyCleanClassName>().TableName("my_not_so_clean_table_name")` )

 - Unless using the explicit insert/update syntax (i.e. `.Value()`/`.Set()`), all columns will be written off the supplied POCO.

## How can I use calculated fields?

You need to mark them to be ignored during insert or update operations, e.g. using `ColumnAttribute` attribute:
```cs
public class MyEntity
{
    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public int CalculatedField { get; set; }
}
```