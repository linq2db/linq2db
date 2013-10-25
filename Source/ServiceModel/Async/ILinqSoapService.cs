using System;
using System.ServiceModel;

namespace LinqToDB.ServiceModel.Async
{
	[ServiceContract]
	[ServiceKnownType(typeof(LinqServiceQuery))]
	[ServiceKnownType(typeof(LinqServiceResult))]
	public interface ILinqSoapService
	{
		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/GetInfo", ReplyAction = "http://tempuri.org/GetInfo")]
		IAsyncResult BeginGetInfo(string configuration, AsyncCallback callback, object asyncState);

		LinqServiceInfo EndGetInfo(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ExecuteNonQuery", ReplyAction = "http://tempuri.org/ExecuteNonQueryResponse")]
		IAsyncResult BeginExecuteNonQuery(string configuration, string queryData, AsyncCallback callback, object asyncState);

		int EndExecuteNonQuery(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ExecuteScalar", ReplyAction = "http://tempuri.org/ExecuteScalarResponse")]
		IAsyncResult BeginExecuteScalar(string configuration, string queryData, AsyncCallback callback, object asyncState);

		object EndExecuteScalar(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ExecuteReader", ReplyAction = "http://tempuri.org/ExecuteReaderResponse")]
		IAsyncResult BeginExecuteReader(string configuration, string queryData, AsyncCallback callback, object asyncState);

		string EndExecuteReader(IAsyncResult result);

		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/ExecuteBatch", ReplyAction = "http://tempuri.org/ExecuteBatchResponse")]
		IAsyncResult BeginExecuteBatch(string configuration, string queryData, AsyncCallback callback, object asyncState);

		int EndExecuteBatch(IAsyncResult result);
	}
}
