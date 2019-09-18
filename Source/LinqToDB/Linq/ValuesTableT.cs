using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Linq
{
	class ValuesTable<T> : ExpressionQuery<T>, ITable<T>, ITable
	{
		readonly IEnumerable<T> _valuesSource;

		public ValuesTable(IDataContext dataContext, IEnumerable<T> valuesSource)
		{
			_valuesSource = valuesSource;

			Init(dataContext, null);
		}

		public  string  DatabaseName => null;
		public  string  SchemaName   => null;

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
						LinqExtensions.TableNameMethodInfo.MakeGenericMethod(typeof(T)),
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
