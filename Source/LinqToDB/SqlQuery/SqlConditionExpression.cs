﻿using System;

namespace LinqToDB.SqlQuery
{
	public class SqlConditionExpression : SqlExpressionBase
	{
		public SqlConditionExpression(ISqlPredicate condition, ISqlExpression trueValue, ISqlExpression falseValue)
		{
			Condition  = condition;
			TrueValue  = trueValue;
			FalseValue = falseValue;
		}

		public ISqlPredicate  Condition  { get; private set; }
		public ISqlExpression TrueValue  { get; private set; }
		public ISqlExpression FalseValue { get; private set; }

		public override int                    Precedence  => SqlQuery.Precedence.Primary;
		public override Type?                  SystemType  => TrueValue.SystemType;
		public override QueryElementType       ElementType => QueryElementType.SqlCondition;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("$IIF$(")
				.AppendElement(Condition)
				.Append(", ")
				.AppendElement(TrueValue)
				.Append(", ")
				.AppendElement(FalseValue)
				.Append(')');

			return writer;
		}

		public override bool Equals(ISqlExpression  other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			if (!(other is SqlConditionExpression otherCondition))
				return false;

			return this.Condition.Equals(otherCondition.Condition, comparer) &&
			       this.TrueValue.Equals(otherCondition.TrueValue, comparer) &&
			       this.FalseValue.Equals(otherCondition.FalseValue, comparer);
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return TrueValue.CanBeNullable(nullability) || FalseValue.CanBeNullable(nullability);
		}

		public void Modify(ISqlPredicate predicate, ISqlExpression trueValue, ISqlExpression falseValue)
		{
			Condition  = predicate;
			TrueValue  = trueValue;
			FalseValue = falseValue;
		}
	}
}
