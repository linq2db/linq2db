#if NETFRAMEWORK
using System;
using System.ServiceModel;
using LinqToDB.Remote.Independent;

namespace LinqToDB.Remote.Wcf
{
	[ServiceBehavior  (InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class WcfLinqService : IWcfLinqService
	{
		private readonly ILinqService _linqService;

		public WcfLinqService(
			ILinqService linqService
			)
		{
			_linqService = linqService ?? throw new ArgumentNullException(nameof(linqService));
		}

		public LinqServiceInfo GetInfo(string? configuration)
		{
			return _linqService.GetInfo(configuration);
		}

		public int ExecuteBatch(string? configuration, string queryData)
		{
			return _linqService.ExecuteBatch(configuration, queryData);
		}

		public int ExecuteNonQuery(string? configuration, string queryData)
		{
			return _linqService.ExecuteNonQuery(configuration, queryData);
		}

		public string ExecuteReader(string? configuration, string queryData)
		{
			return _linqService.ExecuteReader(configuration, queryData);
		}

		public object? ExecuteScalar(string? configuration, string queryData)
		{
			return _linqService.ExecuteScalar(configuration, queryData);
		}
	}
}
#endif
