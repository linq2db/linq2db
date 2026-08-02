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
	///     <para>
	///         Use this on syntactic-sugar overloads that normalise themselves into a richer internal
	///         call (for example <c>AsCte(Action&lt;ICteBuilder&gt;)</c> which desugars to
	///         <c>AsCteInternal(CteAnnotationsContainer)</c>). Without this marker, the exposer only
	///         evaluates calls whose first argument is a <see cref="System.Linq.Expressions.MemberExpression"/>
	///         or <see cref="System.Linq.Expressions.ConstantExpression"/>; lambda-captured calls with
	///         a method-chain source would never get the client-side rewrite.
	///     </para>
	///     <para>
	///         A method marked with this attribute may be invoked more than once per <see cref="System.Linq.IQueryable"/>
	///         materialization — expression tree rewrites (other visitors, re-exposure after transformation) can cause
	///         the exposer to run again. Marked methods and any delegates they invoke (builder callbacks, etc.) must
	///         therefore be pure and idempotent: no observable side effects beyond constructing the replacement
	///         expression tree.
	///     </para>
	///     <para>
	///         The attribute is observed only by the in-box <see cref="Builder.Visitors.ExposeExpressionVisitor"/>.
	///         Custom <see cref="System.Linq.IQueryProvider"/> implementations that do not route through the linq2db
	///         exposer will not desugar methods tagged with this attribute.
	///     </para>
	///     <para>
	///         The exposer throws <see cref="LinqToDBException"/> when a marked method appears with arguments that
	///         cannot be client-evaluated (captured non-compilable values). Silently dropping such calls would lose
	///         the user's configuration, so the failure is surfaced explicitly.
	///     </para>
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class EagerEvaluationAttribute : Attribute
	{
	}
}
