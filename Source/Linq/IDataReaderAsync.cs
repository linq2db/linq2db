using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	interface IDataReaderAsync
	{
		Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken);
	}
}
