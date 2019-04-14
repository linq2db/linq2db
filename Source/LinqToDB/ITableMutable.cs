namespace LinqToDB
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public interface ITableMutable<out T>
	{
		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeDatabaseName(string databaseName);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeSchemaName  (string schemaName);

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableName   (string tableName);
	}
}
