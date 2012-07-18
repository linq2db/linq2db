using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Data.Linq;

	class ExpressionQueryImpl<T> : ExpressionQuery<T>, IExpressionQuery
	{
		public ExpressionQueryImpl(IDataContextInfo dataContext, Expression expression)
			: base(dataContext, expression)
		{
		}

		//public new string SqlText
		//{
		//	get { return base.SqlText; }
		//}

//#if OVERRIDETOSTRING

		public override string ToString()
		{
			return base.SqlText;
		}

//#endif
	}
}
