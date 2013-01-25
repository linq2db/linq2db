using System;

namespace LinqToDB.ServiceModel
{
	using SqlProvider;

	public class LinqServiceInfo
	{
		public string           SqlProviderType   { get; set; }
		public SqlProviderFlags SqlProviderFlags  { get; set; }
		public string[]         ConfigurationList { get; set; }
	}
}
