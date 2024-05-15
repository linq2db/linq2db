﻿using System;

namespace LinqToDB.SqlQuery
{
	public class SqlInlinedSqlExpression : SqlInlinedBase
	{
		public override QueryElementType ElementType  => QueryElementType.SqlInlinedExpression;

		public SqlInlinedSqlExpression(SqlParameter parameter, ISqlExpression inlinedValue) 
			: base(parameter, inlinedValue)
		{
		}

		public override ISqlExpression GetSqlExpression(EvaluationContext evaluationContext)
		{
			if (evaluationContext.ParameterValues == null)
				return InlinedValue;

			if (evaluationContext.ParameterValues.TryGetValue(Parameter, out var value))
			{
				if (value.ProviderValue is ISqlExpression sqlExpression)
					return sqlExpression;
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

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlInlinedSqlExpression otherInlined)
				return false;

			return Parameter.Equals(otherInlined.Parameter, comparer);
		}
	}
}
