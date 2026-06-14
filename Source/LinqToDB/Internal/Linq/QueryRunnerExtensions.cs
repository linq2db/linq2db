namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Internal helpers around <see cref="IQueryRunner"/> construction. The execContext-aware
	/// overload of <c>GetQueryRunner</c> attaches the per-execute <see cref="QueryExecutionContext"/>
	/// to the runner so its <c>SetCommand</c> can fire any temp-table run-steps through the shared
	/// context (preamble + main query coordination — see <see cref="QueryExecutionContext"/>).
	/// </summary>
	static class QueryRunnerExtensions
	{
		public static IQueryRunner GetQueryRunner(
			this IDataContext         dc,
			Query                     query,
			IDataContext              parametersContext,
			int                       queryNumber,
			IQueryExpressions         expressions,
			object?[]?                parameters,
			object?[]?                preambles,
			QueryExecutionContext?    execContext)
		{
			var runner = dc.GetQueryRunner(query, parametersContext, queryNumber, expressions, parameters, preambles);
			if (execContext != null && runner is IExecutionContextAwareRunner aware)
				aware.ExecutionContext = execContext;
			return runner;
		}
	}
}
