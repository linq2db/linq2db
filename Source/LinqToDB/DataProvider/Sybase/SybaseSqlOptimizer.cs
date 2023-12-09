using System;
using System.Collections.Generic;
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

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			return statement.QueryType switch
			{
				QueryType.Update => PrepareUpdateStatement((SqlUpdateStatement)statement),
				_ => statement,
			};
		}

		static SqlStatement PrepareUpdateStatement(SqlUpdateStatement statement)
		{
			var tableToUpdate = statement.Update.Table;

			if (tableToUpdate == null)
				return statement;

			if (statement.SelectQuery.From.Tables.Count > 0)
			{
				if (ReferenceEquals(tableToUpdate, statement.SelectQuery.From.Tables[0].Source))
					return statement;

				var sourceTable = statement.SelectQuery.From.Tables[0];

				for (int i = 0; i < sourceTable.Joins.Count; i++)
				{
					var join = sourceTable.Joins[i];
					if (ReferenceEquals(join.Table.Source, tableToUpdate))
					{
						var sources = new[] { tableToUpdate };
						if (sourceTable.Joins.Skip(i + 1).Any(j => QueryHelper.IsDependsOnSources(j, sources)))
							break;
						statement.SelectQuery.From.Tables.Insert(0, join.Table);
						statement.SelectQuery.Where.SearchCondition.EnsureConjunction().Predicates
							.Add(new SqlCondition(false, join.Condition));

						sourceTable.Joins.RemoveAt(i);

						break;
					}
				}
			}

			return statement;
		}
	}
}
