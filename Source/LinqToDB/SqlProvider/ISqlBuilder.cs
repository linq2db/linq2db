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

		StringBuilder    ConvertTableName     (StringBuilder sb, string? server, string? database, string? schema, string table);
		StringBuilder    BuildTableName       (StringBuilder sb, string? server, string? database, string? schema, string table);
		string           ConvertInline        (string value, ConvertType convertType);
		StringBuilder    Convert              (StringBuilder sb, string value, ConvertType convertType);
		ISqlExpression?  GetIdentityExpression(SqlTable table);

		StringBuilder    PrintParameters      (StringBuilder sb, IEnumerable<IDbDataParameter>? parameters);
		string           ApplyQueryHints      (string sqlText, List<string> queryHints);

		string           GetReserveSequenceValuesSql(int count, string sequenceName);
		string           GetMaxValueSql       (EntityDescriptor entity, ColumnDescriptor column);

		string           Name { get; }

		List<SqlParameter> ActualParameters { get; }
	}
}
