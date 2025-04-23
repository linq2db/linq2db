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

		#region Функции

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func)
			{
				// CHARINDEX(substr, str)
				case { Name: "CharIndex", Parameters: [var sub, var str], SystemType: var t }:
					return new SqlExpression(t,
						"Position({0} in {1})",
						Precedence.Primary,
						sub, str);

				// CHARINDEX(substr, str, start)
				case { Name: "CharIndex", Parameters: [var sub, var str, var start], SystemType: var t }:
					return Add<int>(
						new SqlExpression(t,
							"Position({0} in {1})",
							Precedence.Primary,
							sub,
							(ISqlExpression)Visit(
								new SqlFunction(typeof(string), "Substring",
									str,
									start,
									Sub<int>(
										(ISqlExpression)Visit(
											new SqlFunction(typeof(int), "Length", str)),
										start)))),
						Sub(start, 1));

				default:
					return base.ConvertSqlFunction(func);
			}
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
