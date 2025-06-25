﻿using System.Threading.Tasks;

using LinqToDB.Remote.Grpc.Dto;

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace LinqToDB.Remote.Grpc
{
	/// <summary>
	/// GRPC-based remote context service contract.
	/// </summary>
	[Service]
	public interface IGrpcLinqService
	{
		[Operation("GetInfo")]
		LinqServiceInfo GetInfo(GrpcConfiguration configuration, CallContext context = default);

		[Operation("ExecuteNonQuery")]
		GrpcInt ExecuteNonQuery(GrpcConfigurationQuery caq, CallContext context = default);

		[Operation("ExecuteScalar")]
		GrpcString ExecuteScalar(GrpcConfigurationQuery caq, CallContext context = default);

		[Operation("ExecuteReader")]
		GrpcString ExecuteReader(GrpcConfigurationQuery caq, CallContext context = default);

		[Operation("ExecuteBatch")]
		GrpcInt ExecuteBatch(GrpcConfigurationQuery caq, CallContext context = default);

		[Operation("GetInfoAsync")]
		Task<LinqServiceInfo> GetInfoAsync(GrpcConfiguration configuration, CallContext context = default);

		[Operation("ExecuteNonQueryAsync")]
		Task<GrpcInt> ExecuteNonQueryAsync(GrpcConfigurationQuery caq, CallContext context = default);

		[Operation("ExecuteScalarAsync")]
		Task<GrpcString> ExecuteScalarAsync(GrpcConfigurationQuery caq, CallContext context = default);

		[Operation("ExecuteReaderAsync")]
		Task<GrpcString> ExecuteReaderAsync(GrpcConfigurationQuery caq, CallContext context = default);

		[Operation("ExecuteBatchAsync")]
		Task<GrpcInt> ExecuteBatchAsync(GrpcConfigurationQuery caq, CallContext context = default);
	}
}
