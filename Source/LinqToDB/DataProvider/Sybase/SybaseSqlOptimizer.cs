using System;
using System.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using SqlProvider;
	using SqlQuery;

	sealed class SybaseSqlOptimizer : BasicSqlOptimizer
	{
		public SybaseSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SybaseSqlExpressionConvertVisitor(allowModify);
		}

		protected override SqlStatement FinalizeUpdate(SqlStatement statement, DataOptions dataOptions)
		{
			if (statement.QueryType == QueryType.Update)
				return CorrectSybaseUpdate((SqlUpdateStatement)statement, dataOptions);

			return base.FinalizeUpdate(statement, dataOptions);
		}

		SqlUpdateStatement CorrectSybaseUpdate(SqlUpdateStatement statement, DataOptions dataOptions)
		{
			if (statement.SelectQuery.Select.TakeValue != null)
				throw new LinqToDBException("The Sybase ASE does not support the UPDATE statement with the TOP clause.");

			if (statement.SelectQuery.Select.SkipValue != null)
				throw new LinqToDBException("The Sybase ASE does not support the UPDATE statement with the SKIP clause.");

			CorrectUpdateSetters(statement);

			var isInCompatible = QueryHelper.EnumerateAccessibleSources(statement.SelectQuery).Any(t =>
			{
				if (t != statement.SelectQuery && t is SelectQuery)
					return true;
				if (t is SqlTable table && !ReferenceEquals(table, statement.Update.Table) && QueryHelper.IsEqualTables(table, statement.Update.Table))
					return true;
				return false;
			});

			if (isInCompatible)
			{
				statement = GetAlternativeUpdate(statement, dataOptions);
			}
			else
			{
				var hasTableInQuery = QueryHelper.HasTableInQuery(statement.SelectQuery, statement.Update.Table!);
				if (hasTableInQuery && !RemoveUpdateTableIfPossible(statement.SelectQuery, statement.Update.Table!, out _))
					statement = GetAlternativeUpdate(statement, dataOptions);
			}

			return statement;
		}
	}
}
