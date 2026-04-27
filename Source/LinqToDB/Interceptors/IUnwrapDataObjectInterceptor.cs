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
		/// <summary>
		/// Called when LinqToDB needs the provider connection represented by a wrapped connection.
		/// </summary>
		/// <param name="dataContext">Data context that owns the operation.</param>
		/// <param name="connection">Connection instance to inspect or unwrap.</param>
		/// <returns>Connection instance LinqToDB should use.</returns>
		DbConnection  UnwrapConnection (IDataContext dataContext, DbConnection  connection);
		/// <summary>
		/// Called when LinqToDB needs the provider transaction represented by a wrapped transaction.
		/// </summary>
		/// <param name="dataContext">Data context that owns the operation.</param>
		/// <param name="transaction">Transaction instance to inspect or unwrap.</param>
		/// <returns>Transaction instance LinqToDB should use.</returns>
		DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction);
		/// <summary>
		/// Called when LinqToDB needs the provider command represented by a wrapped command.
		/// </summary>
		/// <param name="dataContext">Data context that owns the operation.</param>
		/// <param name="command">Command instance to inspect or unwrap.</param>
		/// <returns>Command instance LinqToDB should use.</returns>
		DbCommand     UnwrapCommand    (IDataContext dataContext, DbCommand     command);
		/// <summary>
		/// Called when LinqToDB needs the provider data reader represented by a wrapped data reader.
		/// </summary>
		/// <param name="dataContext">Data context that owns the operation.</param>
		/// <param name="dataReader">Data reader instance to inspect or unwrap.</param>
		/// <returns>Data reader instance LinqToDB should use.</returns>
		DbDataReader  UnwrapDataReader (IDataContext dataContext, DbDataReader  dataReader);
	}
}
