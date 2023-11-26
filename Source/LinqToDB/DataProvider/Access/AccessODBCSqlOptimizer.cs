using System;

namespace LinqToDB.DataProvider.Access
{
	using LinqToDB.SqlQuery.Visitors;

	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class AccessODBCSqlOptimizer : AccessSqlOptimizer
	{
		public AccessODBCSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement = base.Finalize(mappingSchema, statement, dataOptions);

			statement = WrapParameters(statement);

			return statement;
		}

		private static SqlStatement WrapParameters(SqlStatement statement)
		{
			// System.Data.Odbc cannot handle types if they are not in a list of hardcoded types.
			// Here we try to avoid FromSqlType to fail when ODBC Access driver returns 0 for type
			// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Data.Odbc/src/System/Data/Odbc/Odbc32.cs#L935
			//
			// This is a bug in Access ODBC driver where it returns no type information for NULL/parameter-based column if query is:
			// 1. SELECT DISTINCT query with FROM clause
			// 2. column contains parameter or NULL directly, without additional expressions
			// See AccessTests.TestParametersWrapping + Distinct5/Distinct6 tests

			// only SELECT query could return dataset in ACCESS
			if (statement.QueryType != QueryType.Select || statement.SelectQuery?.Select.IsDistinct != true || statement.SelectQuery.From.Tables.Count == 0)
				return statement;

			// there is no need in visitors for this fix as issue scope is very narrow
			foreach (var column in statement.SelectQuery.Select.Columns)
			{
				var expr = QueryHelper.UnwrapExpression(column.Expression, false);

				if (expr is SqlParameter || (expr is SqlValue v && v.Value == null))
					column.Expression = new SqlFunction(typeof(object), "CVar", false, true, Precedence.Primary, ParametersNullabilityType.SameAsFirstParameter, null, column.Expression);
			}

			return statement;
		}
	}
}
