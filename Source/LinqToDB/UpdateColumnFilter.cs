using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// Defines signature for column filter for update operations.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	/// <param name="entity">Entity instance.</param>
	/// <param name="column">Descriptor of column.</param>
	/// <returns><see langword="true"/>, if column should be included in operation and <see langword="false"/> otherwise.</returns>
	public delegate bool UpdateColumnFilter<T>(T entity, ColumnDescriptor column);
}
