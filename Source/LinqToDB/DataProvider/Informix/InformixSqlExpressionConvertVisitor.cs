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

		protected override bool SupportsNullInColumn              => false;
		protected override bool SupportsDistinctAsExistsIntersect => true;

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
			if (element.SystemType == null)
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

					case TypeCode.Boolean:
						// boolean literal already has explicit cast
						if (argument is SqlValue { Value: bool, ValueType.DataType: DataType.Boolean })
							return argument;
						break;

					default:
						if (cast.SystemType.ToUnderlying() == typeof(DateTimeOffset))
							goto case TypeCode.DateTime;
						break;
				}
			}

			return base.ConvertConversion(cast);
		}

		protected override ISqlExpression ConvertSqlCaseExpression(SqlCaseExpression element)
		{
			if (element.ElseExpression != null)
			{
				var elseExpression = WrapBooleanExpression(element.ElseExpression, includeFields : true, forceConvert: true);

				if (!ReferenceEquals(elseExpression, element.ElseExpression))
				{
					return new SqlCaseExpression(element.Type, element.Cases, elseExpression);
				}
			}

			return element;
		}

		protected override SqlCaseExpression.CaseItem ConvertCaseItem(SqlCaseExpression.CaseItem newElement)
		{
			var resultExpr = WrapBooleanExpression(newElement.ResultExpression, includeFields : true, forceConvert: true);

			if (!ReferenceEquals(resultExpr, newElement.ResultExpression))
			{
				newElement = new SqlCaseExpression.CaseItem(newElement.Condition, resultExpr);
			}

			return newElement;
		}

		protected override ISqlExpression ConvertSqlCondition(SqlConditionExpression element)
		{
			var trueValue  = WrapBooleanExpression(element.TrueValue, includeFields : true, forceConvert: true);
			var falseValue = WrapBooleanExpression(element.FalseValue, includeFields : true, forceConvert: true);

			if (!ReferenceEquals(trueValue, element.TrueValue) || !ReferenceEquals(falseValue, element.FalseValue))
			{
				return new SqlConditionExpression(element.Condition, trueValue, falseValue);
			}

			return element;
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			var columnExpression = base.WrapColumnExpression(expr);

			if (columnExpression.SystemType == typeof(bool))
			{
				var unwrapped = QueryHelper.UnwrapNullablity(columnExpression);

				if (unwrapped is not SqlFunction and not SqlValue and not SqlCastExpression
					&& !QueryHelper.IsBoolean(columnExpression, includeFields: true))
				{
					columnExpression = new SqlCastExpression(columnExpression, new DbDataType(columnExpression.SystemType!, DataType.Boolean), null, isMandatory: true);
				}
			}

			return columnExpression;
		}

		protected override IQueryElement ConvertIsDistinctPredicateAsIntersect(SqlPredicate.IsDistinct predicate)
		{
			return InformixSqlOptimizer.WrapParameters(base.ConvertIsDistinctPredicateAsIntersect(predicate), EvaluationContext);
		}

		protected override IQueryElement VisitSqlSetExpression(SqlSetExpression element)
		{
			var newElement = (SqlSetExpression)base.VisitSqlSetExpression(element);

			// IFX expression cannot be predicate
			var wrapped = newElement.Expression == null ? null : WrapBooleanExpression(newElement.Expression, includeFields : false, withNull: newElement.Column.CanBeNullable(NullabilityContext), forceConvert: true);

			if (!ReferenceEquals(wrapped, newElement.Expression))
			{
				if (wrapped != null)
					wrapped = (ISqlExpression)Optimize(wrapped);
				if (GetVisitMode(newElement) == VisitMode.Modify)
				{
					newElement.Expression = wrapped;
				}
				else
				{
					newElement = new SqlSetExpression(newElement.Column, wrapped);
				}
			}

			return newElement;
		}
	}
}
