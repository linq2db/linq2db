using System.ServiceModel;
using System.Threading.Tasks;
using LinqToDB.Remote.Grpc.Dto;
using ProtoBuf.Grpc;


namespace LinqToDB.Remote.Grpc
{
	[ServiceContract]
	public interface IGrpcLinqService
	{
		[OperationContract(Name = "GetInfo")]
		GrpcLinqServiceInfo GetInfo(GrpcConfiguration configuration, CallContext context = default);

		[OperationContract(Name = "ExecuteNonQuery")]
		GrpcInt ExecuteNonQuery(GrpcConfigurationQuery caq, CallContext context = default);

		[OperationContract(Name = "ExecuteScalar")]
		string? ExecuteScalar(GrpcConfigurationQuery caq, CallContext context = default);

		[OperationContract(Name = "ExecuteReader")]
		string ExecuteReader(GrpcConfigurationQuery caq, CallContext context = default);

		[OperationContract(Name = "ExecuteBatch")]
		GrpcInt ExecuteBatch(GrpcConfigurationQuery caq, CallContext context = default);





		[OperationContract(Name = "GetInfoAsync")]
		Task<GrpcLinqServiceInfo> GetInfoAsync(GrpcConfiguration configuration, CallContext context = default);

		[OperationContract(Name = "ExecuteNonQueryAsync")]
		Task<GrpcInt> ExecuteNonQueryAsync(GrpcConfigurationQuery caq, CallContext context = default);

		[OperationContract(Name = "ExecuteScalarAsync")]
		Task<string?> ExecuteScalarAsync(GrpcConfigurationQuery caq, CallContext context = default);

		[OperationContract(Name = "ExecuteReaderAsync")]
		Task<string> ExecuteReaderAsync(GrpcConfigurationQuery caq, CallContext context = default);

		[OperationContract(Name = "ExecuteBatchAsync")]
		Task<GrpcInt> ExecuteBatchAsync(GrpcConfigurationQuery caq, CallContext context = default);
	}

}
