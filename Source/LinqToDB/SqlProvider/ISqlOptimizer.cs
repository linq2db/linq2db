using System.Collections.Generic;

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
		/// <param name="parameterValues">Contains parameter values. If it is null, that means that parameters should be ignored from evaluation.</param>
		/// <returns>Optimized statement.</returns>
		SqlStatement OptimizeStatement (SqlStatement statement, IReadOnlyDictionary<SqlParameter, SqlParameterValue>? parameterValues);

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
		/// <param name="parameterValues">Contains parameter values. If it is null, that means that parameters should be ignored from evaluation.</param>
		/// <returns></returns>
		SqlStatement ConvertStatement (MappingSchema mappingSchema, SqlStatement statement, IReadOnlyDictionary<SqlParameter, SqlParameterValue>? parameterValues);
	}
}
