using System.Globalization;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IL2026
#pragma warning disable IL3050

namespace LinqToDB.Remote.HttpClient.Client
{
	/// <summary>
	/// Http-base remote data context client.
	/// </summary>
	public class HttpClientLinqServiceClient(System.Net.Http.HttpClient httpClient, string requestUri) : ILinqService
	{
		public System.Net.Http.HttpClient HttpClient { get; } = httpClient;

		async Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsync($"{requestUri}/{nameof(ILinqService.GetInfoAsync)}/{configuration}", null, cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadFromJsonAsync<LinqServiceInfo>(cancellationToken : cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException("Return value is not allowed to be null");
		}

		async Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteNonQueryAsync)}/{configuration}", queryData, cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return int.Parse(await response.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
				cancellationToken
#endif
				).ConfigureAwait(false), CultureInfo.InvariantCulture);
		}

		async Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteScalarAsync)}/{configuration}", queryData, cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
				cancellationToken
#endif
				).ConfigureAwait(false);
		}

		async Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteReaderAsync)}/{configuration}", queryData, cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
				cancellationToken
#endif
				).ConfigureAwait(false);
		}

		async Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			var response = await HttpClient.PostAsJsonAsync($"{requestUri}/{nameof(ILinqService.ExecuteBatchAsync)}/{configuration}", queryData, cancellationToken).ConfigureAwait(false)
				?? throw new LinqToDBException("Return value is not allowed to be null");

			response.EnsureSuccessStatusCode();

			return int.Parse(await response.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
				cancellationToken
#endif
				).ConfigureAwait(false), CultureInfo.InvariantCulture);
		}

		string? ILinqService.RemoteClientTag { get; set; } = "HttpClient";
	}
}
