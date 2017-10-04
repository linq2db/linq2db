using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	public interface IDataReaderAsync
	{
		Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Func<T,bool> action, CancellationToken cancellationToken);
	}
}
