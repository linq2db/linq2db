using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.DB2
{
	sealed class DB2SqlOptimizer : BasicSqlOptimizer
	{
		public DB2SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			// DB2 LUW 9/10 supports only FETCH, v11 adds OFFSET, but for that we need to introduce versions into DB2 provider first
			//statement = SeparateDistinctFromPagination(statement, q => q.Select.SkipValue != null);
			//statement = ReplaceDistinctOrderByWithRowNumber(statement, q => q.Select.SkipValue != null);
			//statement = ReplaceTakeSkipWithRowNumber(SqlProviderFlags, statement, static (SqlProviderFlags, query) => query.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(query.Select.TakeValue, query.Select.SkipValue), true);

			// This is mutable part
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema),
				_                => statement,
			};
		}

		#region Wrap Parameters
		private static SqlStatement WrapParameters(SqlStatement statement, EvaluationContext context)
		{
			// for some reason DB2 doesn't use parameter type information (not supported?) is some places, so
			// we need to wrap parameter into CAST() to add type information explicitly
			// As it is not clear when type CAST needed, below we should document observations on current behavior.
			//
			// When CAST is not needed:
			// - parameter already in CAST from original query
			// - parameter used as direct inserted/updated value in insert/update queries (including merge)
			//
			// When CAST is needed:
			// - in select column expression at any position (except nested subquery): select, subquery, merge source
			// - in composite expression in insert or update setter: insert, update, merge (not always, in some cases it works)

			var visitor = new WrapParametersVisitor(VisitMode.Modify);

			statement = (SqlStatement)visitor.WrapParameters(statement, WrapParametersVisitor.WrapFlags.InSelect | WrapParametersVisitor.WrapFlags.InInsertOrUpdate | WrapParametersVisitor.WrapFlags.InFunctionParameters);

			return statement;
		}

		#endregion

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = WrapParameters(statement, context);
			return base.FinalizeStatement(statement, context, dataOptions, mappingSchema);
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new DB2SqlExpressionConvertVisitor(allowModify);
		}
	}
}
