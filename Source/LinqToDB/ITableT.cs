using JetBrains.Annotations;

using LinqToDB.Internal.Linq;

namespace LinqToDB
{
	/// <summary>
	/// Table-like queryable source, e.g. table, view or table-valued function.
	/// </summary>
	/// <typeparam name="T">Record mapping type.</typeparam>
	[PublicAPI]
	public interface ITable<out T> : IExpressionQuery<T>
		// TODO: IT: Review in v6, it should be 'class'.
		where T : notnull
	{
		string?      ServerName   { get; }
		string?      DatabaseName { get; }
		string?      SchemaName   { get; }
		string       TableName    { get; }
		TableOptions TableOptions { get; }
		string?      TableID      { get; }
	}
}
