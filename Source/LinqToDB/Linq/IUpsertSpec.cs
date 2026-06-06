using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for an Upsert (insert-or-update) operation.
	/// Returned to the caller of an <c>Upsert</c> extension method through a configuration lambda.
	/// </summary>
	/// <remarks>
	/// This interface is used only as the receiver type of an expression tree captured by linq2db.
	/// The configure expression is parsed by linq2db and is not invoked; implementing this interface
	/// is not a supported extension point.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IUpsertSpec<TTarget>
		where TTarget : notnull
	{
		/// <summary>
		/// Defines the key-equality used to match source rows to target rows: a conjunction of equality
		/// comparisons between target and source member paths — e.g. <c>(t, s) =&gt; t.Id == s.Id</c> or
		/// <c>(t, s) =&gt; t.TenantId == s.TenantId &amp;&amp; t.ExternalId == s.ExternalId</c>. This is not a
		/// general boolean predicate; use the branch <c>.When(...)</c> predicates for conditional
		/// INSERT/UPDATE logic. When omitted, the target table's primary key is used (same as today's
		/// <c>InsertOrUpdate</c>).
		/// </summary>
		IUpsertSpec<TTarget> Match([InstantHandle] Expression<Func<TTarget, TTarget, bool>> matchCondition);

		/// <summary>Sets a target column's value for <b>both</b> INSERT and UPDATE branches from a context-free expression.</summary>
		IUpsertSpec<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TV>>          value);

		/// <summary>Sets a target column's value for <b>both</b> INSERT and UPDATE branches from the source row.</summary>
		IUpsertSpec<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TTarget, TV>> value);

		/// <summary>Excludes a target column from <b>both</b> INSERT and UPDATE.</summary>
		IUpsertSpec<TTarget> Ignore<TV>([InstantHandle] Expression<Func<TTarget, TV>> field);

		/// <summary>Skips the INSERT branch entirely — UPDATE-IF-EXISTS semantics.</summary>
		IUpsertSpec<TTarget> SkipInsert();

		/// <summary>Skips the UPDATE branch entirely — INSERT-IF-NOT-EXISTS semantics.</summary>
		IUpsertSpec<TTarget> SkipUpdate();

		/// <summary>Configures the INSERT branch.</summary>
		IUpsertSpec<TTarget> Insert([InstantHandle] Expression<Func<IUpsertInsertSpec<TTarget>, IUpsertInsertSpec<TTarget>>> configure);

		/// <summary>Configures the UPDATE branch.</summary>
		IUpsertSpec<TTarget> Update([InstantHandle] Expression<Func<IUpsertUpdateSpec<TTarget>, IUpsertUpdateSpec<TTarget>>> configure);
	}
}
