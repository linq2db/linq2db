using System.Data.Common;

namespace LinqToDB.Interceptors
{
	public interface IUnwrapDataObjectInterceptor : IInterceptor
	{
		DbConnection  UnwrapConnection (IDataContext dataContext, DbConnection  connection);
		DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction);
		DbCommand     UnwrapCommand    (IDataContext dataContext, DbCommand     command);
		DbDataReader  UnwrapDataReader (IDataContext dataContext, DbDataReader  dataReader);
	}
}
