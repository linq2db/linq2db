using System;

namespace LinqToDB.Configuration
{
	public interface IConnectionStringSettings
	{
		string ConnectionString { get; }
		string Name { get; }
		string ProviderName { get; }
		bool IsGlobal { get; }
	}
}