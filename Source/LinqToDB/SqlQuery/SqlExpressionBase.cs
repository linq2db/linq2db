using System;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlExpressionBase : QueryElement, ISqlExpression
	{
		public abstract bool            Equals(ISqlExpression? other);

		public abstract ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func);
		public abstract bool            Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer);
		public abstract bool            CanBeNullable(NullabilityContext nullability);
		public abstract int             Precedence { get; }
		public abstract Type?           SystemType { get; }
	}
}
