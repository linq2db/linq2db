using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	public interface IDataReaderAsync : IDisposable, IAsyncDisposable
	{
		DbDataReader DataReader { get; }
		Task<bool>   ReadAsync(CancellationToken cancellationToken);
	}
}
