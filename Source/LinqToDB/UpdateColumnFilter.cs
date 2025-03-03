using LinqToDB.Model;

namespace LinqToDB
{
	/// <summary>
	/// Defines signature for column filter for update operations.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	/// <param name="entity">Entity instance.</param>
	/// <param name="column">Descriptor of column.</param>
	/// <returns><c>true</c>, if column should be included in operation and <c>false</c> otherwise.</returns>
	public delegate bool UpdateColumnFilter<T>(T entity, ColumnDescriptor column);
}
