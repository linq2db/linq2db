using System;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Linq
{
	class Table<T> : ExpressionQuery<T>, ITable<T>, ITable
	{
		public Table(IDataContext dataContext)
		{
			Init(dataContext, null);
		}

		public Table(IDataContext dataContext, Expression expression)
		{
			Init(dataContext, expression);
		}

		public string DatabaseName { get; set; }
		public string SchemaName   { get; set; }
		public string TableName    { get; set; }

		public string GetTableName() =>
			DataContext.CreateSqlProvider()
				.ConvertTableName(new StringBuilder(), DatabaseName, SchemaName, TableName)
				.ToString();

		#region Overrides

		public override string ToString()
		{
			return "Table(" + typeof(T).Name + ")";
		}

		#endregion
	}
}
