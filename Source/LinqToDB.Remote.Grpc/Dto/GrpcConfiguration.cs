using System.Runtime.Serialization;

namespace LinqToDB.Remote.Grpc.Dto
{
	[DataContract]
	public class GrpcConfiguration
	{
		[DataMember(Order = 1)]
		public string? Configuration
		{
			get; set;
		}
	}

}
