using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Data.Linq;

	public class Table<T> : ExpressionQuery<T>, ITable
	{
		public Table(IDataContextInfo dataContextInfo, Expression expression)
			: base(dataContextInfo, expression)
		{
		}

		public Table(IDataContextInfo dataContextInfo)
			: base(dataContextInfo, null)
		{
		}

#if !SILVERLIGHT

		public Table()
			: base(null, null)
		{
		}

		public Table(Expression expression)
			: base(null, expression)
		{
		}

#endif

		public Table(IDataContext dataContext)
			: base(dataContext == null ? null : new DataContextInfo(dataContext), null)
		{
		}

		public Table(IDataContext dataContext, Expression expression)
			: base(dataContext == null ? null : new DataContextInfo(dataContext), expression)
		{
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
}
