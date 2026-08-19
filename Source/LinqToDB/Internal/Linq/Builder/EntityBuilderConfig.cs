using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Parsed state of a single entity-builder configure-chain — the body of an
	/// <c>Expression&lt;Func&lt;I*Builder&lt;T&gt;, I*Builder&lt;T&gt;&gt;&gt;</c> for any of:
	/// standalone Insert (<see cref="IEntityInsertSpec{TTarget}"/>),
	/// standalone Update (<see cref="IEntityUpdateSpec{TTarget}"/>),
	/// Upsert INSERT branch (<see cref="IUpsertInsertSpec{TTarget}"/>),
	/// Upsert UPDATE branch (<see cref="IUpsertUpdateSpec{TTarget}"/>).
	/// </summary>
	/// <remarks>
	/// <see cref="When"/> and <see cref="DoNothing"/> are populated only by Upsert-branch chains —
	/// the standalone <see cref="IEntityInsertSpec{TTarget}"/> / <see cref="IEntityUpdateSpec{TTarget}"/>
	/// don't expose those methods, so the parser leaves them at their defaults.
	/// </remarks>
	sealed class EntityBuilderConfig(ParameterExpression entityParameter)
	{
		public List<(Expression Field, LambdaExpression Value)> Set    { get; } = new();
		public List<Expression>                                 Ignore { get; } = new();

		/// <summary>Set by <c>.Insert(i =&gt; i.When(...))</c> or <c>.Update(v =&gt; v.When(...))</c> in the Upsert context.</summary>
		public LambdaExpression? When { get; set; }

		/// <summary>Set by <c>.Insert(i =&gt; i.DoNothing())</c> or <c>.Update(v =&gt; v.DoNothing())</c> in the Upsert context.</summary>
		public bool DoNothing { get; set; }

		/// <summary>Shared canonicalisation parameter — every field selector is rewritten to use this so structural equality holds.</summary>
		public ParameterExpression EntityParameter { get; } = entityParameter;
	}
}
