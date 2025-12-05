using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Linq
{
	public interface IDataReaderAsync : IDisposable, IAsyncDisposable
	{
		DbDataReader DataReader { get; }
		bool         Read();
		Task<bool>   ReadAsync(CancellationToken cancellationToken);
	}
}
