using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class FirebirdSqlOptimizer : BasicSqlOptimizer
	{
		public FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new FirebirdSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement = base.Finalize(mappingSchema, statement, dataOptions);

			return statement;
		}

		public override bool IsParameterDependedElement(NullabilityContext nullability, IQueryElement element, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			var result = base.IsParameterDependedElement(nullability, element, dataOptions, mappingSchema);
			if (result)
				return true;

			switch (element.ElementType)
			{
				case QueryElementType.LikePredicate:
				{
					var like = (SqlPredicate.Like)element;
					if (like.Expr1.ElementType != QueryElementType.SqlValue ||
					    like.Expr2.ElementType != QueryElementType.SqlValue)
						return true;
					break;
				}

				case QueryElementType.SearchStringPredicate:
				{
					var containsPredicate = (SqlPredicate.SearchString)element;
					if (containsPredicate.Expr1.ElementType != QueryElementType.SqlValue || containsPredicate.Expr2.ElementType != QueryElementType.SqlValue)
						return true;

					return false;
				}

			}

			return false;
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema),
				_                => statement,
			};
		}

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.FinalizeStatement(statement, context, dataOptions, mappingSchema);
			statement = WrapParameters(statement, context);
			return statement;
		}

		#region Wrap Parameters
		static SqlStatement WrapParameters(SqlStatement statement, EvaluationContext context)
		{
			// for some reason Firebird doesn't use parameter type information (not supported?) is some places, so
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

			statement = (SqlStatement)visitor.WrapParameters(statement,
				WrapParametersVisitor.WrapFlags.InSelect         |
				WrapParametersVisitor.WrapFlags.InUpdateSet      |
				WrapParametersVisitor.WrapFlags.InInsertOrUpdate |
				WrapParametersVisitor.WrapFlags.InOutput         |
				WrapParametersVisitor.WrapFlags.InMerge);

			return statement;
		}

		#endregion
	}
}
