namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for an Upsert (insert-or-update) operation.
	/// Returned to the caller of an <c>Upsert</c> extension method through a configuration lambda.
	/// </summary>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	/// <typeparam name="TSource">Source record type. Equals <typeparamref name="TTarget"/> for single-entity upsert; may differ when upserting from an <see cref="System.Collections.Generic.IEnumerable{T}"/> or <see cref="System.Linq.IQueryable{T}"/> source.</typeparam>
	public interface IUpsertable<TTarget, TSource>
	{
	}
}
