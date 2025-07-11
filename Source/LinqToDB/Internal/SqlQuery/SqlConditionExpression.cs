using System;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlConditionExpression : SqlExpressionBase
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

		public override int                    Precedence  => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type?                  SystemType  => TrueValue.SystemType;
		public override QueryElementType       ElementType => QueryElementType.SqlCondition;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				//.DebugAppendUniqueId(this)
				.Append("$IIF$(")
				.AppendElement(Condition)
				.Append(", ")
				.AppendElement(TrueValue)
				.Append(", ")
				.AppendElement(FalseValue)
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				Condition.GetElementHashCode(),
				TrueValue.GetElementHashCode(),
				FalseValue.GetElementHashCode()
			);
		}

		public override bool Equals(ISqlExpression  other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			if (!(other is SqlConditionExpression otherCondition))
				return false;

			return Condition.Equals(otherCondition.Condition, comparer) &&
			       TrueValue.Equals(otherCondition.TrueValue, comparer) &&
			       FalseValue.Equals(otherCondition.FalseValue, comparer);
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			if (Condition is SqlPredicate.IsNull isNullPredicate)
			{
				var unwrapped = QueryHelper.UnwrapNullablity(isNullPredicate.Expr1);

				if (isNullPredicate.IsNot)
				{
					if (unwrapped.Equals(TrueValue, SqlExtensions.DefaultComparer) && !FalseValue.CanBeNullable(nullability))
					{
						return false;
					}
				}
				else if (unwrapped.Equals(FalseValue, SqlExtensions.DefaultComparer) && !TrueValue.CanBeNullable(nullability))
				{
					return false;
				}
			}

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
