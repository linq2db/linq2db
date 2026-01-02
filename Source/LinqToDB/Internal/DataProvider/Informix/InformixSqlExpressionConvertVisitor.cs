using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Informix
{
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
				case "%": return new SqlFunction(element.Type, "Mod", element.Expr1, element.Expr2);
				case "&": return new SqlFunction(element.Type, "BitAnd", element.Expr1, element.Expr2);
				case "|": return new SqlFunction(element.Type, "BitOr", element.Expr1, element.Expr2);
				case "^": return new SqlFunction(element.Type, "BitXor", element.Expr1, element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		protected override SqlCoalesceExpression? WrapBooleanCoalesceItems(SqlCoalesceExpression element, IQueryElement newElement, bool forceConvert)
		{
			return base.WrapBooleanCoalesceItems(element, newElement, forceConvert: true);
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
			var toType       = cast.ToType;
			var argument     = cast.Expression;
			var argumentType = QueryHelper.GetDbDataType(argument, MappingSchema);

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
							return new SqlFunction(cast.Type, "To_Char", argument, new SqlValue("%Y-%m-%d %H:%M:%S.%F"));
						}
#if SUPPORTS_DATEONLY
						if (stype == typeof(DateOnly))
						{
							return new SqlFunction(cast.Type, "To_Char", argument, new SqlValue("%Y-%m-%d"));
						}
#endif
						if (stype.IsNumberType)
						{
							return new SqlFunction(cast.Type, "To_Char", argument);
						}

						break;
					}

					case TypeCode.UInt64   :
						if (argument.SystemType!.IsFloatType)
							argument = new SqlFunction(cast.Type, "Floor", argument);
						break;

					case TypeCode.DateTime :
						if (argument.ElementType == QueryElementType.SqlParameter && argumentType.Equals(toType))
							break;

						if (IsDateDataType(toType, "Date"))
						{
							if (argument.SystemType == typeof(string))
							{
								return new SqlFunction(
									cast.Type,
									"Date",
									new SqlFunction(cast.Type, "To_Date", argument, new SqlValue("%Y-%m-%d")));
							}

							return new SqlFunction(cast.Type, "Date", argument);
						}

						if ((IsDateTime2Type(cast.ToType, "DateTime2")
								|| IsDateTimeType(cast.ToType, "DateTime")
								|| IsSmallDateTimeType(cast.ToType, "SmallDateTime"))
							&& argument.SystemType == typeof(string))
						{
							return new SqlFunction(cast.Type, "To_Date", argument, new SqlValue("%Y-%m-%d %H:%M:%S"));
						}

						if (IsTimeDataType(cast.ToType))
						{
							return new SqlCastExpression(new SqlExpression(QueryHelper.GetDbDataType(cast.Expression, MappingSchema), "Extend({0}, Hour to Second)", Precedence.Primary, argument), new DbDataType(typeof(string), DataType.Char, null, 8), null, true);
						}

						return new SqlFunction(cast.Type, "To_Date", argument);

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
			var trueValue  = WrapBooleanExpression(element.TrueValue, includeFields : false, forceConvert: true);
			var falseValue = WrapBooleanExpression(element.FalseValue, includeFields : false, forceConvert: true);

			if (!ReferenceEquals(trueValue, element.TrueValue) || !ReferenceEquals(falseValue, element.FalseValue))
			{
				return new SqlConditionExpression(element.Condition, trueValue, falseValue);
			}

			return element;
		}

		protected internal override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			var element = base.VisitInListPredicate(predicate);

			if (element is SqlPredicate.InList { Expr1: SqlValue { Value: null } value } p)
			{
				// IFX doesn't support
				// NULL [NOT] IN (...)
				// but support typed NULL or parameter
				// for non-query parameter same code exists in SqlBuilder
				var nullCast = new SqlCastExpression(value, value.ValueType, null, isMandatory: true);
				element      = new SqlPredicate.InList(nullCast, p.WithNull, p.IsNot, p.Values);
			}

			return element;
		}

		protected internal override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			var element = base.VisitInSubQueryPredicate(predicate);

			if (element is SqlPredicate.InSubQuery { Expr1: SqlValue { Value: null } value } p)
			{
				// IFX doesn't support
				// NULL [NOT] IN (...)
				// but support typed NULL or parameter
				// for non-query parameter same code exists in SqlBuilder
				var nullCast = new SqlCastExpression(value, value.ValueType, null, isMandatory: true);
				element      = new SqlPredicate.InSubQuery(nullCast, p.IsNot, p.SubQuery, p.DoNotConvert);
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
			return InformixSqlOptimizer.WrapParameters(base.ConvertIsDistinctPredicateAsIntersect(predicate));
		}

		protected internal override IQueryElement VisitSqlSetExpression(SqlSetExpression element)
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

		protected internal override IQueryElement VisitExprPredicate(SqlPredicate.Expr predicate)
		{
			var newElement = base.VisitExprPredicate(predicate);

			if (newElement is SqlPredicate.Expr { Expr1: SqlParameter { IsQueryParameter: true, NeedsCast: false, Type.DataType: DataType.Boolean } p })
				p.NeedsCast = true;

			return newElement;
	}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case PseudoFunctions.LENGTH:
				{
					/*
					 * CHAR_LENGTH(value + ".") - 1
					 */

					var value     = func.Parameters[0];
					var valueType = Factory.GetDbDataType(value);
					var funcType  = Factory.GetDbDataType(value);

					var valueString = Factory.Add(valueType, value, Factory.Value(valueType, "."));
					var valueLength = Factory.Function(funcType, "CHAR_LENGTH", valueString);

					return Factory.Sub(func.Type, valueLength, Factory.Value(func.Type, 1));
}
			}

			return base.ConvertSqlFunction(func);
		}
	}
}
