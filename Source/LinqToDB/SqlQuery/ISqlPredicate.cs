using System;

namespace LinqToDB.SqlQuery
{
	public interface ISqlPredicate : IQueryElement
	{
		int  Precedence { get; }

		bool          CanInvert(NullabilityContext nullability);
		ISqlPredicate Invert(NullabilityContext    nullability);

		bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer);
	}
}
