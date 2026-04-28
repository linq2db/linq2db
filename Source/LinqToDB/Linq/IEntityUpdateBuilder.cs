using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// F-bounded base for entity-UPDATE-builder fluent chains. Declares the chain methods that
	/// are common to standalone Update (<see cref="IEntityUpdateBuilder{TTarget}"/>) and the
	/// Upsert UPDATE branch (<see cref="IUpsertUpdateBuilder{TTarget}"/>); each leaf interface
	/// substitutes itself as <typeparamref name="TBuilder"/> so chain methods return the correct
	/// concrete builder type.
	/// </summary>
	/// <remarks>
	/// Marker-only interface — not intended for external implementation. All chain methods are
	/// expression-tree markers; calling them outside an <see cref="Expression"/> context is
	/// undefined behaviour.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	/// <typeparam name="TBuilder">Concrete leaf builder type (F-bound: must derive from this interface with itself substituted).</typeparam>
	[PublicAPI]
	public interface IEntityUpdateBuilder<TTarget, TBuilder>
		where TTarget  : notnull
		where TBuilder : IEntityUpdateBuilder<TTarget, TBuilder>
	{
		/// <summary>Sets a target column's value during UPDATE from a context-free expression.</summary>
		TBuilder Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TV>>          value);

		/// <summary>Sets a target column's value during UPDATE from the source row.</summary>
		TBuilder Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TTarget, TV>> value);

		/// <summary>Sets a target column's value during UPDATE from both the current target row and the source row.</summary>
		TBuilder Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>>          field,
			[InstantHandle] Expression<Func<TTarget, TTarget, TV>> value);

		/// <summary>Excludes a target column from the UPDATE statement.</summary>
		TBuilder Ignore<TV>([InstantHandle] Expression<Func<TTarget, TV>> field);
	}

	/// <summary>
	/// Fluent configuration builder for a standalone entity UPDATE
	/// (<c>table.Update(item, b =&gt; b.Set(…).Ignore(…))</c>). Carries only <c>Set</c> / <c>Ignore</c>;
	/// the Upsert-only chain methods <c>When</c> / <c>DoNothing</c> live on
	/// <see cref="IUpsertUpdateBuilder{TTarget}"/> and are unreachable from this builder by design.
	/// </summary>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IEntityUpdateBuilder<TTarget>
		: IEntityUpdateBuilder<TTarget, IEntityUpdateBuilder<TTarget>>
		where TTarget : notnull
	{
	}
}
