using System;

namespace LinqToDB.Remote
{
	public class DataContextFactory<TContext>(Func<string?,TContext> factory) : IDataContextFactory<TContext>
	where TContext : IDataContext
	{
		public TContext GetDataContext(string? configuration) => factory(configuration);
	}
}
