using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for the UPDATE branch of an Upsert operation. Inherits
	/// <c>Set</c> (3 overloads) / <c>Ignore</c> from
	/// <see cref="IEntityUpdateBuilder{TTarget, TBuilder}"/> (with itself substituted as
	/// <c>TBuilder</c>, so chain methods return <see cref="IUpsertUpdateBuilder{TTarget}"/>) and
	/// adds the Upsert-only chain methods <see cref="When"/> / <see cref="DoNothing"/>, which have
	/// no SQL meaning outside MERGE / ON CONFLICT.
	/// </summary>
	/// <remarks>
	/// Marker-only interface — not intended for external implementation. All chain methods are
	/// expression-tree markers; calling them outside an <see cref="Expression"/> context is
	/// undefined behaviour.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IUpsertUpdateBuilder<TTarget>
		: IEntityUpdateBuilder<TTarget, IUpsertUpdateBuilder<TTarget>>
		where TTarget : notnull
	{
		/// <summary>Adds a target/source-row predicate: update only when the predicate holds (<c>WHEN MATCHED AND …</c>).</summary>
		IUpsertUpdateBuilder<TTarget> When([InstantHandle] Expression<Func<TTarget, TTarget, bool>> condition);

		/// <summary>Marks the UPDATE branch as explicitly empty.</summary>
		IUpsertUpdateBuilder<TTarget> DoNothing();
	}
}
