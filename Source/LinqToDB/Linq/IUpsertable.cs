namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for an Upsert (insert-or-update) operation.
	/// Returned to the caller of an <c>Upsert</c> extension method through a configuration lambda.
	/// </summary>
	/// <remarks>
	/// Marker-only interface — not intended for external implementation. The only valid implementation
	/// is produced internally by the <c>Upsert</c> entry points on <see cref="LinqExtensions"/>.
	/// The chain methods (<c>.Match</c>, <c>.Set</c>, <c>.Ignore</c>, <c>.Insert</c>, <c>.Update</c>,
	/// <c>.SkipInsert</c>, <c>.SkipUpdate</c>) are expression-tree markers and throw
	/// <see cref="System.NotSupportedException"/> when invoked directly.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	/// <typeparam name="TSource">Source record type. Equals <typeparamref name="TTarget"/> for single-entity upsert; may differ when upserting from an <see cref="System.Collections.Generic.IEnumerable{T}"/> or <see cref="System.Linq.IQueryable{T}"/> source.</typeparam>
	public interface IUpsertable<TTarget, TSource>
	{
	}
}
