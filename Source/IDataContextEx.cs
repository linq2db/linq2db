using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Linq;

	/// <summary>
	/// Internal data context functionality that shouldn't be exposed to users.
	/// </summary>
	interface IDataContextEx : IDataContext
	{
		/// <summary>
		/// Returns query runner service for current context.
		/// </summary>
		/// <param name="query">Query batch object.</param>
		/// <param name="queryNumber">Index of query in query batch.</param>
		/// <param name="expression">Query results mapping expression.</param>
		/// <param name="parameters">Query parameters.</param>
		/// <returns>Query runner service.</returns>
		IQueryRunner GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters);
	}
}
