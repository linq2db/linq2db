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
	/// Internal because <see cref="GrpcLinqServiceClient"/> is the entry point consumers use, and the members
	/// the generator adds here would otherwise enter the declared public API - two of them without nullable
	/// annotations, since generated code carries none.
	/// </remarks>
	[ProtoGrpc(Model = typeof(GrpcLinqServiceTypeModel))]
	[ProtoService(typeof(IGrpcLinqService), typeof(GrpcLinqService))]
	internal sealed partial class GrpcLinqServiceProxies : ClientFactory;
}
