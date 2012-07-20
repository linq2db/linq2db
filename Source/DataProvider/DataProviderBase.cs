using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	public abstract class DataProviderBase : IDataProvider
	{
		public abstract string Name         { get; }
		public abstract string ProviderName { get; }

		public abstract IDbConnection CreateConnection(string connectionString);

		public virtual void Configure(string name, string value)
		{
		}
	}
}
