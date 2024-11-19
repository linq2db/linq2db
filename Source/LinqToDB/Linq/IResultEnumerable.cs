using System.Collections.Generic;

namespace LinqToDB.Linq
{
	using Async;

	public interface IResultEnumerable<out T> : IEnumerable<T>, IAsyncEnumerable<T>
	{
	}
}
