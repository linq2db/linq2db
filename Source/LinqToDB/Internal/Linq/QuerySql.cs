using System.Collections.Generic;

using LinqToDB.Data;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Command SQL, generated from linq query.
	/// </summary>
	public sealed class QuerySql(string sql, IReadOnlyList<DataParameter> parameters)
	{
		/// <summary>
		/// Command SQL text.
		/// </summary>
		public string                       Sql        => sql;
		/// <summary>
		/// Command parameters with values.
		/// </summary>
		public IReadOnlyList<DataParameter> Parameters => parameters;
	}
}
