using System.Collections.Generic;
using LinqToDB.Async;

namespace LinqToDB.Linq
{
	public interface IResultEnumerable<out T> : IEnumerable<T>, IAsyncEnumerable<T>
	{
		
	}

}
