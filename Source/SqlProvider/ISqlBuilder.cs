using System;
using System.Data;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	public interface ISqlBuilder
	{
		int              CommandCount         (SelectQuery selectQuery);
		void             BuildSql             (int commandNumber, SelectQuery selectQuery, StringBuilder sb);

		StringBuilder    BuildTableName       (StringBuilder sb, string database, string owner, string table);
		object           Convert              (object value, ConvertType convertType);
		ISqlExpression   GetIdentityExpression(SqlTable table);

		StringBuilder    PrintParameters      (StringBuilder sb, IDbDataParameter[] parameters);

		string           Name { get; }
	}
}
