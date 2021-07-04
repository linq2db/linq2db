namespace LinqToDB.CodeGen.Metadata
{
	public record Column(string Name, string? Description, DbType DbType, bool Insertable, bool Updateable);
}
