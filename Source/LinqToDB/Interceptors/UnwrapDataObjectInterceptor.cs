using System.Data.Common;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Base class with pass-through implementations for <see cref="IUnwrapDataObjectInterceptor"/>.
	/// </summary>
	/// <remarks>
	/// Derive from this class when unwrapping only selected wrapped ADO.NET object types.
	/// For callback timing and return-value contracts, see <see cref="IUnwrapDataObjectInterceptor"/>.
	/// </remarks>
	public abstract class UnwrapDataObjectInterceptor : IUnwrapDataObjectInterceptor
	{
		/// <inheritdoc />
		public virtual DbConnection  UnwrapConnection (IDataContext dataContext, DbConnection  connection)  => connection;
		/// <inheritdoc />
		public virtual DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction) => transaction;
		/// <inheritdoc />
		public virtual DbCommand     UnwrapCommand    (IDataContext dataContext, DbCommand     command)     => command;
		/// <inheritdoc />
		public virtual DbDataReader  UnwrapDataReader (IDataContext dataContext, DbDataReader  dataReader)  => dataReader;
	}
}
