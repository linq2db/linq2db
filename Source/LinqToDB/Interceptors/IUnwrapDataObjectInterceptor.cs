using System.Data.Common;
namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Unwraps wrapped ADO.NET objects before LinqToDB uses them internally.
	/// </summary>
	/// <remarks>
	/// Use when an external library wraps <see cref="DbConnection"/>, <see cref="DbTransaction"/>, <see cref="DbCommand"/>,
	/// or <see cref="DbDataReader"/> and LinqToDB must access the underlying provider objects.
	/// This is commonly needed for profiler or instrumentation wrappers.
	/// </remarks>
	public interface IUnwrapDataObjectInterceptor : IInterceptor
	{
		DbConnection  UnwrapConnection (IDataContext dataContext, DbConnection  connection);
		DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction);
		DbCommand     UnwrapCommand    (IDataContext dataContext, DbCommand     command);
		DbDataReader  UnwrapDataReader (IDataContext dataContext, DbDataReader  dataReader);
	}
}