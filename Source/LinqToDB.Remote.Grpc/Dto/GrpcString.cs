using System.Runtime.Serialization;

namespace LinqToDB.Remote.Grpc.Dto
{
	[DataContract]
	public class GrpcString
	{
		[DataMember(Order = 1)]
		public string? Value { get; set; }

		public static implicit operator string?(GrpcString a) => a.Value;

		public static implicit operator GrpcString(string? a) => new GrpcString { Value = a };
	}

}
