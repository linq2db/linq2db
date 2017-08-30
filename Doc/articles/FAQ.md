# Mapping
## How can I use calculated fields?

The only you need is to say that you do not want to insert or update property, and you can do it with `ColumnAttribute`
```cs
public class MyEntity
{
    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public int CalculatedField { get; set; }
}
```