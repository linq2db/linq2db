using ProtoBuf.Grpc.Configuration;

namespace LinqToDB.Remote.Grpc
{
	/// <summary>
	/// Compile-time generated gRPC client proxies and server bindings for <see cref="IGrpcLinqService"/>.
	/// </summary>
	/// <remarks>
	/// Without this the proxies and their payload marshallers are built by reflection, which does not survive
	/// trimming or Native AOT. A server host may keep using <c>AddCodeFirstGrpc()</c>, which registers the
	/// reflection-based binder; the two binders must not both be registered, or every operation binds twice.
	/// </remarks>
	[ProtoGrpc(Model = typeof(GrpcLinqServiceTypeModel))]
	[ProtoService(typeof(IGrpcLinqService), typeof(GrpcLinqService))]
	public sealed partial class GrpcLinqServiceProxies : ClientFactory;
}
