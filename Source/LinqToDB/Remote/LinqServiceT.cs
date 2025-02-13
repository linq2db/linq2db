using System;

using LinqToDB.Data;

namespace LinqToDB.Remote
{
	public class LinqService<T>(IDataContextFactory<T> dataContextFactory) : LinqService
		where T : IDataContext
	{
		public override DataConnection CreateDataContext(string? configuration)
		{
			return dataContextFactory.CreateDataContext(configuration) as DataConnection ?? throw new LinqToDBException($"Type '{typeof(T)}' should implement 'DataConnection'.");
		}
	}
}
