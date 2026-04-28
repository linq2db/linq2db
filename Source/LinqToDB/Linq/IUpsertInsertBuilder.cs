using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for the INSERT branch of an Upsert operation. Inherits
	/// <c>Set</c> / <c>Ignore</c> from <see cref="IEntityInsertBuilder{TTarget, TBuilder}"/>
	/// (with itself substituted as <c>TBuilder</c>, so chain methods return
	/// <see cref="IUpsertInsertBuilder{TTarget}"/>) and adds the Upsert-only chain methods
	/// <see cref="When"/> / <see cref="DoNothing"/>, which have no SQL meaning outside MERGE /
	/// ON CONFLICT.
	/// </summary>
	/// <remarks>
	/// Marker-only interface — not intended for external implementation. All chain methods are
	/// expression-tree markers; calling them outside an <see cref="Expression"/> context is
	/// undefined behaviour.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IUpsertInsertBuilder<TTarget>
		: IEntityInsertBuilder<TTarget, IUpsertInsertBuilder<TTarget>>
		where TTarget : notnull
	{
		/// <summary>Adds a source-row predicate: insert only when the predicate holds (<c>WHEN NOT MATCHED AND …</c>).</summary>
		IUpsertInsertBuilder<TTarget> When([InstantHandle] Expression<Func<TTarget, bool>> condition);

		/// <summary>Marks the INSERT branch as explicitly empty.</summary>
		IUpsertInsertBuilder<TTarget> DoNothing();
	}
}
