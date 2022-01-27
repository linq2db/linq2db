using LinqToDB.Configuration;

namespace LinqToDB.Remote.Grpc
{
	public class ConnectionStringSettings : IConnectionStringSettings
	{
		public string ConnectionString
		{
			get; set;
		} = null!;
		public string Name
		{
			get; set;
		} = null!;
		public string ProviderName
		{
			get; set;
		} = null!;

		public bool IsGlobal => false;
	}

}
