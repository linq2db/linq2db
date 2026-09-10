using ProtoBuf;
using ProtoBuf.Meta;

namespace LinqToDB.Remote.Grpc
{
	/// <summary>
	/// Compile-time serialization model for the types exchanged by <see cref="IGrpcLinqService"/>.
	/// </summary>
	[ProtoModel]
	public partial class GrpcLinqServiceTypeModel : TypeModel;
}
