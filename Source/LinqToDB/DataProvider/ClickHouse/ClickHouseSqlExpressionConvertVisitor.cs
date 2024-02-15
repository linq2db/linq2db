using System.Collections.Generic;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Common;
	using SqlProvider;
	using SqlQuery;

	public class ClickHouseSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		readonly ClickHouseOptions _providerOptions;

		public ClickHouseSqlExpressionConvertVisitor(bool allowModify, ClickHouseOptions providerOptions) : base(allowModify)
		{
			_providerOptions = providerOptions;
		}

		#region LIKE

		// https://clickhouse.com/docs/en/sql-reference/ansi/#feature-status E061-05
		// https://clickhouse.com/docs/en/sql-reference/operators/#like-function

		public override bool     LikeIsEscapeSupported  => false;
		public override string   LikeEscapeCharacter    => "\\";
		public override string[] LikeCharactersToEscape => ClickHouseLikeCharactersToEscape;

		private static readonly string[] ClickHouseLikeCharactersToEscape = { "%", "_" };

		public override ISqlPredicate ConvertLikePredicate(SqlPredicate.Like predicate)
		{
			// remove ESCAPE clause
			if (predicate.Escape != null)
				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, predicate.Expr2, null);

			return base.ConvertLikePredicate(predicate);
		}

		#endregion

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext)
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
			{
				return subStrPredicate.MakeNot(predicate.IsNot);
			}

			return base.ConvertSearchStringPredicate(predicate);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			var newElement = base.ConvertSqlBinaryExpression(element);

			if (newElement is not SqlBinaryExpression binaryExpression)
				return Visit(newElement);

			switch (binaryExpression)
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
						left = ConvertSqlFunction(PseudoFunctions.MakeConvert(
							new SqlDataType(new DbDataType(typeof(double), DataType.Double)),
							new SqlDataType(leftType),
							left));
						rewrite = true;
					}

					if (rightType.DataType is DataType.Decimal32 or DataType.Decimal64 or DataType.Decimal128 or DataType.Decimal256)
					{
						right = ConvertSqlFunction(PseudoFunctions.MakeConvert(
							new SqlDataType(new DbDataType(typeof(double), DataType.Double)),
							new SqlDataType(rightType),
							right));
						rewrite = true;
					}

					return !rewrite
						? element
						: ConvertSqlFunction(PseudoFunctions.MakeConvert(
							new SqlDataType(element.GetExpressionType()),
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

				default: return element;
			}
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

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			if (SqlProviderFlags != null)
			{
				switch (func)
				{
					case SqlFunction(_, "CASE", [_, SqlValue(true), SqlValue(false)]) f when SqlProviderFlags.IsProjectionBoolSupported is false:
						return new SqlFunction(f.SystemType, f.Name, f.Parameters[0], new SqlValue((byte)1), new SqlValue((byte)0));
				}
			}

			switch (func.Name)
			{
				case "Max":
				case "Min":
				case "Avg":
				case "Sum":
				{
					// use standard-compatible aggregates
					// https://github.com/ClickHouse/ClickHouse/pull/16123
					if (func.IsAggregate && _providerOptions.UseStandardCompatibleAggregates)
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
					var toTypeExpr   = func.Parameters[0];

					DbDataType toType;
					if (toTypeExpr is SqlDataType sqlDataType)
					{
						toType = sqlDataType.Type;
					}
					else if (toTypeExpr.SystemType == null)
					{
						throw new LinqToDBException($"Missing conversion function definition to type '{toTypeExpr}'");
					}
					else
					{
						sqlDataType = MappingSchema.GetDataType(toTypeExpr.SystemType);
						toType      = sqlDataType.Type;
					}

					var value        = func.Parameters[2];
					var defaultValue = func.Name == PseudoFunctions.TRY_CONVERT_OR_DEFAULT ? func.Parameters[3] : null;
					var suffix       = func.Name != PseudoFunctions.CONVERT ? "OrNull" : null;

					if (ClickHouseConvertFunctions.TryGetValue(toType.DataType, out var name))
					{
						switch (toType.DataType)
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
										new SqlValue((byte)(toType.Scale ?? ClickHouseMappingSchema.DEFAULT_DECIMAL_SCALE)));

								if (defaultValue != null)
									newFunc = ConvertSqlFunction(PseudoFunctions.MakeCoalesce(func.SystemType, newFunc, defaultValue));

								return newFunc;
							}

							case DataType.DateTime64:
							{
								// toDateTime64(S)

								ISqlExpression newFunc = new SqlFunction(func.SystemType, name + suffix, false, true,
										Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null,
										value,
										new SqlValue((byte)(toType.Precision ?? ClickHouseMappingSchema.DEFAULT_DATETIME64_PRECISION)));

								if (defaultValue != null)
									newFunc = ConvertSqlFunction(PseudoFunctions.MakeCoalesce(func.SystemType, newFunc, defaultValue));

								return newFunc;
							}

							// default call template
							default:
							{
								ISqlExpression newFunc = new SqlFunction(func.SystemType, name + suffix, false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, value);

								if (defaultValue != null)
									newFunc = ConvertSqlFunction(PseudoFunctions.MakeCoalesce(func.SystemType, newFunc, defaultValue));

								return newFunc;
							}
						}
					}

					throw new LinqToDBException($"Missing conversion function definition to type '{toTypeExpr.SystemType}'");
				}
			}

			return base.ConvertSqlFunction(func);
		}
	}
}
