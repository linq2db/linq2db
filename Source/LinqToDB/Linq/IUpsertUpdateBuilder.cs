namespace LinqToDB.Linq
{
	/// <summary>
	/// Fluent configuration builder for the UPDATE branch of an Upsert operation.
	/// Received inside the lambda passed to <see cref="LinqExtensions"/>.<c>Update</c> on an <see cref="IUpsertable{TTarget, TSource}"/>.
	/// </summary>
	/// <remarks>
	/// Marker-only interface — not intended for external implementation. The only valid implementation
	/// is produced internally by the <c>Upsert</c> entry points on <see cref="LinqExtensions"/>.
	/// The chain methods (<c>.Set</c>, <c>.Ignore</c>, <c>.When</c>, <c>.DoNothing</c>) are
	/// expression-tree markers and throw <see cref="System.NotSupportedException"/> when invoked directly.
	/// </remarks>
	/// <typeparam name="TTarget">Target table record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IUpsertUpdateBuilder<TTarget, TSource>
	{
	}
}
