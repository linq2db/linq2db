using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	abstract class SqlServerSqlOptimizer : BasicSqlOptimizer
	{
		private readonly SqlServerVersion _sqlVersion;

		protected SqlServerSqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion sqlVersion) : base(sqlProviderFlags)
		{
			_sqlVersion = sqlVersion;
		}

		protected SqlStatement ReplaceSkipWithRowNumber(SqlStatement statement)
			=> ReplaceTakeSkipWithRowNumber((object?)null, statement, static (_, query) => query.Select.SkipValue != null, false);

		protected SqlStatement WrapRootTakeSkipOrderBy(SqlStatement statement)
		{
			var query = statement.SelectQuery;
			if (query == null)
				return statement;

			if ((query.Select.SkipValue != null ||
			     query.Select.TakeValue != null ||
			     query.Select.TakeHints != null) && !query.OrderBy.IsEmpty)
			{
				statement = QueryHelper.WrapQuery(statement, query, true);
			}

			return statement;
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			var like = base.ConvertSearchStringPredicate(predicate, visitor);

			if (predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									typeof(string), "LEFT", predicate.Expr1,
									new SqlFunction(typeof(int), "Length", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									typeof(string), "RIGHT", predicate.Expr1,
									new SqlFunction(typeof(int), "Length", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "CHARINDEX",
									new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr2),
									new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr1)),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(
						new SqlCondition(false, like, predicate.IsNot),
						new SqlCondition(predicate.IsNot, subStrPredicate));

					return result;
				}
			}

			return like;
		}

		protected override SqlStatement FinalizeUpdate(SqlStatement statement)
		{
			var newStatement = base.FinalizeUpdate(statement);

			if (newStatement is SqlUpdateStatement updateStatement)
			{
				updateStatement = CorrectSqlServerUpdate(updateStatement);
				newStatement    = updateStatement;
			}

			return newStatement;
		}

		static bool IsUpdateUsingSingeTable(SqlUpdateStatement updateStatement)
		{
			return QueryHelper.IsSingleTableInQuery(updateStatement.SelectQuery, updateStatement.Update.Table!);
		}

		SqlUpdateStatement CorrectSqlServerUpdate(SqlUpdateStatement updateStatement)
		{
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException();

			var correctionFinished = false;

			SqlTableSource? removedTableSource = null;

			var hasUpdateTableInQuery = QueryHelper.HasTableInQuery(updateStatement.SelectQuery, updateStatement.Update.Table);

			if (hasUpdateTableInQuery)
			{
				// do not remove if there is other tables
				if (QueryHelper.EnumerateAccessibleTables(updateStatement.SelectQuery).Take(2).Count() == 1)
				{
					if (RemoveUpdateTableIfPossible(updateStatement.SelectQuery, updateStatement.Update.Table, out removedTableSource))
						hasUpdateTableInQuery = false;
				}
			}

			if (hasUpdateTableInQuery)
			{
				// handle simple UPDATE TOP n case
				if (updateStatement.SelectQuery.Select.SkipValue == null && updateStatement.SelectQuery.Select.TakeValue != null)
				{
					if (IsUpdateUsingSingeTable(updateStatement))
					{
						updateStatement.SelectQuery.From.Tables.Clear();
						updateStatement.Update.TableSource = null;
						correctionFinished = true;
					}
				}

				if (!correctionFinished)
				{
					var isCompatibleForUpdate = IsCompatibleForUpdate(updateStatement.SelectQuery, updateStatement.Update.Table);
					if (isCompatibleForUpdate)
					{	
						// for OUTPUT we have to use datached variant
						if (!IsUpdateUsingSingeTable(updateStatement) && updateStatement.Output != null)
						{
							// check that UpdateTable is visible for SET and OUTPUT
							if (QueryHelper.EnumerateLevelSources(updateStatement.SelectQuery).All(e => e.Source != updateStatement.Update.Table))
							{
								isCompatibleForUpdate = false;
							}
						}
					}

					if (isCompatibleForUpdate)
					{
						var needsWrapping = updateStatement.SelectQuery.Select.SkipValue != null;
						if (needsWrapping)
						{
							updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery, true);
						}

						var (ts, path) = FindTableSource(new Stack<IQueryElement>(), updateStatement.SelectQuery,
							updateStatement.Update.Table!);

						updateStatement.Update.TableSource = ts;
					}
					else
					{
						updateStatement = DetachUpdateTableFromUpdateQuery(updateStatement);

						var sqlTableSource = removedTableSource ??
						                     new SqlTableSource(updateStatement.Update.Table!,
							                     QueryHelper.SuggestTableSourceAlias(updateStatement.SelectQuery, "u"));

						updateStatement.SelectQuery.From.Tables.Insert(0, sqlTableSource);
						updateStatement.Update.TableSource = sqlTableSource;
					}
				}
			}

			updateStatement.Update.Table!.Alias = "$F";

			//if (updateStatement.Update.TableSource == null)
			{
				CorrectUpdateSetters(updateStatement);
			}

			return updateStatement;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			switch (expression.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:
					{
						var be = (SqlBinaryExpression)expression;

						switch (be.Operation)
						{
							case "%":
								{
									var type1 = be.Expr1.SystemType!.ToUnderlying();

									if (type1 == typeof(double) || type1 == typeof(float))
									{
										return new SqlBinaryExpression(
											be.Expr2.SystemType!,
											new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, be.Expr1),
											be.Operation,
											be.Expr2);
									}

									break;
								}
						}

						break;
					}

				case QueryElementType.SqlFunction:
					{
						var func = (SqlFunction)expression;

						switch (func.Name)
						{
							case "Convert" :
								{
									if (func.SystemType.ToUnderlying() == typeof(ulong) &&
										func.Parameters[1].SystemType!.IsFloatType())
										return new SqlFunction(
											func.SystemType,
											func.Name,
											false,
											func.Precedence,
											func.Parameters[0],
											new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

									if (Type.GetTypeCode(func.SystemType.ToUnderlying()) == TypeCode.DateTime)
									{
										var type1 = func.Parameters[1].SystemType!.ToUnderlying();

										if (IsTimeDataType(func.Parameters[0]))
										{
											if (type1 == typeof(DateTimeOffset) || type1 == typeof(DateTime))
												if (_sqlVersion >= SqlServerVersion.v2008)
													return new SqlExpression(
														func.SystemType, "CAST({0} AS TIME)", Precedence.Primary, func.Parameters[1]);
												else
													return new SqlExpression(
														func.SystemType, "Cast(Convert(Char, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

											if (func.Parameters[1].SystemType == typeof(string))
												return func.Parameters[1];

											return new SqlExpression(
												func.SystemType, "Convert(Char, {0}, 114)", Precedence.Primary, func.Parameters[1]);
										}

										if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
										{
											if (IsDateDataType(func.Parameters[0], "Datetime"))
												return new SqlExpression(
													func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
										}

										if (func.Parameters.Length == 2 && func.Parameters[0] is SqlDataType && func.Parameters[0] == SqlDataType.DateTime)
											return new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, func.Parameters[0], func.Parameters[1], new SqlValue(120));
									}


									break;
								}
						}

						break;
					}
			}

			return expression;
		}

	}
}
