using System.Collections.Generic;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Common;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class ClickHouseSqlOptimizer : BasicSqlOptimizer
	{
		public ClickHouseSqlOptimizer(SqlProviderFlags sqlProviderFlags, DataOptions dataOptions) : base(sqlProviderFlags)
		{
			_dataOptions = dataOptions;
		}

		readonly DataOptions _dataOptions;

		ClickHouseOptions?   _providerOptions;
		public ClickHouseOptions ProviderOptions => _providerOptions ??= _dataOptions.FindOrDefault(ClickHouseOptions.Default);

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions)
		{
			statement = base.FinalizeStatement(statement, context, dataOptions);

			statement = DisableParameters(statement);

			statement = FixCteAliases(statement);

			return statement;
		}

		private SqlStatement DisableParameters(SqlStatement statement)
		{
			// We disable parameters completely as parameters support is very poor across providers:
			// - big difference in behavior of parameters between providers
			// - not all places could accept parameters (e.g. due to provider limitation)
			//
			// E.g. see https://github.com/Octonica/ClickHouseClient/issues/49
			statement = statement.Convert(static (visitor, e) =>
			{
				if (e is SqlParameter p)
					p.IsQueryParameter = false;

				return e;
			});

			return statement;
		}

		private SqlStatement FixCteAliases(SqlStatement statement)
		{
			// CTE clause in ClickHouse currently doesn't support field list, so we should ensure
			// that CTE query use same field names as we generate for CTE table
			//
			// Issue (has PR): https://github.com/ClickHouse/ClickHouse/issues/22932
			// After it fixed we probably need to introduce dialects to provider for backward compat
			statement = statement.Convert(static (visitor, e) =>
			{
				if (e is CteClause cte)
				{
					for (var i = 0; i < cte.Fields.Count; i++)
						cte.Body!.Select.Columns[i].RawAlias = cte.Fields[i].Alias ?? cte.Fields[i].PhysicalName;

					// block rewrite of alias
					cte.Body!.DoNotSetAliases = true;
				}

				return e;
			});

			return statement;
		}

		#region Predicates

		#region LIKE

		// https://clickhouse.com/docs/en/sql-reference/ansi/#feature-status E061-05
		// https://clickhouse.com/docs/en/sql-reference/operators/#like-function

		public override bool     LikeIsEscapeSupported  => false;
		public override string   LikeEscapeCharacter    => "\\";
		public override string[] LikeCharactersToEscape => ClickHouseLikeCharactersToEscape;

		private static readonly string[] ClickHouseLikeCharactersToEscape = { "%", "_" };

		public override ISqlPredicate ConvertLikePredicate(MappingSchema mappingSchema, SqlPredicate.Like predicate, EvaluationContext context)
		{
			// remove ESCAPE clause
			if (predicate.Escape != null)
				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, predicate.Expr2, null);

			return base.ConvertLikePredicate(mappingSchema, predicate, context);
		}

		#endregion

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context)
				?? true;

			var searchExpr = predicate.Expr2;
			var dataExpr   = predicate.Expr1;

			SqlPredicate.Expr? subStrPredicate = null;

			switch (predicate.Kind)
			{
				case SqlPredicate.SearchString.SearchKind.StartsWith:
					if (!caseSensitive)
						subStrPredicate = new SqlPredicate.Expr(
							new SqlFunction(typeof(bool), "startsWith", false, true, Precedence.Primary,
								ParametersNullabilityType.IfAnyParameterNullable, null,
								PseudoFunctions.MakeToLower(dataExpr), PseudoFunctions.MakeToLower(searchExpr)));
					else
						subStrPredicate = new SqlPredicate.Expr(
							new SqlFunction(typeof(bool), "startsWith", false, true, Precedence.Primary,
								ParametersNullabilityType.IfAnyParameterNullable, null, dataExpr, searchExpr));
					break;

				case SqlPredicate.SearchString.SearchKind.EndsWith:
					if (!caseSensitive)
						subStrPredicate = new SqlPredicate.Expr(
							new SqlFunction(typeof(bool), "endsWith", false, true, Precedence.Primary,
								ParametersNullabilityType.IfAnyParameterNullable, null,
								PseudoFunctions.MakeToLower(dataExpr), PseudoFunctions.MakeToLower(searchExpr)));
					else
						subStrPredicate = new SqlPredicate.Expr(
							new SqlFunction(typeof(bool), "endsWith", false, true, Precedence.Primary,
								ParametersNullabilityType.IfAnyParameterNullable, null, dataExpr, searchExpr));
					break;

				case SqlPredicate.SearchString.SearchKind.Contains:
					subStrPredicate = new SqlPredicate.ExprExpr(
						new SqlFunction(typeof(bool), caseSensitive ? "position" : "positionCaseInsensitive", false, true, Precedence.Primary,
							ParametersNullabilityType.IfAnyParameterNullable, null, dataExpr, searchExpr),
						SqlPredicate.Operator.Greater,
						new SqlValue(0),
						null);
					break;
			}

			if (subStrPredicate != null)
				return new SqlSearchCondition(new SqlCondition(predicate.IsNot, subStrPredicate));

			return base.ConvertSearchStringPredicate(predicate, visitor);
		}

		#endregion

		#region Function/Expression Conversions

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			switch (expression)
			{
				case SqlBinaryExpression(var type, var left, "%", var right):
				{
					// % operation not implemented for decimal arguments and we need to cast them to supported type
					// also see https://github.com/ClickHouse/ClickHouse/issues/39287

					var leftType  = left .GetExpressionType();
					var rightType = right.GetExpressionType();
					var rewrite   = false;

					if (leftType.DataType is DataType.Decimal32 or DataType.Decimal64 or DataType.Decimal128 or DataType.Decimal256)
					{
						left = ConvertFunction(visitor.Context.Nullability, PseudoFunctions.MakeConvert(
							new SqlDataType(new DbDataType(typeof(double), DataType.Double)),
							new SqlDataType(leftType),
							left));
						rewrite = true;
					}

					if (rightType.DataType is DataType.Decimal32 or DataType.Decimal64 or DataType.Decimal128 or DataType.Decimal256)
					{
						right = ConvertFunction(visitor.Context.Nullability, PseudoFunctions.MakeConvert(
							new SqlDataType(new DbDataType(typeof(double), DataType.Double)),
							new SqlDataType(rightType),
							right));
						rewrite = true;
					}

					return !rewrite
						? expression
						: ConvertFunction(visitor.Context.Nullability, PseudoFunctions.MakeConvert(
							new SqlDataType(expression.GetExpressionType()),
							new SqlDataType(new DbDataType(typeof(double), DataType.Double)),
							new SqlBinaryExpression(typeof(double), left, "%", right)));
				}

				case SqlBinaryExpression(var type, var left, "|", var right)    : return new SqlFunction(type, "bitOr",  false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, left, right);
				case SqlBinaryExpression(var type, var left, "&", var right)    : return new SqlFunction(type, "bitAnd", false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, left, right);
				case SqlBinaryExpression(var type, var left, "^", var right)    : return new SqlFunction(type, "bitXor", false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, left, right);
				case SqlBinaryExpression(var type, SqlValue(-1), "*", var right): return new SqlFunction(type, "negate", false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, right      );

				case SqlBinaryExpression(var type, var ex1, "+", var ex2) when type == typeof(string):
				{
					return ConvertFunc(new(type, "concat", false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, ex1, ex2));

					static SqlFunction ConvertFunc(SqlFunction func)
					{
						for (var i = 0; i < func.Parameters.Length; i++)
						{
							switch (func.Parameters[i])
							{
								case SqlBinaryExpression(var t, var e1, "+", var e2) when t == typeof(string):
								{
									var ps = new List<ISqlExpression>(func.Parameters);

									ps.RemoveAt(i);
									ps.Insert(i, e1);
									ps.Insert(i + 1, e2);

									return ConvertFunc(new(t, func.Name, false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, ps.ToArray()));
								}

								case SqlFunction(var t, "concat") f when t == typeof(string):
								{
									var ps = new List<ISqlExpression>(func.Parameters);

									ps.RemoveAt(i);
									ps.InsertRange(i, f.Parameters);

									return ConvertFunc(new(t, func.Name, false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, ps.ToArray()));
								}
							}
						}

						return func;
					}
				}

				case SqlFunction(_, "CASE", [_, SqlValue(true), SqlValue(false)]) f when SqlProviderFlags.IsProjectionBoolSupported is false:
					return new SqlFunction(f.SystemType, f.Name, f.Parameters[0], new SqlValue((byte)1), new SqlValue((byte)0));

				default: return expression;
			}
		}

		protected override ISqlExpression ConvertFunction(NullabilityContext nullability, SqlFunction func)
		{
			switch (func.Name)
			{
				case "Max":
				case "Min":
				case "Avg":
				case "Sum":
				{
					// use standard-compatible aggregates
					// https://github.com/ClickHouse/ClickHouse/pull/16123
					if (func.IsAggregate && ProviderOptions.UseStandardCompatibleAggregates)
					{
						return new SqlFunction(func.SystemType, func.Name.ToLowerInvariant() + "OrNull", true, func.IsPure, func.Precedence, ParametersNullabilityType.Nullable, null, func.Parameters)
						{
							DoNotOptimize = func.DoNotOptimize,
							CanBeNull     = true
						};
					}

					break;
				}
				case PseudoFunctions.TO_LOWER              : return func.WithName("lowerUTF8");
				case PseudoFunctions.TO_UPPER              : return func.WithName("upperUTF8");

				case PseudoFunctions.CONVERT               : // toType
				case PseudoFunctions.TRY_CONVERT           : // toTypeOrNull
				case PseudoFunctions.TRY_CONVERT_OR_DEFAULT: // coalesce(toTypeOrNull, defaultValue)
				{
					var toType       = (SqlDataType)func.Parameters[0];
					var value        = func.Parameters[2];
					var defaultValue = func.Name == PseudoFunctions.TRY_CONVERT_OR_DEFAULT ? func.Parameters[3] : null;
					var suffix       = func.Name != PseudoFunctions.CONVERT ? "OrNull" : null;

					if (ClickHouseConvertFunctions.TryGetValue(toType.Type.DataType, out var name))
					{
						switch (toType.Type.DataType)
						{
							// special cases: String(N)
							case DataType.VarChar :
							case DataType.NVarChar:
							{
								// skip Try[OrDefault] as toString always succeed

								// if converting from FixedString - just trim trailing \0s
								var valueType = value.GetExpressionType();
								if (valueType.DataType is DataType.Char or DataType.NChar or DataType.Binary)
								{
									return new SqlFunction(func.SystemType, "trim", false, true,
										new SqlExpression(func.SystemType, "TRAILING '\x00' FROM {0}", Precedence.Primary, SqlFlags.None, ParametersNullabilityType.IfAnyParameterNullable, null, value));
								}

								return new SqlFunction(func.SystemType, name, false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, value);
							}

							case DataType.Decimal32:
							case DataType.Decimal64:
							case DataType.Decimal128:
							case DataType.Decimal256:
							{
								// toDecimalX(S)
								ISqlExpression newFunc = new SqlFunction(func.SystemType, name + suffix, false, true, 
										Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null,
										value,
										new SqlValue((byte)(toType.Type.Scale ?? ClickHouseMappingSchema.DEFAULT_DECIMAL_SCALE)));

								if (defaultValue != null)
									newFunc = ConvertFunction(nullability, PseudoFunctions.MakeCoalesce(func.SystemType, newFunc, defaultValue));

								return newFunc;
							}

							case DataType.DateTime64:
							{
								// toDateTime64(S)

								ISqlExpression newFunc = new SqlFunction(func.SystemType, name + suffix, false, true, 
										Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null,
										value,
										new SqlValue((byte)(toType.Type.Precision ?? ClickHouseMappingSchema.DEFAULT_DATETIME64_PRECISION)));

								if (defaultValue != null)
									newFunc = ConvertFunction(nullability, PseudoFunctions.MakeCoalesce(func.SystemType, newFunc, defaultValue));

								return newFunc;
							}

							// default call template
							default:
							{
								ISqlExpression newFunc = new SqlFunction(func.SystemType, name + suffix, false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, value);

								if (defaultValue != null)
									newFunc = ConvertFunction(nullability, PseudoFunctions.MakeCoalesce(func.SystemType, newFunc, defaultValue));

								return newFunc;
							}
						}
					}

					throw new LinqToDBException($"Missing conversion function definition to type '{toType.Type}'");

				}
			}

			return base.ConvertFunction(nullability, func);
		}

		// ClickHouse provides several ways to specify type:
		// - to<TYPE_NAME> functions
		// - ::TYPE pgsql-like type hint
		// - CAST function
		//
		// we use functions in most of places
		// [target type, type conversion function name]
		private static readonly IReadOnlyDictionary<DataType, string> ClickHouseConvertFunctions = new Dictionary<DataType, string>()
		{
			{ DataType.Byte      , "toUInt8"      },
			{ DataType.SByte     , "toInt8"       },
			{ DataType.UInt16    , "toUInt16"     },
			{ DataType.Int16     , "toInt16"      },
			{ DataType.UInt32    , "toUInt32"     },
			{ DataType.Int32     , "toInt32"      },
			{ DataType.UInt64    , "toUInt64"     },
			{ DataType.Int64     , "toInt64"      },
			{ DataType.UInt128   , "toUInt128"    },
			{ DataType.Int128    , "toInt128"     },
			{ DataType.UInt256   , "toUInt256"    },
			{ DataType.Int256    , "toInt256"     },

			{ DataType.Single    , "toFloat32"    },
			{ DataType.Double    , "toFloat64"    },

			{ DataType.Boolean   , "toBool"       },

			{ DataType.Guid      , "toUUID"       },

			{ DataType.Date      , "toDate"       },
			{ DataType.Date32    , "toDate32"     },

			{ DataType.DateTime  , "toDateTime"   },
			{ DataType.DateTime64, "toDateTime64" },

			{ DataType.Decimal32 , "toDecimal32"  },
			{ DataType.Decimal64 , "toDecimal64"  },
			{ DataType.Decimal128, "toDecimal128" },
			{ DataType.Decimal256, "toDecimal256" },

			{ DataType.VarChar   , "toString"     },
			{ DataType.NVarChar  , "toString"     },
			{ DataType.VarBinary , "toString"     },

			{ DataType.Json      , "toJSONString" },

			{ DataType.IPv4      , "toIPv4"       },
			{ DataType.IPv6      , "toIPv6"       },
		};

		#endregion
	}
}
