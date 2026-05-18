using LinqToDB.Mapping;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Single-column wrapper for scalar element types in <c>AsQueryable(...).UseTempTable(...)</c>.
	/// linq2db's table creation requires an entity type with at least one mapped column —
	/// scalars (<see langword="int"/>, <see langword="string"/>, etc.) have none, so the
	/// <c>EnumerableBuilder</c> wraps each value in a <c>ValueHolder&lt;T&gt;</c> before passing
	/// the list to the temp-table run-step. The column name <c>item</c> matches the implicit
	/// alias the inline-VALUES path uses for scalar AsQueryable (see
	/// <c>SequenceHelper.CreateSpecialProperty(..., "item")</c>), so the SQL projection
	/// (<c>SELECT [t1].[item] FROM temp.[T_xxx] [t1]</c>) resolves correctly.
	/// </summary>
	internal sealed class ValueHolder<T>
		where T : notnull
	{
		[Column("item")]
		public T Value { get; set; } = default!;
	}
}
