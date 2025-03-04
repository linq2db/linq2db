namespace LinqToDB.Internal.DataProvider
{
	public enum IdentifierKind
	{
		Table,
		Field,
		Index,
		ForeignKey,
		PrimaryKey,
		UniqueKey,
		Sequence,
		Trigger,
		StoredProcedure,
		Function,
		View,
		Database,
		Schema,
		Alias,
		Parameter,
		Variable,
		Keyword,
		DataType,
		Other
	}
}
