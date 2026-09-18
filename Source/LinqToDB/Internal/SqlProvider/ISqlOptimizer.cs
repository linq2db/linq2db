using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlProvider
{
	public interface ISqlOptimizer
	{
		/// <summary>
		/// Finalizes query.
		/// </summary>
		/// <returns>Query which is ready for optimization.</returns>
		SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions);

		/// <summary>
		/// Examine query for parameter dependency.
		/// </summary>
		/// <returns></returns>
		bool IsParameterDependent(NullabilityContext nullability, MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions);

		SqlExpressionOptimizerVisitor      CreateOptimizerVisitor(bool allowModify);
		SqlExpressionConvertVisitor        CreateConvertVisitor(bool   allowModify);

		ISqlExpressionFactory CreateSqlExpressionFactory(MappingSchema mappingSchema, DataOptions dataOptions);
	}
}
