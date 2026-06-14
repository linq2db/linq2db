using LinqToDB.Mapping;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Single-column wrapper for scalar element types in
	/// <c>AsQueryable(...).UseTempTable(...)</c> and the scalar
	/// <c>UseTempTablesForContains</c> rewrite. linq2db's table creation requires an entity
	/// type with at least one mapped column — scalars (<see langword="int"/>,
	/// <see langword="string"/>, etc.) have none, so the run-step wraps each value in a
	/// <c>ValueHolder&lt;T&gt;</c> before BULK-inserting into the temp table. The column
	/// name <c>item</c> matches the implicit alias the inline-VALUES path uses for scalar
	/// AsQueryable (see <c>SequenceHelper.CreateSpecialProperty(..., "item")</c>) and the
	/// name <c>EnumerableBuilder.BuildScalarValuesTableForContains</c> assigns to the
	/// companion's SqlField, so the SQL projection
	/// (<c>SELECT [t1].[item] FROM temp.[T_xxx] [t1]</c>) resolves correctly in both paths.
	/// Entity / composite-PK Contains uses the user's entity type directly — no wrapper.
	/// </summary>
	internal sealed class ValueHolder<T>
		where T : notnull
	{
		[Column("item")]
		public T Value { get; set; } = default!;
	}
}
