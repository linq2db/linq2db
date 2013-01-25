using System;
using System.ServiceModel;

namespace LinqToDB.ServiceModel
{
	[ServiceContract]
	public interface ILinqSoapService
	{
		[OperationContract] LinqServiceInfo GetInfo        (string configuration);
		[OperationContract] int             ExecuteNonQuery(string configuration, string queryData);
		[OperationContract] object          ExecuteScalar  (string configuration, string queryData);
		[OperationContract] string          ExecuteReader  (string configuration, string queryData);
		[OperationContract] int             ExecuteBatch   (string configuration, string queryData);
	}
}
