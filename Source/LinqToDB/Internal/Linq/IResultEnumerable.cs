using System.Collections.Generic;

namespace LinqToDB.Internal.Linq
{
	public interface IResultEnumerable<out T> : IEnumerable<T>, IAsyncEnumerable<T>
	{
	}
}
