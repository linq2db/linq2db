using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for the UPDATE branch of an Upsert operation. Inherits
	/// <c>Set</c> (3 overloads) / <c>Ignore</c> from
	/// <see cref="IEntityUpdateSpec{TTarget, TBuilder}"/> (with itself substituted as
	/// <c>TBuilder</c>, so chain methods return <see cref="IUpsertUpdateSpec{TTarget}"/>) and
	/// adds the Upsert-only chain methods <see cref="When"/> / <see cref="DoNothing"/>, which have
	/// no SQL meaning outside MERGE / ON CONFLICT.
	/// </summary>
	/// <remarks>
	/// This interface is used only as the receiver type of an expression tree captured by linq2db.
	/// The configure expression is parsed by linq2db and is not invoked; implementing this interface
	/// is not a supported extension point.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IUpsertUpdateSpec<TTarget>
		: IEntityUpdateSpec<TTarget, IUpsertUpdateSpec<TTarget>>
		where TTarget : notnull
	{
		/// <summary>Adds a target/source-row predicate: update only when the predicate holds (<c>WHEN MATCHED AND …</c>).</summary>
		IUpsertUpdateSpec<TTarget> When([InstantHandle] Expression<Func<TTarget, TTarget, bool>> condition);

		/// <summary>Marks the UPDATE branch as explicitly empty.</summary>
		IUpsertUpdateSpec<TTarget> DoNothing();
	}
}
