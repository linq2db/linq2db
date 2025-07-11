using System.Collections.Generic;

using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlObjectNameComparer : IComparer<SqlObjectName>
	{
		public static readonly IComparer<SqlObjectName> Instance = new SqlObjectNameComparer();

		private SqlObjectNameComparer()
		{
		}

		int IComparer<SqlObjectName>.Compare(SqlObjectName x, SqlObjectName y)
		{
			var res = string.CompareOrdinal(x.Server, y.Server);
			if (res != 0) return res;
			res = string.CompareOrdinal(x.Database, y.Database);
			if (res != 0) return res;
			res = string.CompareOrdinal(x.Schema, y.Schema);
			if (res != 0) return res;
			res = string.CompareOrdinal(x.Package, y.Package);
			if (res != 0) return res;
			return string.CompareOrdinal(x.Name, y.Name);
		}
	}
}
