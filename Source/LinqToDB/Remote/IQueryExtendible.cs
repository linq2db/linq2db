namespace LinqToDB.Remote
{
	using SqlQuery;

	interface IQueryExtendible
	{
		List<SqlQueryExtension>? SqlQueryExtensions { get; set; }
	}
}
