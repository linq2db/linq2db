namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for the INSERT branch of an Upsert operation.
	/// Received inside the lambda passed to <see cref="LinqExtensions"/>.<c>Insert</c> on an <see cref="IUpsertable{TTarget, TSource}"/>.
	/// </summary>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IUpsertInsertBuilder<TTarget, TSource>
	{
	}
}
