using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Remote.Http.Client
{
	/// <summary>
	/// Http-base remote data context client.
	/// </summary>
	public class HttpLinqServiceClient(HttpClient httpClient, string requestUri) : ILinqService
	{
		public HttpClient HttpClient { get; } = httpClient;

		LinqServiceInfo ILinqService.GetInfo(string? configuration)
		{
			var response = HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.GetInfo)}", configuration, default).Result
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return response.Content.ReadFromJsonAsync<LinqServiceInfo>().Result
				?? throw new LinqToDBException("Return value is not allowed to be null");
		}

		int ILinqService.ExecuteNonQuery(string? configuration, string queryData)
		{
			var response = HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteNonQuery)}/{configuration}", queryData).Result
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return int.Parse(response.Content.ReadAsStringAsync(default).Result, CultureInfo.InvariantCulture);
		}

		string ILinqService.ExecuteScalar(string? configuration, string queryData)
		{
			var response = HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteScalar)}/{configuration}", queryData).Result
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return response.Content.ReadAsStringAsync(default).Result;
		}

		string ILinqService.ExecuteReader(string? configuration, string queryData)
		{
			var response = HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteReader)}/{configuration}", queryData).Result
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return response.Content.ReadAsStringAsync(default).Result;
		}

		int ILinqService.ExecuteBatch(string? configuration, string queryData)
		{
			var response = HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteBatch)}/{configuration}", queryData).Result
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return int.Parse(response.Content.ReadAsStringAsync(default).Result, CultureInfo.InvariantCulture);
		}

		async Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.GetInfoAsync)}", configuration, cancellationToken)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadFromJsonAsync<LinqServiceInfo>(cancellationToken)
				?? throw new LinqToDBException("Return value is not allowed to be null");
		}

		async Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteNonQueryAsync)}/{configuration}", queryData, cancellationToken)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return int.Parse(await response.Content.ReadAsStringAsync(cancellationToken), CultureInfo.InvariantCulture);
		}

		async Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteScalarAsync)}/{configuration}", queryData, cancellationToken)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync(cancellationToken);
		}

		async Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteReaderAsync)}/{configuration}", queryData, cancellationToken)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync(cancellationToken);
		}

		async Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteNonQueryAsync)}/{configuration}", queryData, cancellationToken)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return int.Parse(await response.Content.ReadAsStringAsync(cancellationToken), CultureInfo.InvariantCulture);
		}
	}
}
