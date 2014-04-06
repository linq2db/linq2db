using System;
using System.ServiceModel;

namespace LinqToDB.ServiceModel.Async
{
	[ServiceContract]
	[ServiceKnownType(typeof(LinqServiceQuery))]
	[ServiceKnownType(typeof(LinqServiceResult))]
	public interface ILinqService
	{
		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ILinqService/GetInfo", ReplyAction = "http://tempuri.org/ILinqService/GetSqlInfoResponse")]
		IAsyncResult BeginGetInfo(string configuration, AsyncCallback callback, object asyncState);

		LinqServiceInfo EndGetInfo(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ILinqService/ExecuteNonQuery", ReplyAction = "http://tempuri.org/ILinqService/ExecuteNonQueryResponse")]
		IAsyncResult BeginExecuteNonQuery(string configuration, string queryData, AsyncCallback callback, object asyncState);

		int EndExecuteNonQuery(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ILinqService/ExecuteScalar", ReplyAction = "http://tempuri.org/ILinqService/ExecuteScalarResponse")]
		IAsyncResult BeginExecuteScalar(string configuration, string queryData, AsyncCallback callback, object asyncState);

		object EndExecuteScalar(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ILinqService/ExecuteReader", ReplyAction = "http://tempuri.org/ILinqService/ExecuteReaderResponse")]
		IAsyncResult BeginExecuteReader(string configuration, string queryData, AsyncCallback callback, object asyncState);

		string EndExecuteReader(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ILinqService/ExecuteBatch", ReplyAction = "http://tempuri.org/ILinqService/ExecuteBatchResponse")]
		IAsyncResult BeginExecuteBatch(string configuration, string queryData, AsyncCallback callback, object asyncState);

		int EndExecuteBatch(IAsyncResult result);
	}
}
