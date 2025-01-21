using System;
using System.Net.Http;

namespace LinqToDB.Remote.Http.Client
{
	/// <summary>
	/// Remote data context implementation over HttpClient.
	/// </summary>
	public class HttpDataContext : RemoteDataContextBase
	{
		protected HttpLinqServiceClient Client;

		#region Init

		static readonly DataOptions _defaultDataOptions = new();

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="optionBuilder"></param>
		public HttpDataContext(HttpLinqServiceClient client, Func<DataOptions,DataOptions>? optionBuilder = null)
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
		public HttpDataContext(HttpClient httpClient, string requestUri, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(new HttpLinqServiceClient(httpClient, requestUri), optionBuilder)
		{
		}

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="baseAddress">Server baseAddress.</param>
		/// <param name="requestUri"></param>
		/// <param name="optionBuilder"></param>
		public HttpDataContext(Uri baseAddress, string requestUri, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(new HttpClient { BaseAddress = baseAddress}, requestUri, optionBuilder)
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
