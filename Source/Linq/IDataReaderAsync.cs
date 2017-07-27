using System;
using System.Data;
using System.Threading;

#if !SL4
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	interface IDataReaderAsync
	{
#if !SL4
		Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken);
#endif
	}
}
