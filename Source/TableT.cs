using System.Linq.Expressions;

namespace LinqToDB
{
	using Linq;

	public class Table<T> : ExpressionQuery<T>, ITable
	{
		public Table(IDataContextInfo dataContextInfo, Expression expression)
		{
			Init(dataContextInfo, expression);
		}

		public Table(IDataContextInfo dataContextInfo)
		{
			Init(dataContextInfo, null);
		}

#if !SILVERLIGHT

		public Table()
		{
			Init(null, null);
		}

		public Table(Expression expression)
		{
			Init(null, expression);
		}

#endif

		public Table(IDataContext dataContext)
		{
			Init(dataContext == null ? null : new DataContextInfo(dataContext), null);
		}

		public Table(IDataContext dataContext, Expression expression)
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
}
