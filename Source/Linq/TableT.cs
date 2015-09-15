using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class Table<T> : ExpressionQuery<T>, ITable<T>, ITable
	{
		public Table(IDataContextInfo dataContextInfo)
		{
			Init(dataContextInfo, null);
		}

		public Table(IDataContext dataContext)
		{
			Init(dataContext == null ? null : new DataContextInfo(dataContext), null);
		}

		public Table(IDataContext dataContext, Expression expression)
		{
			Init(dataContext == null ? null : new DataContextInfo(dataContext), expression);
		}

#if !SILVERLIGHT

		public Table()
		{
			Init(null, null);
		}

#endif
		public string DatabaseName;
		public string SchemaName;
		public string TableName;

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return "Table(" + typeof(T).Name + ")";
		}

#endif

		#endregion
	}

	class TableQuery<T> : ExpressionQueryNew<T>, ITable<T>, ITable
	{
		public TableQuery(IDataContext dataContext)
			: base(dataContext, null)
		{
		}

		public override string ToString()
		{
			return "Table(" + typeof(T).Name + ")";
		}
	}
}
