using LinqToDB.Mapping;
using LinqToDB.Model;

namespace LinqToDB.Internal.Linq
{
	public interface ITableMutable<out T>
		where T : notnull
	{
		ITable<T> ChangeServerName  (string? serverName);

		ITable<T> ChangeDatabaseName(string? databaseName);

		ITable<T> ChangeSchemaName  (string? schemaName);

		ITable<T> ChangeTableName   (string tableName);

		ITable<T> ChangeTableOptions(TableOptions options);

		ITable<T> ChangeTableDescriptor(EntityDescriptor tableDescriptor);

		ITable<T> ChangeTableID(string? tableID);
	}
}
