using System;

namespace LinqToDB.Internal.SqlQuery
{
	public interface ISqlPredicate : IQueryElement
	{
		int  Precedence { get; }

		bool          CanInvert(NullabilityContext nullability);
		ISqlPredicate Invert   (NullabilityContext nullability);

		/// <summary>
		/// Returns <c>true</c> if predicate could be evaluated to UNKNOWN.
		/// </summary>
		bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased);

		bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer);
	}
}
