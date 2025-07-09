using System;

using LinqToDB.Linq.Builder;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlInlinedToSqlExpression : SqlInlinedBase
	{
		public override QueryElementType ElementType => QueryElementType.SqlInlinedToSqlExpression;

		public SqlInlinedToSqlExpression(SqlParameter parameter, ISqlExpression inlinedValue) 
			: base(parameter, inlinedValue)
		{
		}

		public override ISqlExpression GetSqlExpression(EvaluationContext evaluationContext)
		{
			if (evaluationContext.ParameterValues == null)
				return InlinedValue;

			if (evaluationContext.ParameterValues.TryGetValue(Parameter, out var value))
			{
				if (value.ProviderValue is IToSqlConverter converter)
					return converter.ToSql(converter);
			}

			return InlinedValue;
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("@(")
				.DebugAppendUniqueId(this)
				.AppendElement(InlinedValue)
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Parameter.GetElementHashCode());
			hash.Add(InlinedValue.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlInlinedToSqlExpression otherInlined)
				return false;

			return Parameter.Equals(otherInlined.Parameter, comparer);
		}

	}
}
