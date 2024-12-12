using System;

namespace LinqToDB.SqlQuery
{
	public interface ISqlPredicate : IQueryElement
	{
		int  Precedence { get; }

		/// <summary>
		/// Returns <c>true</c>, if predicate inversion doesn't make it more complex. Usually it means it will not add too many
		/// NOT inversion predicates.
		/// </summary>
		bool          InvertIsSimple();

		/// <summary>
		/// Inverts predicate logic. Must be implemented even if <see cref="InvertIsSimple"/> returns <c>false</c>.
		/// </summary>
		ISqlPredicate Invert();

		/// <summary>
		/// Returns true, if predicate could yield UNKNOWN boolean value.
		/// </summary>
		bool CanBeUnknown(NullabilityContext nullability);

		bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer);
	}
}
