using System.Linq.Expressions;

namespace LinqToDB.Async;

/// <summary>
/// This is internal API and is not intended for use by Linq To DB applications.
/// It may change or be removed without further notice.
/// </summary>
public interface IQueryProviderAsync : IQueryProvider
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken);

	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);
}
