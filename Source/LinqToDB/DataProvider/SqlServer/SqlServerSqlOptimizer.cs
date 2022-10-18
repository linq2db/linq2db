using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Extensions;
	using LinqToDB.Common;
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

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			if (func.Name == "Coalesce" && func.Parameters.Length == 2)
			{
				if (func.Parameters[0].IsComplexEvaluationExpression())
				{
					var parameters = new[] { func.Parameters[0], func.Parameters[1] };

					var type1 = parameters[0].GetExpressionType();
					var type2 = parameters[1].GetExpressionType();
					
					var resultType = GetWideType(type1, type2);
					if (type1 != resultType)
						parameters[0] = new SqlFunction(func.SystemType, "CAST({0} AS {1})", parameters[0], new SqlDataType(resultType));

					func = new SqlFunction(func.SystemType, "ISNULL", parameters);
				}
			}

			return base.ConvertFunction(func);
		}

		// ISNULL type result by type of first argument, so if second argument type is wider, we should cast
		// first argument to type, created as wide version of both types
		// or data loss could occur for second parameter
		private DbDataType GetWideType(DbDataType type1, DbDataType type2)
		{
			var result = type1;

			// length:
			// null means MAX
			if (type1.Length != null && (type2.Length == null || type1.Length.Value < type2.Length.Value))
				result = result.WithLength(type2.Length);

			// strings and (non)unicode
			var isType1Unicode = type1.DataType == DataType.NChar || type1.DataType == DataType.NVarChar || type1.DataType == DataType.NText;
			var isType2Unicode = type2.DataType == DataType.NChar || type2.DataType == DataType.NVarChar || type2.DataType == DataType.NText;
			var isType1Fixed   = type1.DataType == DataType.NChar || type1.DataType == DataType.Char;
			var isType2Fixed   = type2.DataType == DataType.NChar || type2.DataType == DataType.Char;
			if (!isType1Unicode && isType2Unicode)
			{
				result = result.WithDataType(DataType.NVarChar);
			}
			else if (isType1Fixed || isType2Fixed)
			{
				result = result.WithDataType(isType1Unicode ? DataType.NVarChar : DataType.VarChar);
			}

			// binary/varbinary
			if (type1.DataType == DataType.Binary || type2.DataType == DataType.Binary)
			{
				result = result.WithDataType(DataType.VarBinary);
			}

			// precision/scale
			// null means type-specific default value
			int? defaultPrecision = null;
			int? defaultScale     = null;
			if (type1.DataType == DataType.SmallMoney || type2.DataType == DataType.SmallMoney)
			{
				defaultPrecision = 10;
				defaultScale     = 4;
			}
			if (type1.DataType == DataType.Decimal || type2.DataType == DataType.Decimal)
			{
				defaultPrecision = 18;
				defaultScale     = 0;
			}
			if (type1.DataType == DataType.Money || type2.DataType == DataType.Money)
			{
				defaultPrecision = 19;
				defaultScale     = 4;
			}

			if (type1.DataType != type2.DataType && defaultPrecision != null)
				result = result.WithDataType(DataType.Decimal);

			if (   type1.DataType == DataType.DateTime2 || type1.DataType == DataType.DateTimeOffset || type1.DataType == DataType.Time
				|| type2.DataType == DataType.DateTime2 || type2.DataType == DataType.DateTimeOffset || type2.DataType == DataType.Time)
			{
				if (type1.DataType != type2.DataType)
				{
					if (type2.DataType == DataType.DateTimeOffset)
						result = result.WithDataType(DataType.DateTimeOffset);
					else if (type2.DataType == DataType.DateTime2)
						result = result.WithDataType(DataType.DateTime2);
				}

				defaultPrecision = 7;
			}

			if (defaultPrecision != null)
			{
				var precision1 = type1.Precision ?? defaultPrecision;
				var precision2 = type2.Precision ?? defaultPrecision;
				if (precision1 < precision2)
					result = result.WithPrecision(precision2);
			}

			if (defaultScale != null)
			{
				var precision1 = type1.Precision ?? defaultPrecision;
				var precision2 = type2.Precision ?? defaultPrecision;
				if (precision1 < precision2)
					result = result.WithPrecision(precision2);
			}

			return result;
		}
	}
}
