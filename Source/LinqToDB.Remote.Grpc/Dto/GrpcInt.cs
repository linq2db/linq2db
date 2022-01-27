using System.Runtime.Serialization;

namespace LinqToDB.Remote.Grpc.Dto
{
	[DataContract]
	public class GrpcInt
	{
		[DataMember(Order = 1)]
		public int Value { get; set; }

		public static implicit operator int(GrpcInt a) => a.Value;

		public static implicit operator GrpcInt(int a) => new GrpcInt { Value = a };
	}

}
