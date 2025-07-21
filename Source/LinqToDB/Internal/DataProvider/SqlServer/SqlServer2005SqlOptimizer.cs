﻿using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServer2005SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2005SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2005)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlServer2005SqlExpressionConvertVisitor(allowModify, SQLVersion);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			//SQL Server 2005 supports ROW_NUMBER but not OFFSET/FETCH

			statement = SeparateDistinctFromPagination(statement, q => q.Select.TakeValue != null || q.Select.SkipValue != null);

			if (statement.IsUpdate() || statement.IsDelete())
				statement = WrapRootTakeSkipOrderBy(statement);

			statement = ReplaceSkipWithRowNumber(statement, mappingSchema);

			return statement;
		}
	}
}
