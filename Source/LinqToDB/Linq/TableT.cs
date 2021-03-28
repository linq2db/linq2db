using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Linq
{
	using Extensions;
	using Reflection;

	class Table<T> : ExpressionQuery<T>, ITable<T>, ITableMutable<T>, ITable
		where T : notnull
	{
		public Table(IDataContext dataContext)
		{
			var expression = typeof(T).IsScalar()
				? null
				: Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(typeof(T)),
					Expression.Constant(dataContext));

			InitTable(dataContext, expression);
		}

		public Table(IDataContext dataContext, Expression expression)
		{
			InitTable(dataContext, expression);
		}

		void InitTable(IDataContext dataContext, Expression? expression)
		{
			Init(dataContext, expression);

			var ed = dataContext.MappingSchema.GetEntityDescriptor(typeof(T));

			_serverName   = ed.ServerName;
			_databaseName = ed.DatabaseName;
			_schemaName   = ed.SchemaName;
			_tableName    = ed.TableName;
			_tableOptions = ed.TableOptions;
		}

		// ReSharper disable StaticMemberInGenericType
		static MethodInfo? _serverNameMethodInfo;
		static MethodInfo? _databaseNameMethodInfo;
		static MethodInfo? _schemaNameMethodInfo;
		static MethodInfo? _tableNameMethodInfo;
		static MethodInfo? _tableOptionsMethodInfo;
		// ReSharper restore StaticMemberInGenericType

		private string? _serverName;
		public  string?  ServerName
		{
			get => _serverName;
			set
			{
				if (_serverName != value)
				{
					Expression = Expression.Call(
						null,
						_serverNameMethodInfo ??= Methods.LinqToDB.Table.ServerName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_serverName = value;
				}
			}
		}

		private string? _databaseName;
		public  string?  DatabaseName
		{
			get => _databaseName;
			set
			{
				if (_databaseName != value)
				{
					Expression = Expression.Call(
						null,
						_databaseNameMethodInfo ??= Methods.LinqToDB.Table.DatabaseName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_databaseName = value;
				}
			}
		}

		private string? _schemaName;
		public  string?  SchemaName
		{
			get => _schemaName;
			set
			{
				if (_schemaName != value)
				{
					Expression = Expression.Call(
						null,
						_schemaNameMethodInfo ??= Methods.LinqToDB.Table.SchemaName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_schemaName = value;
				}
			}
		}

		private TableOptions _tableOptions;
		public  TableOptions  TableOptions
		{
			get => _tableOptions;
			set
			{
				if (_tableOptions != value)
				{
					Expression = Expression.Call(
						null,
						_tableOptionsMethodInfo ??= Methods.LinqToDB.Table.TableOptions.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_tableOptions = value;
				}
			}
		}

		private string _tableName = null!;
		public  string  TableName
		{
			get => _tableName;
			set
			{
				if (_tableName != value)
				{
					Expression = Expression.Call(
						null,
						_tableNameMethodInfo ??= Methods.LinqToDB.Table.TableName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_tableName = value;
				}
			}
		}

		public string GetTableName() =>
			DataContext.CreateSqlProvider()
				.ConvertTableName(new StringBuilder(), ServerName, DatabaseName, SchemaName, TableName, TableOptions)
				.ToString();

		public ITable<T> ChangeServerName(string? serverName)
		{
			return new Table<T>(DataContext)
			{
				TableName    = TableName,
				SchemaName   = SchemaName,
				DatabaseName = DatabaseName,
				Expression   = Expression,
				ServerName   = serverName,
				TableOptions = TableOptions
			};
		}

		public ITable<T> ChangeDatabaseName(string? databaseName)
		{
			return new Table<T>(DataContext)
			{
				TableName    = TableName,
				SchemaName   = SchemaName,
				ServerName   = ServerName,
				Expression   = Expression,
				DatabaseName = databaseName,
				TableOptions = TableOptions
			};
		}

		public ITable<T> ChangeSchemaName(string? schemaName)
		{
			return new Table<T>(DataContext)
			{
				TableName    = TableName,
				ServerName   = ServerName,
				DatabaseName = DatabaseName,
				Expression   = Expression,
				SchemaName   = schemaName,
				TableOptions = TableOptions
			};
		}

		public ITable<T> ChangeTableName(string tableName)
		{
			return new Table<T>(DataContext)
			{
				SchemaName   = SchemaName,
				ServerName   = ServerName,
				DatabaseName = DatabaseName,
				Expression   = Expression,
				TableName    = tableName,
				TableOptions = TableOptions
			};
		}

		public ITable<T> ChangeTableOptions(TableOptions options)
		{
			return new Table<T>(DataContext)
			{
				SchemaName   = SchemaName,
				ServerName   = ServerName,
				DatabaseName = DatabaseName,
				Expression   = Expression,
				TableName    = TableName,
				TableOptions = options
			};
		}

		#region Overrides

		public override string ToString()
		{
			return $"Table({GetTableName()})";
		}

		#endregion
	}
}
