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
	/// Marker-only interface — not intended for external implementation. The chain methods
	/// (<see cref="Match"/>, <c>Set</c>, <c>Ignore</c>, <see cref="Insert"/>, <see cref="Update"/>,
	/// <see cref="SkipInsert"/>, <see cref="SkipUpdate"/>) are expression-tree markers; calling
	/// them outside an <see cref="Expression"/> context is undefined behaviour.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IEntityUpsertBuilder<TTarget>
		where TTarget : notnull
	{
		/// <summary>
		/// Defines the match condition used to decide between INSERT and UPDATE.
		/// When omitted, the target table's primary key is used (same as today's <c>InsertOrUpdate</c>).
		/// </summary>
		IEntityUpsertBuilder<TTarget> Match([InstantHandle] Expression<Func<TTarget, TTarget, bool>> matchCondition);

		/// <summary>Sets a target column's value for <b>both</b> INSERT and UPDATE branches from a context-free expression.</summary>
		IEntityUpsertBuilder<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TV>>          value);

		/// <summary>Sets a target column's value for <b>both</b> INSERT and UPDATE branches from the source row.</summary>
		IEntityUpsertBuilder<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TTarget, TV>> value);

		/// <summary>Excludes a target column from <b>both</b> INSERT and UPDATE.</summary>
		IEntityUpsertBuilder<TTarget> Ignore<TV>([InstantHandle] Expression<Func<TTarget, TV>> field);

		/// <summary>Skips the INSERT branch entirely — UPDATE-IF-EXISTS semantics.</summary>
		IEntityUpsertBuilder<TTarget> SkipInsert();

		/// <summary>Skips the UPDATE branch entirely — INSERT-IF-NOT-EXISTS semantics.</summary>
		IEntityUpsertBuilder<TTarget> SkipUpdate();

		/// <summary>Configures the INSERT branch.</summary>
		IEntityUpsertBuilder<TTarget> Insert([InstantHandle] Expression<Func<IEntityInsertBuilder<TTarget>, IEntityInsertBuilder<TTarget>>> configure);

		/// <summary>Configures the UPDATE branch.</summary>
		IEntityUpsertBuilder<TTarget> Update([InstantHandle] Expression<Func<IEntityUpdateBuilder<TTarget>, IEntityUpdateBuilder<TTarget>>> configure);
	}
}
