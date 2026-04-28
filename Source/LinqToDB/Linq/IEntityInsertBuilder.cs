using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// F-bounded base for entity-INSERT-builder fluent chains. Declares the chain methods that
	/// are common to standalone Insert (<see cref="IEntityInsertBuilder{TTarget}"/>) and the
	/// Upsert INSERT branch (<see cref="IUpsertInsertBuilder{TTarget}"/>); each leaf interface
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
	public interface IEntityInsertBuilder<TTarget, TBuilder>
		where TTarget  : notnull
		where TBuilder : IEntityInsertBuilder<TTarget, TBuilder>
	{
		/// <summary>Sets a target column's value during INSERT from a context-free expression.</summary>
		TBuilder Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TV>>          value);

		/// <summary>Sets a target column's value during INSERT from the source row.</summary>
		TBuilder Set<TV>(
			[InstantHandle] Expression<Func<TTarget, TV>> field,
			[InstantHandle] Expression<Func<TTarget, TV>> value);

		/// <summary>Excludes a target column from the INSERT statement.</summary>
		TBuilder Ignore<TV>([InstantHandle] Expression<Func<TTarget, TV>> field);
	}

	/// <summary>
	/// Fluent configuration builder for a standalone entity INSERT
	/// (<c>table.Insert(item, b =&gt; b.Set(…).Ignore(…))</c>). Carries only <c>Set</c> / <c>Ignore</c>;
	/// the Upsert-only chain methods <c>When</c> / <c>DoNothing</c> live on
	/// <see cref="IUpsertInsertBuilder{TTarget}"/> and are unreachable from this builder by design.
	/// </summary>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IEntityInsertBuilder<TTarget>
		: IEntityInsertBuilder<TTarget, IEntityInsertBuilder<TTarget>>
		where TTarget : notnull
	{
	}
}
