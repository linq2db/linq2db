using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public interface ITableMutable<out T>
		where T : notnull
	{
		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeServerName  (string? serverName);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeDatabaseName(string? databaseName);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeSchemaName  (string? schemaName);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableName   (string tableName);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableOptions(TableOptions options);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableDescriptor(EntityDescriptor tableDescriptor);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableID(string? tableID);
	}
}
