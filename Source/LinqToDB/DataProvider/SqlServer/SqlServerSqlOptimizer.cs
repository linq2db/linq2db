using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Collections.Generic;
	using Common;
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
			return QueryHelper.WrapQuery(
				(object?)null,
				statement,
				static (_, query, _) => query.ParentSelect == null && (query.Select.SkipValue != null ||
														query.Select.TakeValue != null ||
														query.Select.TakeHints != null || !query.OrderBy.IsEmpty),
				null,
				allowMutation: true,
				withStack: false);
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
						case "Convert":
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

		protected override SqlStatement FixSetOperationColumnTypes(SqlStatement statement)
		{
			statement = base.FixSetOperationColumnTypes(statement);

			// sql server use more strict checks for sets in recursive CTEs, so we need to implement additional fixes for them.
			// List of known limitations:
			// - string types should be same by both type and length
			statement.Visit(static e =>
			{
				if (e is CteClause cte && cte.Body?.HasSetOperators == true)
				{
					var query = cte.Body;

					for (int i = 0; i < query.Select.Columns.Count; i++)
					{
						var commonDataType    = DataType.Undefined;
						var applyConvert      = false;
						SqlField? commonField = null;

						var idx = 0;
						foreach (var column in EnumerateSetColumns(query, i))
						{
							var underlyingField = QueryHelper.GetUnderlyingField(column.Expression);
							var type            = column.Expression.GetExpressionType();

							applyConvert = applyConvert || underlyingField == null;
							commonField  = commonField == underlyingField || idx == 0 ? underlyingField : null;

							switch (type.DataType)
							{
								case DataType.Guid:
									commonDataType = DataType.Guid;
									break;
								case DataType.NVarChar:
								case DataType.NChar:
								case DataType.NText:
									commonDataType = DataType.NVarChar;
									break;
								case DataType.VarChar:
								case DataType.Char:
								case DataType.Text:
									commonDataType = commonDataType == DataType.NVarChar ? DataType.NVarChar : DataType.VarChar;
									break;
								case DataType.Undefined:
									if (type.SystemType == typeof(string))
										commonDataType = DataType.NVarChar;
									break;
							}

							idx++;
						}

						if (commonField != null)
							continue;

						if (applyConvert)
						{
							DbDataType type;

							if (commonDataType == DataType.Guid)
								type = new DbDataType(typeof(Guid), commonDataType);
							else if (commonDataType == DataType.NVarChar || commonDataType == DataType.VarChar)
								type = new DbDataType(typeof(string), commonDataType);
							else
								continue;

							foreach (var column in EnumerateSetColumns(query, i))
								column.Expression = new SqlExpression(column.Expression.SystemType, "Cast({0} as {1})", Precedence.Primary, column.Expression, new SqlDataType(type));
						}
					}
				}
			});

			return statement;
		}

		private static IEnumerable<SqlColumn> EnumerateSetColumns(SelectQuery setQuery, int index)
		{
			yield return setQuery.Select.Columns[index];
			foreach (var set in setQuery.SetOperators)
				yield return set.SelectQuery.Select.Columns[index];
		}
	}
}
