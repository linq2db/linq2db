using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for the INSERT branch of an Upsert operation. Inherits
	/// <c>Set</c> / <c>Ignore</c> from <see cref="IEntityInsertSpec{TTarget, TBuilder}"/>
	/// (with itself substituted as <c>TBuilder</c>, so chain methods return
	/// <see cref="IUpsertInsertSpec{TTarget}"/>) and adds the Upsert-only chain methods
	/// <see cref="When"/> / <see cref="DoNothing"/>, which have no SQL meaning outside MERGE /
	/// ON CONFLICT.
	/// </summary>
	/// <remarks>
	/// This interface is used only as the receiver type of an expression tree captured by linq2db.
	/// The configure expression is parsed by linq2db and is not invoked; implementing this interface
	/// is not a supported extension point.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	[PublicAPI]
	public interface IUpsertInsertSpec<TTarget>
		: IEntityInsertSpec<TTarget, IUpsertInsertSpec<TTarget>>
		where TTarget : notnull
	{
		/// <summary>Adds a source-row predicate: insert only when the predicate holds (<c>WHEN NOT MATCHED AND …</c>).</summary>
		IUpsertInsertSpec<TTarget> When([InstantHandle] Expression<Func<TTarget, bool>> condition);

		/// <summary>Marks the INSERT branch as explicitly empty.</summary>
		IUpsertInsertSpec<TTarget> DoNothing();
	}
}
