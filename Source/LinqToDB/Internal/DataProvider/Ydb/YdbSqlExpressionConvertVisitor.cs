using System;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public YdbSqlExpressionConvertVisitor(bool allowModify) : base(allowModify) { }

		/// <inheritdoc/>
		protected override bool SupportsNullInColumn => false;

		// ------------------------------------------------------------------
		// SearchString → LIKE  (+ регистронезависимый поиск)
		// ------------------------------------------------------------------
		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate);

			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) ?? true;
			if (!caseSensitive && like is SqlPredicate.Like lp)
			{
				like = new SqlPredicate.Like(
					PseudoFunctions.MakeToLower(lp.Expr1, MappingSchema),
					lp.IsNot,
					PseudoFunctions.MakeToLower(lp.Expr2, MappingSchema),
					lp.Escape);
			}

			return like;
		}

		// ------------------------------------------------------------------
		// Бинарные операции
		// ------------------------------------------------------------------
		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				// Сцепление строк YDB — оператор ||
				case "+" when element.SystemType == typeof(string):
					return new SqlBinaryExpression(
						element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence);

				// Остаток от деления: приводим к decimal, если требуется
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

		// ------------------------------------------------------------------
		// Функции и псевдо-функции
		// ------------------------------------------------------------------
		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				//----------------------------------------------------------------
				// Регистронезависимые функции
				case PseudoFunctions.TO_LOWER:
					return func.WithName("Unicode::ToLower");

				case PseudoFunctions.TO_UPPER:
					return func.WithName("Unicode::ToUpper");

				//----------------------------------------------------------------
				// Безопасные преобразования типов
				case PseudoFunctions.TRY_CONVERT:
					// CAST(x AS <type>?) → null при ошибке
					return new SqlExpression(
						func.Type,
						"CAST({0} AS {1}?)",
						Precedence.Primary,
						func.Parameters[2],      // значение
						func.Parameters[0]);     // целевой тип

				case PseudoFunctions.TRY_CONVERT_OR_DEFAULT:
					// COALESCE(CAST(x AS <type>?), default)
					return new SqlExpression(
							func.Type,
							"COALESCE(CAST({0} AS {1}?), {2})",
							Precedence.Primary,
							func.Parameters[2],    // значение
							func.Parameters[0],    // целевой тип
							func.Parameters[3])    // default
					{
						CanBeNull =
								func.Parameters[2].CanBeNullable(NullabilityContext) ||
								func.Parameters[3].CanBeNullable(NullabilityContext)
					};

				//----------------------------------------------------------------
				// CharIndex (аналога POSITION в YDB нет; используем FIND)
				case "CharIndex":
					switch (func.Parameters.Length)
					{
						// CharIndex(substr, str)
						case 2:
							return new SqlExpression(
								func.Type,
								"COALESCE(FIND({1}, {0}) + 1, 0)",
								Precedence.Primary,
								func.Parameters[0],    // substring
								func.Parameters[1]);   // source

						// CharIndex(substr, str, start)
						case 3:
							return new SqlExpression(
								func.Type,
								"COALESCE(FIND(SUBSTRING({1}, {2} - 1), {0}) + {2}, 0)",
								Precedence.Primary,
								func.Parameters[0],    // substring
								func.Parameters[1],    // source
								func.Parameters[2]);   // start
					}

					break;
			}

			return base.ConvertSqlFunction(func);
		}

		// ------------------------------------------------------------------
		// CAST / преобразование bool → CASE
		// ------------------------------------------------------------------
		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (cast.SystemType.ToUnderlying() == typeof(bool) &&
			    !(cast.IsMandatory && cast.Expression.SystemType?.ToNullableUnderlying() == typeof(bool)) &&
			    cast.Expression is not SqlSearchCondition and not SqlCaseExpression)
			{
				// YDB не поддерживает CAST(condition AS bool),
				// поэтому превращаем в CASE WHEN ... THEN TRUE ELSE FALSE END
				return ConvertBooleanToCase(cast.Expression, cast.ToType);
			}

			cast = FloorBeforeConvert(cast);
			return base.ConvertConversion(cast);
		}
	}
}
