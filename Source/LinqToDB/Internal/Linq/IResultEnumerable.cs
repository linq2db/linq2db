using System.Collections.Generic;

namespace LinqToDB.Internal.Linq
{
	interface IResultEnumerable<out T> : IEnumerable<T>, IAsyncEnumerable<T>
	{
	}
}
