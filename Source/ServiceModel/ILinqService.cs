using System;
using System.ServiceModel;

namespace LinqToDB.ServiceModel
{
	[ServiceContract]
	public interface ILinqService
	{
		[OperationContract] string GetSqlProviderType();
		[OperationContract] int    ExecuteNonQuery(string queryData);
		[OperationContract] object ExecuteScalar  (string queryData);
		[OperationContract] string ExecuteReader  (string queryData);
		[OperationContract] int    ExecuteBatch   (string queryData);
	}
}
