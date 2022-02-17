#if NETFRAMEWORK
using System;
using System.ServiceModel;

namespace LinqToDB.Remote.WCF
{
	[ServiceBehavior  (InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class WcfLinqService : IWcfLinqService
	{
		private readonly ILinqService _linqService;
		private readonly bool _transferInternalExceptionToClient;

		public WcfLinqService(
			ILinqService linqService,
			bool transferInternalExceptionToClient
			)
		{
			_linqService = linqService ?? throw new ArgumentNullException(nameof(linqService));
			_transferInternalExceptionToClient = transferInternalExceptionToClient;
		}

		public LinqServiceInfo GetInfo(string? configuration)
		{
			try
			{
				return _linqService.GetInfo(configuration);
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		public int ExecuteBatch(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteBatch(configuration, queryData);
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		public int ExecuteNonQuery(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteNonQuery(configuration, queryData);
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		public string ExecuteReader(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteReader(configuration, queryData);
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}

		public object? ExecuteScalar(string? configuration, string queryData)
		{
			try
			{
				return _linqService.ExecuteScalar(configuration, queryData);
			}
			catch (Exception exception)
			when (_transferInternalExceptionToClient)
			{
				throw new FaultException(exception.ToString());
			}
		}
	}
}
#endif
