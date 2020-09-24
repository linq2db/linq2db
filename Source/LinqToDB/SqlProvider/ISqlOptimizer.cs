namespace LinqToDB.SqlProvider
{
	using SqlQuery;
	using Mapping;

	public interface ISqlOptimizer
	{
		/// <summary>
		/// Finalizes query.
		/// </summary>
		/// <param name="statement"></param>
		/// <returns>Query which is ready for optimization.</returns>
		SqlStatement Finalize          (SqlStatement statement);

		/// <summary>
		/// Optimizes statement.
		/// </summary>
		/// <param name="statement">Statement for optimization.</param>
		/// <param name="withParameters">Indicates that query should be optimized including parameter values.</param>
		/// <returns>Optimized statement.</returns>
		SqlStatement OptimizeStatement (SqlStatement statement, bool withParameters);

		/// <summary>
		/// Examine query for parameter dependency.
		/// </summary>
		/// <param name="statement"></param>
		/// <returns></returns>
		bool IsParameterDependent(SqlStatement statement);

		/// <summary>
		/// Converts query to specific provider dialect. Including Take/Skip, CASE, Convert ect.
		/// </summary>
		/// <param name="mappingSchema"></param>
		/// <param name="statement"></param>
		/// <param name="withParameters">Indicates that query should be optimized including parameter values.</param>
		/// <returns></returns>
		SqlStatement ConvertStatement (MappingSchema mappingSchema, SqlStatement statement, bool withParameters);
	}
}
