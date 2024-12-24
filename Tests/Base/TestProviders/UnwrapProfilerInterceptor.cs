using System.Data.Common;

using LinqToDB;
using LinqToDB.Interceptors;

using StackExchange.Profiling.Data;

namespace Tests
{
	public class UnwrapProfilerInterceptor : UnwrapDataObjectInterceptor
	{
		public static readonly UnwrapDataObjectInterceptor Instance = new UnwrapProfilerInterceptor();

		private UnwrapProfilerInterceptor()
		{
		}

		public override DbConnection UnwrapConnection(IDataContext dataContext, DbConnection connection)
		{
			return connection is ProfiledDbConnection c ? c.WrappedConnection : connection;
		}

		public override DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction)
		{
			return transaction is ProfiledDbTransaction t ? t.WrappedTransaction : transaction;
		}

		public override DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command)
		{
			return command is ProfiledDbCommand c ? c.WrappedCommand : command;
		}

		public override DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
		{
			return dataReader is ProfiledDbDataReader dr ? dr.WrappedReader : dataReader;
		}
	}
}
