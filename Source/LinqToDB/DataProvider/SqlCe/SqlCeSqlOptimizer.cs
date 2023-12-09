using System;

namespace LinqToDB.DataProvider.SqlCe
{
	using SqlQuery;
	using SqlProvider;

	sealed class SqlCeSqlOptimizer : BasicSqlOptimizer
	{
		public SqlCeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlCeSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			// This function mutates statement which is allowed only in this place
			CorrectSkipAndColumns(statement);

			// This function mutates statement which is allowed only in this place
			CorrectInsertParameters(statement);

			CorrectFunctionParameters(statement, dataOptions);

			statement = CorrectBooleanComparison(statement);

			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SqlDeleteStatement) statement, dataOptions);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SqlUpdateStatement) statement, dataOptions);
					break;
			}

			// call fixer after CorrectSkipAndColumns for remaining cases
			base.FixEmptySelect(statement);

			return statement;
		}

		void CorrectInsertParameters(SqlStatement statement)
		{
			//SlqCe do not support parameters in columns for insert
			//
			if (statement.IsInsert())
			{
				var query = statement.SelectQuery;
				if (query != null)
				{
					foreach (var column in query.Select.Columns)
					{
						if (column.Expression is SqlParameter parameter)
						{
							parameter.IsQueryParameter = false;
						}
					}
				}
			}
		}

		static void CorrectSkipAndColumns(SqlStatement statement)
		{
			statement.Visit(static e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlQuery:
						{
							var q = (SelectQuery)e;

							if (q.Select.SkipValue != null && q.OrderBy.IsEmpty)
							{
								if (q.Select.Columns.Count == 0)
								{
									var source = q.Select.From.Tables[0].Source;
									var keys   = source.GetKeys(true);

									if (keys != null)
									{
										foreach (var key in keys)
										{
											q.Select.AddNew(key);
										}
									}
								}

								for (var i = 0; i < q.Select.Columns.Count; i++)
									q.OrderBy.ExprAsc(q.Select.Columns[i].Expression);

								if (q.OrderBy.IsEmpty)
								{
									throw new LinqToDBException("Order by required for Skip operation.");
								}
							}

							// looks like SqlCE do not allow '*' for grouped records
							if (!q.GroupBy.IsEmpty && q.Select.Columns.Count == 0)
							{
								q.Select.Add(new SqlValue(1));
							}

							break;
						}
				}
			});
		}

		static void CorrectFunctionParameters(SqlStatement statement, DataOptions options)
		{
			if (!options.FindOrDefault(SqlCeOptions.Default).InlineFunctionParameters)
				return;

			statement.Visit(static e =>
			{
				if (e.ElementType == QueryElementType.SqlFunction)
				{
					var sqlFunction = (SqlFunction)e;
					foreach (var parameter in sqlFunction.Parameters)
					{
						if (parameter.ElementType == QueryElementType.SqlParameter &&
						    parameter is SqlParameter sqlParameter)
						{
							sqlParameter.IsQueryParameter = false;
						}
					}
				}
			});
		}

		protected override void FixEmptySelect(SqlStatement statement)
		{
			// already fixed by CorrectSkipAndColumns
		}

		private SqlStatement CorrectBooleanComparison(SqlStatement statement)
		{
			statement = statement.ConvertAll(this, true, static (_, e) =>
			{
				if (e.ElementType == QueryElementType.IsTruePredicate)
				{
					var isTruePredicate = (SqlPredicate.IsTrue)e;
					if (isTruePredicate.Expr1 is SelectQuery query && query.Select.Columns.Count == 1)
					{
						query.Select.Where.EnsureConjunction();
						query.Select.Where.SearchCondition.Predicates.Add(new SqlCondition(false,
							new SqlPredicate.IsTrue(query.Select.Columns[0].Expression, isTruePredicate.TrueValue,
								isTruePredicate.FalseValue, isTruePredicate.WithNull, isTruePredicate.IsNot)));
						query.Select.Columns.Clear();

						return new SqlPredicate.FuncLike(SqlFunction.CreateExists(query));
					}
				}

				return e;
			});

			return statement;
		}
	}
}
