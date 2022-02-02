#if NETFRAMEWORK
using System.ServiceModel;

namespace LinqToDB.Remote.WCF
{
	[ServiceContract]
	public interface IWcfLinqService
	{
		[OperationContract] LinqServiceInfo GetInfo        (string? configuration);
		[OperationContract] int             ExecuteNonQuery(string? configuration, string queryData);
		[OperationContract] object?         ExecuteScalar  (string? configuration, string queryData);
		[OperationContract] string          ExecuteReader  (string? configuration, string queryData);
		[OperationContract] int             ExecuteBatch   (string? configuration, string queryData);
	}
}
#endif
