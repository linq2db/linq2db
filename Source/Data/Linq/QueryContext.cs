using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Data.Linq
{
	public class QueryContext
	{
		public class DataContextContext
		{
			public IDataContextInfo DataContextInfo;
			public bool             InUse;
		}

		public QueryContext(IDataContextInfo dataContext, Expression expr, object[] compiledParameters)
		{
			RootDataContext    = dataContext;
			Expression         = expr;
			CompiledParameters = compiledParameters;
		}

		public IDataContextInfo        RootDataContext;
		public Expression              Expression;
		public object[]                CompiledParameters;
		public int                     Counter;

		List<DataContextContext> _contexts;

		public DataContextContext GetDataContext()
		{
			if (_contexts == null)
			{
				RootDataContext.DataContext.OnClosing += OnRootClosing;
				_contexts = new List<DataContextContext>(1);
			}

			foreach (var context in _contexts)
			{
				if (!context.InUse)
				{
					context.InUse = true;
					return context;
				}
			}

			var ctx = new DataContextContext { DataContextInfo = RootDataContext.Clone(), InUse = true };

			_contexts.Add(ctx);

			return ctx;
		}

		public void ReleaseDataContext(DataContextContext context)
		{
			context.InUse = false;
		}

		void OnRootClosing(object sender, EventArgs e)
		{
			foreach (var context in _contexts)
				context.DataContextInfo.DataContext.Dispose();

			RootDataContext.DataContext.OnClosing -= OnRootClosing;

			_contexts = null;
		}

		public void AfterQuery()
		{
			Counter++;
		}
	}
}
