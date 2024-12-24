using System.ServiceModel;
using System.Threading.Tasks;

namespace LinqToDB.Remote.Wcf
{
	[ServiceContract]
	public interface IWcfLinqService
	{
		[OperationContract] LinqServiceInfo GetInfo        (string? configuration);
		[OperationContract] int             ExecuteNonQuery(string? configuration, string queryData);
		[OperationContract] string?         ExecuteScalar  (string? configuration, string queryData);
		[OperationContract] string          ExecuteReader  (string? configuration, string queryData);
		[OperationContract] int             ExecuteBatch   (string? configuration, string queryData);

		[OperationContract(Name = nameof(GetInfoAsync))        ] Task<LinqServiceInfo> GetInfoAsync        (string? configuration);
		[OperationContract(Name = nameof(ExecuteNonQueryAsync))] Task<int>             ExecuteNonQueryAsync(string? configuration, string queryData);
		[OperationContract(Name = nameof(ExecuteScalarAsync))  ] Task<string?>         ExecuteScalarAsync  (string? configuration, string queryData);
		[OperationContract(Name = nameof(ExecuteReaderAsync))  ] Task<string>          ExecuteReaderAsync  (string? configuration, string queryData);
		[OperationContract(Name = nameof(ExecuteBatchAsync))   ] Task<int>             ExecuteBatchAsync   (string? configuration, string queryData);
	}
}
