﻿namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	class SqlServer2000SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2000SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2000)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			// very limited provider, it do not support Window functions.

			if (statement.IsUpdate())
			{
				var selectQuery = statement.SelectQuery!;
				if (selectQuery.Select.SkipValue != null || selectQuery.Select.TakeValue != null)
					throw new LinqToDBException("SQL Server 2000 do not support Skip, Take in Update statement.");

				if (!statement.SelectQuery!.OrderBy.IsEmpty)
				{
					statement.SelectQuery.OrderBy.Items.Clear();
				}
			}

			return statement;
		}
	}
}
