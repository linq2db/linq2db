using System;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ydb
{
	public sealed class YdbSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public YdbSqlExpressionConvertVisitor(bool allowModify) : base(allowModify) { }

		protected override bool SupportsNullInColumn => false;

		#region SearchString → LIKE

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate);

			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) ?? true;
			if (!caseSensitive && like is SqlPredicate.Like lp)
			{
				like = new SqlPredicate.Like(
					PseudoFunctions.MakeToLower(lp.Expr1),
					lp.IsNot,
					PseudoFunctions.MakeToLower(lp.Expr2),
					lp.Escape);
			}

			return like;
		}

		#endregion

		#region Бинарные операции

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "+" when element.SystemType == typeof(string):
					return new SqlBinaryExpression(
						element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence);

				case "%":
				{
					var dbType = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);

					if (dbType.SystemType.ToNullableUnderlying() != typeof(decimal))
					{
						var toType   = MappingSchema.GetDbDataType(typeof(decimal));
						var newLeft  = PseudoFunctions.MakeCast(element.Expr1, toType);

						var sysType  = dbType.SystemType?.IsNullable() == true
							? typeof(decimal?)
							: typeof(decimal);

						var newExpr  = PseudoFunctions.MakeMandatoryCast(
							new SqlBinaryExpression(sysType, newLeft, "%", element.Expr2),
							toType);

						return Visit(Optimize(newExpr));
					}

					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		#endregion

		#region Functions

		/// <summary>
		/// Converts pseudo-SQL functions into YDB-compatible expressions.
		/// Handles case-insensitive string operations, safe type conversions, and character indexing.
		/// </summary>
		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				// ---------- Case-insensitive string functions ----------
				case PseudoFunctions.TO_LOWER:
					// Unicode::ToLower(<string>)
					return func.WithName("Unicode::ToLower");

				case PseudoFunctions.TO_UPPER:
					// Unicode::ToUpper(<string>)
					return func.WithName("Unicode::ToUpper");

				// ---------- Safe type conversions ----------
				case PseudoFunctions.TRY_CONVERT:
					// CAST(<value> AS <type>?) → returns null on conversion error
					return new SqlExpression(
							func.SystemType,
							"CAST({0} AS {1}?)",
							Precedence.Primary,
							func.Parameters[2],        // value
							func.Parameters[0]         // target type
						)
					{ CanBeNull = true };

				case PseudoFunctions.TRY_CONVERT_OR_DEFAULT:
					// COALESCE(CAST(<value> AS <type>?), <defaultValue>)
					return new SqlExpression(
							func.SystemType,
							"COALESCE(CAST({0} AS {1}?), {2})",
							Precedence.Primary,
							func.Parameters[2],        // value
							func.Parameters[0],        // target type
							func.Parameters[3]         // default value
						)
					{
						CanBeNull =
								func.Parameters[2].CanBeNullable(NullabilityContext) ||
								func.Parameters[3].CanBeNullable(NullabilityContext)
					};

				// ---------- Substring search ----------
				// CharIndex(substring, source [, startLocation])
				case "CharIndex":
					switch (func.Parameters.Length)
					{
						// Two-argument form: search from beginning
						case 2:
							// COALESCE(FIND(source, substring) + 1, 0)
							return new SqlExpression(
								func.SystemType,
								"COALESCE(FIND({1}, {0}) + 1, 0)",
								Precedence.Primary,
								func.Parameters[0],    // substring
								func.Parameters[1]     // source
							);

						// Three-argument form: search from specified offset (T-SQL is 1-based)
						case 3:
							// COALESCE(FIND(SUBSTRING(source, start-1), substring) + start, 0)
							return new SqlExpression(
								func.SystemType,
								"COALESCE(FIND(SUBSTRING({1}, {2} - 1), {0}) + {2}, 0)",
								Precedence.Primary,
								func.Parameters[0],    // substring
								func.Parameters[1],    // source
								func.Parameters[2]     // startLocation
							);
					}

					break;
			}

			return base.ConvertSqlFunction(func);
		}

		#endregion

		#region CAST / BOOL → CASE

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (cast.SystemType.ToUnderlying() == typeof(bool) &&
				!(cast.IsMandatory && cast.Expression.SystemType?.ToNullableUnderlying() == typeof(bool)) &&
				cast.Expression is not SqlSearchCondition and not SqlCaseExpression)
			{
				return ConvertBooleanToCase(cast.Expression, cast.ToType);
			}

			cast = FloorBeforeConvert(cast);
			return base.ConvertConversion(cast);
		}

		#endregion
	}
}
