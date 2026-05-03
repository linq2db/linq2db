using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// Defines signature for column filter for insert or update/replace operations.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	/// <param name="entity">Entity instance.</param>
	/// <param name="column">Descriptor of column.</param>
	/// <param name="isInsert">If <see langword="true"/>, filter applied to insert operation, otherwise to update/replace.</param>
	/// <returns><see langword="true"/>, if column should be included in operation and <see langword="false"/> otherwise.</returns>
	public delegate bool InsertOrUpdateColumnFilter<T>(T entity, ColumnDescriptor column, bool isInsert);
}
