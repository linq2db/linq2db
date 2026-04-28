using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for an entity INSERT branch (used by Upsert and other
	/// entity-shaped insert APIs).
	/// Received inside the lambda passed to <see cref="IEntityUpsertBuilder{TTarget}.Insert"/>.
	/// </summary>
	/// <remarks>
	/// Marker-only interface — not intended for external implementation. The chain methods
	/// (<c>Set</c>, <c>Ignore</c>, <see cref="When"/>, <see cref="DoNothing"/>) are
	/// expression-tree markers; calling them outside an <see cref="Expression"/> context is
	/// undefined behaviour.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IEntityInsertBuilder<TTarget>
		where TTarget : notnull
	{
		/// <summary>Adds a source-row predicate: insert only when the predicate holds (<c>WHEN NOT MATCHED AND …</c>).</summary>
		IEntityInsertBuilder<TTarget> When([InstantHandle] Expression<Func<TTarget, bool>> condition);

		/// <summary>Marks the INSERT branch as explicitly empty.</summary>
		IEntityInsertBuilder<TTarget> DoNothing();

		/// <summary>Sets a target column's value during INSERT from a context-free expression.</summary>
		IEntityInsertBuilder<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TV>>          value);

		/// <summary>Sets a target column's value during INSERT from the source row.</summary>
		IEntityInsertBuilder<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TTarget, TV>> value);

		/// <summary>Excludes a target column from the INSERT statement.</summary>
		IEntityInsertBuilder<TTarget> Ignore<TV>([InstantHandle] Expression<Func<TTarget, TV>> field);
	}
}
