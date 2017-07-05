using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class QueryContext
	{
		public QueryContext(IDataContext dataContext, Expression expr, object[] compiledParameters)
		{
			DataContext        = dataContext;
			Expression         = expr;
			CompiledParameters = compiledParameters;
		}

		public IDataContext DataContext;
		public Expression   Expression;
		public object[]     CompiledParameters;
	}
}
