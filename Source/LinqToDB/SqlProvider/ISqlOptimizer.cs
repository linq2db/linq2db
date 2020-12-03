using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
		/// Examine query for parameter dependency.
		/// </summary>
		/// <param name="statement"></param>
		/// <returns></returns>
		bool IsParameterDependent(SqlStatement statement);

		/// <summary>
		/// Corrects skip/take for specific DataProvider
		/// </summary>
		/// <param name="mappingSchema"></param>
		/// <param name="selectQuery"></param>
		/// <param name="optimizationContext"></param>
		/// <param name="takeExpr"></param>
		/// <param name="skipExpr"></param>
		void ConvertSkipTake(MappingSchema mappingSchema, SelectQuery selectQuery, OptimizationContext optimizationContext, out ISqlExpression? takeExpr, out ISqlExpression? skipExpr);
		SqlStatement ConvertStatement(MappingSchema mappingSchema, SqlStatement statement, OptimizationContext optimizationContext);

		/// <summary>
		/// Converts expression to specific provider dialect. 
		/// </summary>
		/// <param name="mappingSchema"></param>
		/// <param name="expression"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("expression")]
		ISqlExpression? ConvertExpression(MappingSchema mappingSchema, ISqlExpression? expression, OptimizationContext context);

		/// <summary>
		/// Converts predicate to specific provider dialect. 
		/// </summary>
		/// <param name="mappingSchema"></param>
		/// <param name="predicate"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		ISqlPredicate ConvertPredicate(MappingSchema mappingSchema, ISqlPredicate predicate, OptimizationContext context);
	}
}
