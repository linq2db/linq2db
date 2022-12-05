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
		/// <returns>Query which is ready for optimization.</returns>
		SqlStatement Finalize          (MappingSchema mappingSchema, SqlStatement statement);

		/// <summary>
		/// Examine query for parameter dependency.
		/// </summary>
		/// <returns></returns>
		bool IsParameterDependent(SqlStatement statement);

		/// <summary>
		/// Corrects skip/take for specific DataProvider
		/// </summary>
		void ConvertSkipTake(MappingSchema mappingSchema, SelectQuery selectQuery, OptimizationContext optimizationContext, out ISqlExpression? takeExpr, out ISqlExpression? skipExpr);

		/// <summary>
		/// Converts query element to specific provider dialect. 
		/// </summary>
		[return: NotNullIfNotNull(nameof(element))]
		IQueryElement? ConvertElement(MappingSchema mappingSchema, IQueryElement? element, OptimizationContext context);

	}
}
