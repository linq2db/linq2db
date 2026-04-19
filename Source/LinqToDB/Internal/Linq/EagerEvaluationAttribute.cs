using System;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	///     Marks an <see cref="System.Linq.IQueryable"/>-returning extension method whose call
	///     should be evaluated on the client during expression tree exposure, regardless of the
	///     shape of its source argument. The returned queryable's expression replaces the original
	///     method call before query translation.
	/// </summary>
	/// <remarks>
	///     Use this on syntactic-sugar overloads that normalise themselves into a richer internal
	///     call (for example <c>AsCte(Action&lt;ICteBuilder&gt;)</c> which desugars to
	///     <c>AsCteInternal(CteAnnotationsContainer)</c>). Without this marker, the exposer only
	///     evaluates calls whose first argument is a <see cref="System.Linq.Expressions.MemberExpression"/>
	///     or <see cref="System.Linq.Expressions.ConstantExpression"/>; lambda-captured calls with
	///     a method-chain source would never get the client-side rewrite.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class EagerEvaluationAttribute : Attribute
	{
	}
}
