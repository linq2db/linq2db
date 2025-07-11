using LinqToDB.Data;

namespace LinqToDB.Remote
{
	public class LinqService<T>(IDataContextFactory<T> dataContextFactory) : LinqService, ILinqService<T>
		where T : IDataContext
	{
		public override DataConnection CreateDataContext(string? configuration)
		{
			var dc = dataContextFactory.CreateDataContext(configuration) as DataConnection ?? throw new LinqToDBException($"Type '{typeof(T)}' should implement 'DataConnection'.");

			dc.Tag = RemoteClientTag;

			return dc;
		}
	}
}
