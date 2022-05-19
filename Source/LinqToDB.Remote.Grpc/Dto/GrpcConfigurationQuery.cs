using System.Runtime.Serialization;

namespace LinqToDB.Remote.Grpc.Dto;

/// <summary>
/// Query configuration data contract.
/// </summary>
[DataContract]
public class GrpcConfigurationQuery
{
	[DataMember(Order = 1)]
	public string? Configuration { get; set; }

	[DataMember(Order = 2)]
	public string QueryData { get; set; } = null!;
}
