using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

	public interface ISqlBuilder
	{
		int              CommandCount         (SqlStatement statement);
		void             BuildSql             (int commandNumber, SqlStatement statement, StringBuilder sb, int startIndent = 0);

		StringBuilder    ConvertTableName     (StringBuilder sb, string database, string schema, string table);
		StringBuilder    BuildTableName       (StringBuilder sb, string database, string schema, string table);
		object           Convert              (object value, ConvertType convertType);
		ISqlExpression   GetIdentityExpression(SqlTable table);

		StringBuilder    PrintParameters      (StringBuilder sb, IDbDataParameter[] parameters);
		string           ApplyQueryHints      (string sqlText, List<string> queryHints);

		string           GetReserveSequenceValuesSql(int count, string sequenceName);
		string           GetMaxValueSql       (EntityDescriptor entity, ColumnDescriptor column);

		string           Name { get; }
	}
}
