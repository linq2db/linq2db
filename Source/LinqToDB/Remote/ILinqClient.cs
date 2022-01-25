using System.Threading.Tasks;
using LinqToDB.Remote.Independent;

namespace LinqToDB.Remote
{
	public interface ILinqClient
	{
		LinqServiceInfo GetInfo        (string? configuration);
		int             ExecuteNonQuery(string? configuration, string queryData);
		object?         ExecuteScalar  (string? configuration, string queryData);
		string          ExecuteReader  (string? configuration, string queryData);
		int             ExecuteBatch   (string? configuration, string queryData);

		Task<LinqServiceInfo> GetInfoAsync        (string? configuration);
		Task<int>             ExecuteNonQueryAsync(string? configuration, string queryData);
		Task<object?>         ExecuteScalarAsync  (string? configuration, string queryData);
		Task<string>          ExecuteReaderAsync  (string? configuration, string queryData);
		Task<int>             ExecuteBatchAsync   (string? configuration, string queryData);
	}
}
