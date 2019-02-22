using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Linq
{
	class Table<T> : ExpressionQuery<T>, ITable<T>, ITable
	{
		public Table(IDataContext dataContext)
		{
			InitTable(dataContext, null);
		}

		public Table(IDataContext dataContext, Expression expression)
		{
			InitTable(dataContext, expression);
		}

		void InitTable(IDataContext dataContext, Expression expression)
		{
			Init(dataContext, expression);

			var ed = dataContext.MappingSchema.GetEntityDescriptor(typeof(T));

			_databaseName = ed.DatabaseName;
			_schemaName   = ed.SchemaName;
			_tableName    = ed.TableName;
		}

		// ReSharper disable StaticMemberInGenericType
		static MethodInfo _databaseNameMethodInfo;
		static MethodInfo _schemaNameMethodInfo;
		static MethodInfo _tableNameMethodInfo;
		// ReSharper restore StaticMemberInGenericType

		private string _databaseName;
		public  string  DatabaseName
		{
			get => _databaseName;
			set
			{
				if (_databaseName != value)
				{
					Expression = Expression.Call(
						null,
						_databaseNameMethodInfo ?? (_databaseNameMethodInfo = LinqExtensions.DatabaseNameMethodInfo.MakeGenericMethod(typeof(T))),
						new[] { Expression, Expression.Constant(value) });

					_databaseName = value;
				}
			}
		}

		private string _schemaName;
		public  string  SchemaName
		{
			get => _schemaName;
			set
			{
				if (_schemaName != value)
				{
					Expression = Expression.Call(
						null,
						_schemaNameMethodInfo ?? (_schemaNameMethodInfo = LinqExtensions.SchemaNameMethodInfo.MakeGenericMethod(typeof(T))),
						new[] { Expression, Expression.Constant(value) });

					_schemaName = value;
				}
			}
		}

		private string _tableName;
		public  string  TableName
		{
			get => _tableName;
			set
			{
				if (_tableName != value)
				{
					Expression = Expression.Call(
						null,
						_tableNameMethodInfo ?? (_tableNameMethodInfo = LinqExtensions.TableNameMethodInfo.MakeGenericMethod(typeof(T))),
						new[] { Expression, Expression.Constant(value) });

					_tableName = value;
				}
			}
		}

		public string GetTableName() =>
			DataContext.CreateSqlProvider()
				.ConvertTableName(new StringBuilder(), DatabaseName, SchemaName, TableName)
				.ToString();

		#region Overrides

		public override string ToString()
		{
			return $"Table({GetTableName()})";
		}

		#endregion
	}
}
