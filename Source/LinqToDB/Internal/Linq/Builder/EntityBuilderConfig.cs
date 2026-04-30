using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Parsed state of a single entity-builder configure-chain — the body of an
	/// <c>Expression&lt;Func&lt;I*Builder&lt;T&gt;, I*Builder&lt;T&gt;&gt;&gt;</c> for any of:
	/// standalone Insert (<see cref="IEntityInsertBuilder{TTarget}"/>),
	/// standalone Update (<see cref="IEntityUpdateBuilder{TTarget}"/>),
	/// Upsert INSERT branch (<see cref="IUpsertInsertBuilder{TTarget}"/>),
	/// Upsert UPDATE branch (<see cref="IUpsertUpdateBuilder{TTarget}"/>).
	/// </summary>
	/// <remarks>
	/// <see cref="When"/> and <see cref="DoNothing"/> are populated only by Upsert-branch chains —
	/// the standalone <see cref="IEntityInsertBuilder{TTarget}"/> / <see cref="IEntityUpdateBuilder{TTarget}"/>
	/// don't expose those methods, so the parser leaves the fields at their defaults.
	/// </remarks>
	sealed class EntityBuilderConfig
	{
		public readonly List<(Expression Field, LambdaExpression Value)> Set    = new();
		public readonly List<Expression>                                 Ignore = new();

		/// <summary>Set by <c>.Insert(i =&gt; i.When(...))</c> or <c>.Update(v =&gt; v.When(...))</c> in the Upsert context.</summary>
		public LambdaExpression? When;

		/// <summary>Set by <c>.Insert(i =&gt; i.DoNothing())</c> or <c>.Update(v =&gt; v.DoNothing())</c> in the Upsert context.</summary>
		public bool DoNothing;

		/// <summary>Shared canonicalisation parameter — every field selector is rewritten to use this so structural equality holds.</summary>
		public readonly ParameterExpression EntityParm;

		public EntityBuilderConfig(ParameterExpression entityParm) => EntityParm = entityParm;
	}
}
