using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace LinqToDB.ServiceModel
{
	[ServiceContract]
	[ServiceKnownType(typeof(LinqServiceQuery))]
	[ServiceKnownType(typeof(LinqServiceResult))]
	public interface ILinqClient
	{
		[OperationContract(Action="http://tempuri.org/ILinqService/GetInfo",         ReplyAction="http://tempuri.org/ILinqService/GetInfoResponse")]         LinqServiceInfo GetInfo        (string configuration);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteNonQuery", ReplyAction="http://tempuri.org/ILinqService/ExecuteNonQueryResponse")] int             ExecuteNonQuery(string configuration, string queryData);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteScalar",   ReplyAction="http://tempuri.org/ILinqService/ExecuteScalarResponse")]   object          ExecuteScalar  (string configuration, string queryData);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteReader",   ReplyAction="http://tempuri.org/ILinqService/ExecuteReaderResponse")]   string          ExecuteReader  (string configuration, string queryData);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteBatch",    ReplyAction="http://tempuri.org/ILinqService/ExecuteBatchResponse")]    int             ExecuteBatch   (string configuration, string queryData);

		[OperationContract(Action="http://tempuri.org/ILinqService/GetInfo",         ReplyAction="http://tempuri.org/ILinqService/GetInfoResponse")]         Task<LinqServiceInfo> GetInfoAsync        (string configuration);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteNonQuery", ReplyAction="http://tempuri.org/ILinqService/ExecuteNonQueryResponse")] Task<int>             ExecuteNonQueryAsync(string configuration, string queryData);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteScalar",   ReplyAction="http://tempuri.org/ILinqService/ExecuteScalarResponse")]   Task<object>          ExecuteScalarAsync  (string configuration, string queryData);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteReader",   ReplyAction="http://tempuri.org/ILinqService/ExecuteReaderResponse")]   Task<string>          ExecuteReaderAsync  (string configuration, string queryData);
		[OperationContract(Action="http://tempuri.org/ILinqService/ExecuteBatch",    ReplyAction="http://tempuri.org/ILinqService/ExecuteBatchResponse")]    Task<int>             ExecuteBatchAsync   (string configuration, string queryData);
	}
}
