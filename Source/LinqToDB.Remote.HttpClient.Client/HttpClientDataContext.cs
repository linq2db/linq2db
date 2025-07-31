using System;

namespace LinqToDB.Remote.HttpClient.Client
{
	/// <summary>
	/// Remote data context implementation over HttpClient.
	/// </summary>
	public class HttpClientDataContext : RemoteDataContextBase
	{
		protected HttpClientLinqServiceClient Client;

		#region Init

		static readonly DataOptions _defaultDataOptions = new();

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="optionBuilder"></param>
		public HttpClientDataContext(HttpClientLinqServiceClient client, Func<DataOptions,DataOptions>? optionBuilder = null)
			: base(optionBuilder == null ? _defaultDataOptions : optionBuilder(_defaultDataOptions))
		{
			Client = client;
		}

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="httpClient">HttpClient</param>
		/// <param name="requestUri"></param>
		/// <param name="optionBuilder"></param>
		public HttpClientDataContext(System.Net.Http.HttpClient httpClient, string requestUri, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(new HttpClientLinqServiceClient(httpClient, requestUri), optionBuilder)
		{
		}

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="baseAddress">Server baseAddress.</param>
		/// <param name="requestUri"></param>
		/// <param name="optionBuilder"></param>
		public HttpClientDataContext(Uri baseAddress, string requestUri, Func<DataOptions,DataOptions>? optionBuilder = null)
#pragma warning disable CA2000 // Dispose objects before losing scope
			: this(new System.Net.Http.HttpClient() { BaseAddress = baseAddress }, requestUri, optionBuilder)
#pragma warning restore CA2000 // Dispose objects before losing scope
		{
		}

		#endregion

		#region Overrides

		protected override ILinqService GetClient()
		{
			return Client;
		}

		protected override string ContextIDPrefix => "HttpRemoteLinqService";

		#endregion
	}
}
