using System.Data.Common;

namespace LinqToDB.Interceptors
{
	public abstract class UnwrapDataObjectInterceptor : IUnwrapDataObjectInterceptor
	{
		public virtual DbConnection  UnwrapConnection (IDataContext dataContext, DbConnection  connection)  => connection;
		public virtual DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction) => transaction;
		public virtual DbCommand     UnwrapCommand    (IDataContext dataContext, DbCommand     command)     => command;
		public virtual DbDataReader  UnwrapDataReader (IDataContext dataContext, DbDataReader  dataReader)  => dataReader;
	}
}
