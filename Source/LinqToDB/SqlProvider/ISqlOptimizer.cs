using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

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

		/// <summary>
		/// Converts query element to specific provider dialect. 
		/// </summary>
		/// <param name="mappingSchema"></param>
		/// <param name="element"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("element")]
		IQueryElement? ConvertElement(MappingSchema mappingSchema, IQueryElement? element, OptimizationContext context);

	}
}
