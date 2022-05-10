using System.ServiceModel;
using LinqToDB.Remote.Grpc.Dto;
using ProtoBuf.Grpc;


namespace LinqToDB.Remote.Grpc;

/// <summary>
/// grpc-based remote context service contract.
/// </summary>
[ServiceContract]
public interface IGrpcLinqService
{
	[OperationContract(Name = "GetInfo")]
	LinqServiceInfo GetInfo(GrpcConfiguration configuration, CallContext context = default);

	[OperationContract(Name = "ExecuteNonQuery")]
	GrpcInt ExecuteNonQuery(GrpcConfigurationQuery caq, CallContext context = default);

	[OperationContract(Name = "ExecuteScalar")]
	GrpcString ExecuteScalar(GrpcConfigurationQuery caq, CallContext context = default);

	[OperationContract(Name = "ExecuteReader")]
	GrpcString ExecuteReader(GrpcConfigurationQuery caq, CallContext context = default);

	[OperationContract(Name = "ExecuteBatch")]
	GrpcInt ExecuteBatch(GrpcConfigurationQuery caq, CallContext context = default);

	[OperationContract(Name = "GetInfoAsync")]
	Task<LinqServiceInfo> GetInfoAsync(GrpcConfiguration configuration, CallContext context = default);

	[OperationContract(Name = "ExecuteNonQueryAsync")]
	Task<GrpcInt> ExecuteNonQueryAsync(GrpcConfigurationQuery caq, CallContext context = default);

	[OperationContract(Name = "ExecuteScalarAsync")]
	Task<GrpcString> ExecuteScalarAsync(GrpcConfigurationQuery caq, CallContext context = default);

	[OperationContract(Name = "ExecuteReaderAsync")]
	Task<GrpcString> ExecuteReaderAsync(GrpcConfigurationQuery caq, CallContext context = default);

	[OperationContract(Name = "ExecuteBatchAsync")]
	Task<GrpcInt> ExecuteBatchAsync(GrpcConfigurationQuery caq, CallContext context = default);
}
