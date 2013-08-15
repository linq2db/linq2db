using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	public interface ISqlBuilder
	{
		int              CommandCount         (SelectQuery selectQuery);
		void             BuildSql             (int commandNumber, SelectQuery selectQuery, StringBuilder sb, int indent, bool skipAlias);
		ISqlExpression   ConvertExpression    (ISqlExpression expression);
		ISqlPredicate    ConvertPredicate     (ISqlPredicate  predicate);
		SelectQuery      Finalize             (SelectQuery selectQuery);

		StringBuilder    BuildTableName       (StringBuilder sb, string database, string owner, string table);
		object           Convert              (object value, ConvertType convertType);
		ISqlExpression   GetIdentityExpression(SqlTable table, SqlField identityField, bool forReturning);

		string           Name        { get; }
		SelectQuery      SelectQuery { get; set; }
	}
}
