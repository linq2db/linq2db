using System.Collections.Generic;

namespace LinqToDB.Linq
{
	public interface IResultEnumerable<out T> : IEnumerable<T>, IAsyncEnumerable<T>
	{
	}
}
