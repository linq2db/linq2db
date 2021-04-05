using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using System.Data.Common;
	using Mapping;
	using SqlQuery;

	public interface ISqlBuilder
	{
		int              CommandCount         (SqlStatement statement);
		void             BuildSql             (int commandNumber, SqlStatement statement, StringBuilder sb, OptimizationContext optimizationContext, int startIndent = 0);

		StringBuilder    ConvertTableName     (StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions);
		StringBuilder    BuildTableName       (StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions);
		string?          GetTableServerName   (SqlTable table);
		string?          GetTableDatabaseName (SqlTable table);
		string?          GetTableSchemaName   (SqlTable table);
		string?          GetTablePhysicalName (SqlTable table);
		string           ConvertInline        (string value, ConvertType convertType);
		StringBuilder    Convert              (StringBuilder sb, string value, ConvertType convertType);
		ISqlExpression?  GetIdentityExpression(SqlTable table);

		StringBuilder    PrintParameters      (StringBuilder sb, IEnumerable<DbParameter>? parameters);
		string           ApplyQueryHints      (string sqlText, List<string> queryHints);

		string           GetReserveSequenceValuesSql(int count, string sequenceName);
		string           GetMaxValueSql       (EntityDescriptor entity, ColumnDescriptor column);

		string Name { get; }
	}
}
