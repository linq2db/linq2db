using System;

namespace LinqToDB.Remote
{
	public interface ILinqService<T> : ILinqService
		where T : IDataContext
	{
	}
}
