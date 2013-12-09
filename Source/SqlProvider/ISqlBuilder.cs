using System;
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

        string Name { get; }
        bool UseQueryText { get; set; }
	}
}
