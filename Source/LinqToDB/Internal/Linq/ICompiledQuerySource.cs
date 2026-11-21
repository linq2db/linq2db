namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// The compiled query an <see cref="ExpressionQuery{T}"/> was returned from.
	/// </summary>
	interface ICompiledQuerySource<T>
	{
		/// <summary>
		/// Whether an argument selects the query by value (<see cref="LinqToDB.Mapping.SqlQueryDependentAttribute"/>), which the caller may
		/// change in place after the call.
		/// </summary>
		bool HasDependentArguments { get; }

		/// <summary>
		/// Resolves the query for <paramref name="dataContext"/> and <paramref name="parameterValues"/>, as a call of the
		/// compiled query does.
		/// </summary>
		Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues, out IQueryExpressions expressions);
	}
}
