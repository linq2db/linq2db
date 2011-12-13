using System;
using System.Linq.Expressions;

namespace LinqToDB.Data.Linq
{
	class ExpressionQuery<T> : Table<T>, IExpressionQuery
	{
		public ExpressionQuery(IDataContextInfo dataContext, Expression expression)
			: base(dataContext, expression)
		{
		}

		public new string SqlText
		{
			get { return base.SqlText; }
		}

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return base.SqlText;
		}

#endif
	}
}
