using System;

namespace LinqToDB.SqlQuery
{
	public class SqlConditionExpression : SqlExpressionBase
	{
		public SqlConditionExpression(ISqlPredicate predicate, ISqlExpression trueValue, ISqlExpression falseValue)
		{
			Predicate  = predicate;
			TrueValue  = trueValue;
			FalseValue = falseValue;
		}

		public ISqlPredicate  Predicate  { get; private set; }
		public ISqlExpression TrueValue  { get; private set; }
		public ISqlExpression FalseValue { get; private set; }

		public override int                    Precedence  => SqlQuery.Precedence.LogicalDisjunction;
		public override Type?                  SystemType  => TrueValue.SystemType;
		public override QueryElementType       ElementType => QueryElementType.SqlCondition;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("$IIF$(")
				.Append(Predicate)
				.Append(", ")
				.Append(TrueValue)
				.Append(", ")
				.Append(FalseValue)
				.Append(')');

			return writer;
		}

		public override bool Equals(ISqlExpression  other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			throw new NotImplementedException();
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return TrueValue.CanBeNullable(nullability) || FalseValue.CanBeNullable(nullability);
		}

		public void Modify(ISqlPredicate predicate, ISqlExpression trueValue, ISqlExpression falseValue)
		{
			Predicate  = predicate;
			TrueValue  = trueValue;
			FalseValue = falseValue;
		}
	}
}
