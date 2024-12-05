using System.Collections.Generic;

using LinqToDB.Data;

namespace LinqToDB.Linq
{
	public sealed record QuerySql(string Sql, IReadOnlyList<DataParameter> Parameters);
}
