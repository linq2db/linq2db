namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// The compiled query that built a <see cref="Query{T}"/>.
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
