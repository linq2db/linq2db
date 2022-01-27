#if NETFRAMEWORK
using System.ServiceModel;
using System.Threading.Tasks;

namespace LinqToDB.Remote.Wcf
{
	[ServiceContract]
	[ServiceKnownType(typeof(LinqServiceQuery))]
	[ServiceKnownType(typeof(LinqServiceResult))]
	public interface IWcfLinqClient
	{
		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/GetInfo",         ReplyAction= "http://tempuri.org/IWcfLinqService/GetInfoResponse")]
		LinqServiceInfo GetInfo        (string? configuration);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteNonQuery", ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteNonQueryResponse")]
		int             ExecuteNonQuery(string? configuration, string queryData);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteScalar",   ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteScalarResponse")] 
		string?         ExecuteScalar  (string? configuration, string queryData);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteReader",   ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteReaderResponse")]
		string          ExecuteReader  (string? configuration, string queryData);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteBatch",    ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteBatchResponse")]
		int             ExecuteBatch   (string? configuration, string queryData);




		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/GetInfo",         ReplyAction= "http://tempuri.org/IWcfLinqService/GetInfoResponse")]
		Task<LinqServiceInfo> GetInfoAsync(string? configuration);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteNonQuery", ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteNonQueryResponse")]
		Task<int> ExecuteNonQueryAsync(string? configuration, string queryData);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteScalar",   ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteScalarResponse")]
		Task<string?> ExecuteScalarAsync(string? configuration, string queryData);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteReader",   ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteReaderResponse")]
		Task<string> ExecuteReaderAsync(string? configuration, string queryData);

		[OperationContract(Action= "http://tempuri.org/IWcfLinqService/ExecuteBatch",    ReplyAction= "http://tempuri.org/IWcfLinqService/ExecuteBatchResponse")]
		Task<int> ExecuteBatchAsync(string? configuration, string queryData);
	}
}
#endif
