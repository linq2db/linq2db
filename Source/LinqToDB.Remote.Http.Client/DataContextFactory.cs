using System;

namespace LinqToDB.Remote.Http.Client
{
	public class DataContextFactory<T>(Func<T> factory) : IDataContextFactory<T>
	where T : IDataContext
	{
		public T GetDataContext() => factory();
	}
}
