using System;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlInlinedBase : SqlExpressionBase
	{
		protected SqlInlinedBase(SqlParameter parameter, ISqlExpression inlinedValue)
		{
			Parameter    = parameter;
			InlinedValue = inlinedValue;
		}

		public abstract ISqlExpression GetSqlExpression(EvaluationContext evaluationContext);

		public SqlParameter   Parameter    { get; private set; }
		public ISqlExpression InlinedValue { get; private set; }

		public override int   Precedence => InlinedValue.Precedence;
		public override Type? SystemType => InlinedValue.SystemType;

		public override bool CanBeNullable(NullabilityContext nullability) => true;

		public void Modify(SqlParameter parameter, ISqlExpression inlinedValue)
		{
			Parameter    = parameter;
			InlinedValue = inlinedValue;
		}
	}
}
