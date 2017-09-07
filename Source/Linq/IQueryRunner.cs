using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	interface IQueryRunner: IDisposable
	{
		/// <summary>
		/// Executes query and returns number of affected records.
		/// </summary>
		/// <returns>Number of affected records.</returns>
		int                   ExecuteNonQuery();
		/// <summary>
		/// Executes query and returns scalar value.
		/// </summary>
		/// <returns>Scalar value.</returns>
		object                ExecuteScalar  ();
		/// <summary>
		/// Executes query and returns data reader.
		/// </summary>
		/// <returns>Data reader with query results.</returns>
		IDataReader           ExecuteReader  ();

#if !NOASYNC
		/// <summary>
		/// Executes query asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		Task<int>              ExecuteNonQueryAsync(CancellationToken cancellationToken);
		/// <summary>
		/// Executes query asynchronously and returns scalar value.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Scalar value.</returns>
		Task<object>           ExecuteScalarAsync  (CancellationToken cancellationToken);
		/// <summary>
		/// Executes query asynchronously and returns data reader.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Data reader with query results.</returns>
		Task<IDataReaderAsync> ExecuteReaderAsync  (CancellationToken cancellationToken);
#endif

		/// <summary>
		/// Returns SQL text for query.
		/// </summary>
		/// <returns>Query SQL text.</returns>
		string                GetSqlText     ();

		Func<int>      SkipAction       { get; set; }
		Func<int>      TakeAction       { get; set; }
		Expression     Expression       { get; set; }
		IDataContextEx DataContext      { get; set; }
		object[]       Parameters       { get; set; }
		Expression     MapperExpression { get; set; }
		int            RowsCount        { get; set; }
		int            QueryNumber      { get; set; }
	}
}
