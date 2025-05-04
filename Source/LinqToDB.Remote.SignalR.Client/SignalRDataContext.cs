﻿using System;
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
		readonly bool                     _dispose;

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
			_client  = client;
			_dispose = false;
		}

		/// <summary>
		/// Creates instance of http-based remote data context.
		/// </summary>
		/// <param name="optionBuilder"></param>
		public SignalRDataContext(HubConnection hubConnection, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(new SignalRLinqServiceClient(hubConnection), optionBuilder)
		{
			_dispose = true;
		}

		#endregion

		#region Overrides

		protected override ILinqService GetClient()
		{
			return _client;
		}

		protected override string ContextIDPrefix => "SignalRRemoteLinqService";

		public override async ValueTask DisposeAsync()
		{
			if (_dispose)
				await _client.DisposeAsync().ConfigureAwait(false);
			await base.DisposeAsync().ConfigureAwait(false);
		}

		#endregion
	}
}
