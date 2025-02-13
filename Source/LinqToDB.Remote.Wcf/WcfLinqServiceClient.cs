using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Remote.Wcf
{
	sealed class WcfLinqServiceClient : ClientBase<IWcfLinqService>, ILinqService
	{
		public WcfLinqServiceClient(string endpointConfigurationName)
			: base(endpointConfigurationName)
		{ }

		public WcfLinqServiceClient(string endpointConfigurationName, string remoteAddress)
			: base(endpointConfigurationName, remoteAddress)
		{ }

		public WcfLinqServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress)
			: base(endpointConfigurationName, remoteAddress)
		{ }

		public WcfLinqServiceClient(Binding binding, EndpointAddress remoteAddress)
			: base(binding, remoteAddress)
		{ }

		#region ILinqService Members

		LinqServiceInfo ILinqService.GetInfo(string? configuration)
		{
			return Channel.GetInfo(configuration);
		}

		int ILinqService.ExecuteNonQuery(string? configuration, string queryData)
		{
			return Channel.ExecuteNonQuery(configuration, queryData);
		}

		string? ILinqService.ExecuteScalar(string? configuration, string queryData)
		{
			return Channel.ExecuteScalar(configuration, queryData);
		}

		string ILinqService.ExecuteReader(string? configuration, string queryData)
		{
			return Channel.ExecuteReader(configuration, queryData);
		}

		int ILinqService.ExecuteBatch(string? configuration, string queryData)
		{
			return Channel.ExecuteBatch(configuration, queryData);
		}

		Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			return Channel.GetInfoAsync(configuration);
		}

		Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Channel.ExecuteNonQueryAsync(configuration, queryData);
		}

		Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Channel.ExecuteScalarAsync(configuration, queryData);
		}

		Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Channel.ExecuteReaderAsync(configuration, queryData);
		}

		Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Channel.ExecuteBatchAsync(configuration, queryData);
		}

		string? ILinqService.RemoteClientTag { get; set; } = "Wсf";

		#endregion
	}
}
