#if NETFRAMEWORK
using System;
using System.Web.Services;
using LinqToDB.Remote.Independent;

namespace LinqToDB.Remote.Soap
{
	[WebService       (Namespace  = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class SoapLinqService : ISoapLinqService
	{
		private readonly ILinqService _linqService;

		public SoapLinqService(
			ILinqService linqService
			)
		{
			_linqService = linqService ?? throw new ArgumentNullException(nameof(linqService));
		}

		[WebMethod]
		public LinqServiceInfo GetInfo(string? configuration)
		{
			return _linqService.GetInfo(configuration);
		}

		[WebMethod]
		public int ExecuteBatch(string? configuration, string queryData)
		{
			return _linqService.ExecuteBatch(configuration, queryData);
		}

		[WebMethod]
		public int ExecuteNonQuery(string? configuration, string queryData)
		{
			return _linqService.ExecuteNonQuery(configuration, queryData);
		}

		[WebMethod]
		public string ExecuteReader(string? configuration, string queryData)
		{
			return _linqService.ExecuteReader(configuration, queryData);
		}

		[WebMethod]
		public object? ExecuteScalar(string? configuration, string queryData)
		{
			return _linqService.ExecuteScalar(configuration, queryData);
		}
	}
}
#endif
