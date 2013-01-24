using System;
using System.ServiceModel;

namespace LinqToDB.ServiceModel
{
	using SqlProvider;

	[ServiceContract]
	public interface ILinqSoapService
	{
		[OperationContract] string           GetSqlProviderType();
		[OperationContract] SqlProviderFlags GetSqlProviderFlags();
		[OperationContract] int              ExecuteNonQuery(string queryData);
		[OperationContract] object           ExecuteScalar  (string queryData);
		[OperationContract] string           ExecuteReader  (string queryData);
		[OperationContract] int              ExecuteBatch   (string queryData);
	}
}
