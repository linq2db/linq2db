using System;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class InformixSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public InformixSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullInColumn => false;

		public override ISqlPredicate ConvertLikePredicate(SqlPredicate.Like predicate)
		{
			//Informix cannot process parameter in Like template (only Informix provider, not InformixDB2)
			//
			if (EvaluationContext.ParameterValues != null)
			{
				var exp2 = TryConvertToValue(predicate.Expr2, EvaluationContext);

				if (!ReferenceEquals(exp2, predicate.Expr2))
				{
					predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, exp2, predicate.Escape);
				}
			}

			return predicate;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%": return new SqlFunction(element.SystemType, "Mod", element.Expr1, element.Expr2);
				case "&": return new SqlFunction(element.SystemType, "BitAnd", element.Expr1, element.Expr2);
				case "|": return new SqlFunction(element.SystemType, "BitOr", element.Expr1, element.Expr2);
				case "^": return new SqlFunction(element.SystemType, "BitXor", element.Expr1, element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertCoalesce(SqlCoalesceExpression element)
		{
			if (SqlProviderFlags == null || element.SystemType == null)
				return element;

			return ConvertCoalesceToBinaryFunc(element, "Nvl", supportsParameters : false);
		}

		//TODO: Move everything to SQLBuilder
		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var toType   = cast.ToType;
			var argument = cast.Expression;

			var isNull = argument is SqlValue sqlValue && sqlValue.Value == null;

			if (!isNull)
			{
				switch (Type.GetTypeCode(cast.SystemType.ToUnderlying()))
				{
					case TypeCode.String   :
					{
						var stype = argument.SystemType!.ToUnderlying();
						if (stype == typeof(DateTime))
						{
							return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("%Y-%m-%d %H:%M:%S.%F"));
						}
#if NET6_0_OR_GREATER
						if (stype == typeof(DateOnly))
						{
							return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("%Y-%m-%d"));
						}
#endif
						if (stype.IsNumeric())
						{
							return new SqlFunction(cast.SystemType, "To_Char", argument);
						}

						break;
					}

					case TypeCode.UInt64   :
						if (argument.SystemType!.IsFloatType())
							argument = new SqlFunction(cast.SystemType, "Floor", argument);
						break;

					case TypeCode.DateTime :
						if (IsDateDataType(toType, "Date"))
						{
							if (argument.SystemType == typeof(string))
							{
								return new SqlFunction(
									cast.SystemType,
									"Date",
									new SqlFunction(cast.SystemType, "To_Date", argument, new SqlValue("%Y-%m-%d")));
							}

							return new SqlFunction(cast.SystemType, "Date", argument);
						}

						if ((IsDateTime2Type(cast.ToType, "DateTime2")
								|| IsDateTimeType(cast.ToType, "DateTime")
								|| IsSmallDateTimeType(cast.ToType, "SmallDateTime"))
							&& argument.SystemType == typeof(string))
							return new SqlFunction(cast.SystemType, "To_Date", argument, new SqlValue("%Y-%m-%d %H:%M:%S"));

						if (IsTimeDataType(cast.ToType))
						{
							return new SqlCastExpression(new SqlExpression(cast.Expression.SystemType, "Extend({0}, Hour to Second)", Precedence.Primary, argument), new DbDataType(typeof(string), DataType.Char, null, 8), null, true);
						}

						return new SqlFunction(cast.SystemType, "To_Date", argument);

					default:
						if (cast.SystemType.ToUnderlying() == typeof(DateTimeOffset))
							goto case TypeCode.DateTime;
						break;
				}
			}

			return base.ConvertConversion(cast);
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			var columnExpression = base.WrapColumnExpression(expr);

			if (SqlProviderFlags != null
			    && columnExpression.SystemType == typeof(bool)
			    && QueryHelper.UnwrapNullablity(columnExpression) is not (SqlCastExpression or SqlColumn or SqlField))
			{
				columnExpression = new SqlCastExpression(columnExpression, new DbDataType(columnExpression.SystemType!, DataType.Boolean), null, isMandatory: true);
			}

			return columnExpression;
		}

		protected override IQueryElement VisitSqlConditionExpression(SqlConditionExpression element)
		{
			var expression = base.VisitSqlConditionExpression(element);

			if (expression is SqlConditionExpression condition && condition.SystemType?.UnwrapNullableType() == typeof(bool))
			{
				if (condition.TrueValue is SqlValue && QueryHelper.UnwrapNullablity(condition.FalseValue) is not SqlValue)
				{
					expression = new SqlConditionExpression(condition.Condition, new SqlCastExpression(condition.TrueValue, new DbDataType(typeof(bool), DataType.Boolean), null, isMandatory: true), condition.FalseValue);
				}
				else if (condition.FalseValue is SqlValue && QueryHelper.UnwrapNullablity(condition.TrueValue) is not SqlValue)
				{
					expression = new SqlConditionExpression(condition.Condition, condition.TrueValue, new SqlCastExpression(condition.FalseValue, new DbDataType(typeof(bool), DataType.Boolean), null, isMandatory: true));
				}
			}

			return expression;
		}
	}
}
