using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Model;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	sealed class Table<T> : ExpressionQuery<T>, ITable<T>, ITableMutable<T>
		where T : notnull
	{
		public Table(IDataContext dataContext)
		{
			var expression = dataContext.MappingSchema.IsScalarType(typeof(T)) && typeof(T) != typeof(object)
				? null
				: Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(typeof(T)),
					SqlQueryRootExpression.Create(dataContext.MappingSchema, dataContext.GetType()));

			InitTable(dataContext, expression, null);
		}

		internal Table(IDataContext dataContext, Table<T> basedOn)
		{
			Init(dataContext, basedOn.Expression);
			_tableOptions   = basedOn.TableOptions;
			_tableID        = basedOn.TableID;
			_name           = basedOn._name;
			TableDescriptor = basedOn.TableDescriptor;
		}

		internal Table(IDataContext dataContext, EntityDescriptor? tableDescriptor)
		{
			var expression = dataContext.MappingSchema.IsScalarType(typeof(T)) && typeof(T) != typeof(object)
				? null
				: Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(typeof(T)),
					SqlQueryRootExpression.Create(dataContext.MappingSchema, dataContext.GetType()));

			InitTable(dataContext, expression, tableDescriptor);
		}

		public Table(IDataContext dataContext, Expression expression)
		{
			InitTable(dataContext, expression, null);
		}

		void InitTable(IDataContext dataContext, Expression? expression, EntityDescriptor? tableDescriptor)
		{
			if (expression != null && tableDescriptor != null)
			{
				if (tableDescriptor.TableOptions != TableOptions.NotSet)
					expression = ApplyTableOptions(expression, tableDescriptor.TableOptions);

				expression = ApplyTableName(expression, tableDescriptor.Name.Name);
				if (!string.IsNullOrEmpty(tableDescriptor.Name.Schema))
					expression = ApplySchemaName(expression, tableDescriptor.Name.Schema);
				if (!string.IsNullOrEmpty(tableDescriptor.Name.Database))
					expression = ApplyDatabaseName(expression, tableDescriptor.Name.Database);
				if (!string.IsNullOrEmpty(tableDescriptor.Name.Server))
					expression = ApplyServerName(expression, tableDescriptor.Name.Server);

				expression = ApplyTableDescriptor(expression, tableDescriptor);
			}

			Init(dataContext, expression);

			var ed = tableDescriptor ?? dataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

			_name         = ed.Name;
			_tableOptions = ed.TableOptions;
		}

		internal EntityDescriptor? TableDescriptor { get; set; }

		// ReSharper disable StaticMemberInGenericType
		static MethodInfo? _serverNameMethodInfo;
		static MethodInfo? _databaseNameMethodInfo;
		static MethodInfo? _schemaNameMethodInfo;
		static MethodInfo? _tableNameMethodInfo;
		static MethodInfo? _tableOptionsMethodInfo;
		static MethodInfo? _tableIDMethodInfo;
		static MethodInfo? _tableDescriptorMethodInfo;
		// ReSharper restore StaticMemberInGenericType

		static Expression ApplyTableOptions(Expression expression, TableOptions tableOptions)
		{
			expression = Expression.Call(
				null,
				_tableOptionsMethodInfo ??= Methods.LinqToDB.Table.TableOptions.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(tableOptions));
			return expression;
		}

		static Expression ApplyTableName(Expression expression, string? tableName)
		{
			expression = Expression.Call(
				null,
				_tableNameMethodInfo ??= Methods.LinqToDB.Table.TableName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(tableName));
			return expression;
		}

		static Expression ApplyDatabaseName(Expression expression, string? databaseName)
		{
			expression = Expression.Call(
				null,
				_databaseNameMethodInfo ??= Methods.LinqToDB.Table.DatabaseName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(databaseName));
			return expression;
		}

		static Expression ApplySchemaName(Expression expression, string? schemaName)
		{
			expression = Expression.Call(
				null,
				_schemaNameMethodInfo ??= Methods.LinqToDB.Table.SchemaName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(schemaName));
			return expression;
		}

		static Expression ApplyServerName(Expression expression, string? serverName)
		{
			expression = Expression.Call(
				null,
				_serverNameMethodInfo ??= Methods.LinqToDB.Table.ServerName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(serverName));
			return expression;
		}

		static Expression ApplyTableDescriptor(Expression expression, EntityDescriptor entityDescriptor)
		{
			expression = Expression.Call(
				null,
				_tableDescriptorMethodInfo ??= Methods.LinqToDB.Table.UseTableDescriptor.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(entityDescriptor));
			return expression;
		}

		static Expression ApplyTaleId(Expression expression, string? id)
		{
			expression = Expression.Call(
				null,
				_tableIDMethodInfo ??= Methods.LinqToDB.Table.TableID.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(id, typeof(string)));
			return expression;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private SqlObjectName _name;

		public  string?  ServerName
		{
			get => _name.Server;
			set
			{
				if (_name.Server != value)
				{
					Expression = ApplyServerName(Expression, value);

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
					Expression = ApplyDatabaseName(Expression, value);

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
					Expression = ApplySchemaName(Expression, value);

					_name = _name with { Schema = value };
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private TableOptions _tableOptions;
		public  TableOptions  TableOptions
		{
			get => _tableOptions;
			set
			{
				if (_tableOptions != value)
				{
					Expression = ApplyTableOptions(Expression, value);

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
					Expression = ApplyTableName(Expression, value);

					_name = _name with { Name = value };
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private string? _tableID;
		public string?   TableID
		{
			get => _tableID;
			set
			{
				if (_tableID != value)
				{
					Expression = ApplyTaleId(Expression, value);

					_tableID = value;
				}
			}
		}

		public ITable<T> ChangeServerName(string? serverName)
		{
			return new Table<T>(DataContext, this)
			{
				ServerName = serverName
			};
		}

		public ITable<T> ChangeDatabaseName(string? databaseName)
		{
			return new Table<T>(DataContext, this)
			{
				DatabaseName = databaseName
			};
		}

		public ITable<T> ChangeSchemaName(string? schemaName)
		{
			return new Table<T>(DataContext, this)
			{
				SchemaName = schemaName
			};
		}

		public ITable<T> ChangeTableName(string tableName)
		{
			return new Table<T>(DataContext, this)
			{
				TableName = tableName
			};
		}

		public ITable<T> ChangeTableOptions(TableOptions options)
		{
			return new Table<T>(DataContext, this)
			{
				TableOptions = options
			};
		}

		public ITable<T> ChangeTableDescriptor(EntityDescriptor tableDescriptor)
		{
			return new Table<T>(DataContext, this)
			{
				TableDescriptor = tableDescriptor
			};
		}

		public ITable<T> ChangeTableID(string? tableID)
		{
			return new Table<T>(DataContext, this)
			{
				SchemaName      = SchemaName,
				ServerName      = ServerName,
				DatabaseName    = DatabaseName,
				Expression      = Expression,
				TableName       = TableName,
				TableOptions    = TableOptions,
				TableDescriptor = TableDescriptor,
				TableID         = tableID,
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
