using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Linq
{
	using Extensions;
	using LinqToDB.SqlQuery;
	using Reflection;

	sealed class Table<T> : ExpressionQuery<T>, ITable<T>, ITableMutable<T>, ITable
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

			_name         = ed.Name;
			_tableOptions = ed.TableOptions;
		}

		// ReSharper disable StaticMemberInGenericType
		static MethodInfo? _serverNameMethodInfo;
		static MethodInfo? _databaseNameMethodInfo;
		static MethodInfo? _schemaNameMethodInfo;
		static MethodInfo? _tableNameMethodInfo;
		static MethodInfo? _tableOptionsMethodInfo;
		static MethodInfo? _tableIDMethodInfo;
		// ReSharper restore StaticMemberInGenericType

		private SqlObjectName _name;

		public  string?  ServerName
		{
			get => _name.Server;
			set
			{
				if (_name.Server != value)
				{
					Expression = Expression.Call(
						null,
						_serverNameMethodInfo ??= Methods.LinqToDB.Table.ServerName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_name = _name with { Server = value };
				}
			}
		}

		public  string?  DatabaseName
		{
			get => _name.Database;
			set
			{
				if (_name.Database != value)
				{
					Expression = Expression.Call(
						null,
						_databaseNameMethodInfo ??= Methods.LinqToDB.Table.DatabaseName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_name = _name with { Database = value };
				}
			}
		}

		public  string?  SchemaName
		{
			get => _name.Schema;
			set
			{
				if (_name.Schema != value)
				{
					Expression = Expression.Call(
						null,
						_schemaNameMethodInfo ??= Methods.LinqToDB.Table.SchemaName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_name = _name with { Schema = value };
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

		public  string  TableName
		{
			get => _name.Name;
			set
			{
				if (_name.Name != value)
				{
					Expression = Expression.Call(
						null,
						_tableNameMethodInfo ??= Methods.LinqToDB.Table.TableName.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value));

					_name = _name with { Name = value };
				}
			}
		}

		private string? _tableID;
		public string?   TableID
		{
			get => _tableID;
			set
			{
				if (_tableID != value)
				{
					Expression = Expression.Call(
						null,
						_tableIDMethodInfo ??= Methods.LinqToDB.Table.TableID.MakeGenericMethod(typeof(T)),
						Expression, Expression.Constant(value, typeof(string)));

					_tableID = value;
				}
			}
		}

		public ITable<T> ChangeServerName(string? serverName)
		{
			return new Table<T>(DataContext)
			{
				TableName    = TableName,
				SchemaName   = SchemaName,
				DatabaseName = DatabaseName,
				Expression   = Expression,
				ServerName   = serverName,
				TableOptions = TableOptions,
				TableID      = TableID,
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
				TableOptions = TableOptions,
				TableID      = TableID,
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
				TableOptions = TableOptions,
				TableID      = TableID,
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
				TableOptions = TableOptions,
				TableID      = TableID,
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
				TableOptions = options,
				TableID      = TableID,
			};
		}

		public ITable<T> ChangeTableID(string? tableID)
		{
			return new Table<T>(DataContext)
			{
				SchemaName   = SchemaName,
				ServerName   = ServerName,
				DatabaseName = DatabaseName,
				Expression   = Expression,
				TableName    = TableName,
				TableOptions = TableOptions,
				TableID      = tableID,
			};
		}

		#region Overrides

		public override string ToString()
		{
			return $"Table({this.GetTableName()})";
		}

		#endregion
	}
}
