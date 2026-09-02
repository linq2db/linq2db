using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// Remote data context implementation over Signal/R.
	/// </summary>
	public class SignalRDataContext : RemoteDataContextBase
	{
		readonly SignalRLinqServiceClient _client;
		readonly HubConnection?           _ownedHubConnection;

		#region Init

		static readonly DataOptions _defaultDataOptions = new();

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="optionBuilder"></param>
		public SignalRDataContext(SignalRLinqServiceClient client, Func<DataOptions,DataOptions>? optionBuilder = null)
			: base(optionBuilder == null ? _defaultDataOptions : optionBuilder(_defaultDataOptions))
		{
			_client = client;
		}

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="optionBuilder"></param>
		public SignalRDataContext(HubConnection hubConnection, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(new SignalRLinqServiceClient(hubConnection), optionBuilder)
		{
			_ownedHubConnection = hubConnection;
		}

		#endregion

		#region Overrides

		protected override bool OwnsClient => false;

		protected override ILinqService GetClient()
		{
			return _client;
		}

		protected override string ContextIDPrefix => "SignalRRemoteLinqService";

		public override void Dispose()
		{
			base.Dispose();

			if (_ownedHubConnection != null)
				Task.Run(DisposeOwnedHubConnectionAsync).GetAwaiter().GetResult();
		}

		// HubConnection offers no synchronous disposal, and Task.Run keeps the wait off the caller's
		// synchronization context. A method rather than a lambda because DisposeAsync returns Task on
		// net462/netstandard2.0 and ValueTask from net8.0 on, so a lambda is reducible on the former only
		// (IDE0200) while being required on the latter.
		async Task DisposeOwnedHubConnectionAsync() => await _ownedHubConnection!.DisposeAsync().ConfigureAwait(false);

		public override async ValueTask DisposeAsync()
		{
			await base.DisposeAsync().ConfigureAwait(false);

			if (_ownedHubConnection != null)
				await _ownedHubConnection.DisposeAsync().ConfigureAwait(false);
		}

		#endregion
	}
}
