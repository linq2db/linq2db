using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class TableOld<T> : ExpressionQueryOld<T>, ITable<T>, ITable
	{
		public TableOld(IDataContextInfo dataContextInfo, Expression expression)
		{
			Init(dataContextInfo, expression);
		}

		public TableOld(IDataContextInfo dataContextInfo)
		{
			Init(dataContextInfo, null);
		}

#if !SILVERLIGHT

		public TableOld()
		{
			Init(null, null);
		}

		public TableOld(Expression expression)
		{
			Init(null, expression);
		}

#endif

		public TableOld(IDataContext dataContext)
		{
			Init(dataContext == null ? null : new DataContextInfo(dataContext), null);
		}

		public TableOld(IDataContext dataContext, Expression expression)
		{
			Init(dataContext == null ? null : new DataContextInfo(dataContext), expression);
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return "Table(" + typeof (T).Name + ")";
		}

#endif

		#endregion
	}

	class Table<T> : ExpressionQuery<T>, ITable<T>, ITable
	{
		public Table(IDataContext dataContext)
			: base(dataContext, null)
		{
		}

		public override string ToString()
		{
			return "Table(" + typeof(T).Name + ")";
		}
	}
}
