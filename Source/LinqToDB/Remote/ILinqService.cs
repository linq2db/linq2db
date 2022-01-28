using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Remote
{
	public interface ILinqService
	{
		LinqServiceInfo GetInfo        (string? configuration);
		int             ExecuteNonQuery(string? configuration, string queryData);
		string?         ExecuteScalar  (string? configuration, string queryData);
		string          ExecuteReader  (string? configuration, string queryData);
		int             ExecuteBatch   (string? configuration, string queryData);



		Task<int> ExecuteNonQueryAsync(
			string? configuration,
			string queryData,
			CancellationToken cancellationToken
			);


		Task<string> ExecuteReaderAsync(
			string? configuration,
			string queryData,
			CancellationToken cancellationToken
			);

		Task<string?> ExecuteScalarAsync(
			string? configuration,
			string queryData,
			CancellationToken cancellationToken
			);
	}
}
