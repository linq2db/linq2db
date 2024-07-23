using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace LinqToDB.Remote.Wcf
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class WcfLinqService : IWcfLinqService
	{
		private readonly ILinqService _linqService;
		private readonly bool         _transferInternalExceptionToClient;

		public WcfLinqService(
			ILinqService linqService,
			bool transferInternalExceptionToClient
			)
		{
			_linqService = linqService ?? throw new ArgumentNullException(nameof(linqService));
			_transferInternalExceptionToClient = transferInternalExceptionToClient;
		}

		LinqServiceInfo IWcfLinqService.GetInfo(string? configuration)
		{
			try
			{
				return _linqService.GetInfo(configuration);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		int IWcfLinqService.ExecuteBatch(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteBatch(configuration, queryData);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		int IWcfLinqService.ExecuteNonQuery(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteNonQuery(configuration, queryData);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		string IWcfLinqService.ExecuteReader(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteReader(configuration, queryData);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		string? IWcfLinqService.ExecuteScalar(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteScalar(configuration, queryData);
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		async Task<LinqServiceInfo> IWcfLinqService.GetInfoAsync(string? configuration)
		{
			try
			{
				return await _linqService.GetInfoAsync(configuration)
					.ConfigureAwait(false); ;
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		async Task<int> IWcfLinqService.ExecuteBatchAsync(string? configuration, string queryData)
		{
			try
			{
				return await _linqService.ExecuteBatchAsync(configuration, queryData)
					.ConfigureAwait(false); ;
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		async Task<int> IWcfLinqService.ExecuteNonQueryAsync(string? configuration, string queryData)
		{
			try
			{
				return await _linqService.ExecuteNonQueryAsync(configuration, queryData)
					.ConfigureAwait(false); ;
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		async Task<string> IWcfLinqService.ExecuteReaderAsync(string? configuration, string queryData)
		{
			try
			{
				return await _linqService.ExecuteReaderAsync(configuration, queryData)
					.ConfigureAwait(false); ;
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		async Task<string?> IWcfLinqService.ExecuteScalarAsync(string? configuration, string queryData)
		{
			try
			{
				return await _linqService.ExecuteScalarAsync(configuration, queryData)
					.ConfigureAwait(false); ;
			}
			catch (Exception exception) when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}
	}
}
