using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Remote
{
	public interface ILinqService
	{
		Task<LinqServiceInfo> GetInfoAsync        (string? configuration, CancellationToken cancellationToken = default);
		Task<int>             ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken = default);
		Task<string?>         ExecuteScalarAsync  (string? configuration, string queryData, CancellationToken cancellationToken = default);
		Task<string>          ExecuteReaderAsync  (string? configuration, string queryData, CancellationToken cancellationToken = default);
		Task<int>             ExecuteBatchAsync   (string? configuration, string queryData, CancellationToken cancellationToken = default);

		string? RemoteClientTag { get; set; }
	}
}
