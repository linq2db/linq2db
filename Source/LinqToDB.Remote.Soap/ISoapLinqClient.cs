#if NETFRAMEWORK
using System.ServiceModel;
using System.Threading.Tasks;

namespace LinqToDB.Remote.Soap
{
	[ServiceContract]
	[ServiceKnownType(typeof(LinqServiceQuery))]
	[ServiceKnownType(typeof(LinqServiceResult))]
	public interface ISoapLinqClient
	{
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/GetInfo",         ReplyAction= "http://tempuri.org/ISoapLinqService/GetInfoResponse")]
		LinqServiceInfo GetInfo        (string? configuration);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteNonQuery", ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteNonQueryResponse")]
		int             ExecuteNonQuery(string? configuration, string queryData);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteScalar",   ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteScalarResponse")]
		string?         ExecuteScalar  (string? configuration, string queryData);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteReader",   ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteReaderResponse")]
		string          ExecuteReader  (string? configuration, string queryData);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteBatch",    ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteBatchResponse")]
		int             ExecuteBatch   (string? configuration, string queryData);



		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/GetInfo",         ReplyAction= "http://tempuri.org/ISoapLinqService/GetInfoResponse")]
		Task<LinqServiceInfo> GetInfoAsync        (string? configuration);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteNonQuery", ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteNonQueryResponse")]
		Task<int>             ExecuteNonQueryAsync(string? configuration, string queryData);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteScalar",   ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteScalarResponse")]
		Task<string?>         ExecuteScalarAsync  (string? configuration, string queryData);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteReader",   ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteReaderResponse")]
		Task<string>          ExecuteReaderAsync  (string? configuration, string queryData);
		
		[OperationContract(Action= "http://tempuri.org/ISoapLinqService/ExecuteBatch",    ReplyAction= "http://tempuri.org/ISoapLinqService/ExecuteBatchResponse")]
		Task<int>             ExecuteBatchAsync   (string? configuration, string queryData);
	}
}
#endif
