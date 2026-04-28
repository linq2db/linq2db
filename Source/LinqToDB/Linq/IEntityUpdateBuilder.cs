using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for an entity UPDATE branch (used by Upsert and other
	/// entity-shaped update APIs).
	/// Received inside the lambda passed to <see cref="IEntityUpsertBuilder{TTarget}.Update"/>.
	/// </summary>
	/// <remarks>
	/// Marker-only interface — not intended for external implementation. The chain methods
	/// (<c>Set</c>, <c>Ignore</c>, <see cref="When"/>, <see cref="DoNothing"/>) are
	/// expression-tree markers; calling them outside an <see cref="Expression"/> context is
	/// undefined behaviour.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IEntityUpdateBuilder<TTarget>
		where TTarget : notnull
	{
		/// <summary>Adds a target/source-row predicate: update only when the predicate holds (<c>WHEN MATCHED AND …</c>).</summary>
		IEntityUpdateBuilder<TTarget> When([InstantHandle] Expression<Func<TTarget, TTarget, bool>> condition);

		/// <summary>Marks the UPDATE branch as explicitly empty.</summary>
		IEntityUpdateBuilder<TTarget> DoNothing();

		/// <summary>Sets a target column's value during UPDATE from a context-free expression.</summary>
		IEntityUpdateBuilder<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TV>>          value);

		/// <summary>Sets a target column's value during UPDATE from the source row.</summary>
		IEntityUpdateBuilder<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TTarget, TV>> value);

		/// <summary>Sets a target column's value during UPDATE from both the current target row and the source row.</summary>
		IEntityUpdateBuilder<TTarget> Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>>          field,
			[InstantHandle] Expression<Func<TTarget, TTarget, TV>> value);

		/// <summary>Excludes a target column from the UPDATE statement.</summary>
		IEntityUpdateBuilder<TTarget> Ignore<TV>([InstantHandle] Expression<Func<TTarget, TV>> field);
	}
}
