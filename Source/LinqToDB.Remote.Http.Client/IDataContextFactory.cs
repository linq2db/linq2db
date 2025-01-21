using System;

namespace LinqToDB.Remote.Http.Client
{
	public interface IDataContextFactory<T>
	where T : IDataContext
	{
		T GetDataContext();
	}
}
