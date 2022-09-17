using System;

namespace LinqToDB.DataProvider.Informix
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class InformixSqlOptimizer : BasicSqlOptimizer
	{
		public InformixSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new InformixSqlExpressionConvertVisitor(allowModify);
		}

		public override bool IsParameterDependedElement(NullabilityContext nullability, IQueryElement element, DataOptions dataOptions)
		{
			if (base.IsParameterDependedElement(nullability, element, dataOptions))
				return true;

			switch (element.ElementType)
			{
				case QueryElementType.LikePredicate:
				{
					var like = (SqlPredicate.Like)element;
					if (like.Expr2.ElementType != QueryElementType.SqlValue)
						return true;
					break;
				}

				case QueryElementType.SearchStringPredicate:
				{
					var containsPredicate = (SqlPredicate.SearchString)element;
					if (containsPredicate.Expr2.ElementType != QueryElementType.SqlValue)
						return true;

					return false;
				}

			}

			return false;
		}

		static void SetQueryParameter(IQueryElement element)
		{
			if (element is SqlParameter p)
			{
				// TimeSpan parameters created for IDS provider and must be converted to literal as IDS doesn't support
				// intervals explicitly
				if ((p.Type.SystemType == typeof(TimeSpan) || p.Type.SystemType == typeof(TimeSpan?))
						&& p.Type.DataType != DataType.Int64)
					p.IsQueryParameter = false;
			}
		}

		static void ClearQueryParameter(IQueryElement element)
		{
			if (element is SqlParameter p && p.IsQueryParameter)
				p.IsQueryParameter = false;
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement.VisitAll(SetQueryParameter);

			// TODO: test if it works and enable support with type-cast like it is done for Firebird
			// Informix doesn't support parameters in select list
			var ignore = statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count == 0;
			if (!ignore)
				statement.VisitAll(static e =>
				{
					if (e is SqlSelectClause select)
						select.VisitAll(ClearQueryParameter);
				});

			return base.Finalize(mappingSchema, statement, dataOptions);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = CorrectMultiTableQueries(statement);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
					var deleteStatement = GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions);
					statement = deleteStatement;
					if (deleteStatement.SelectQuery != null)
						deleteStatement.SelectQuery.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update:
					statement = GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					break;
			}

			return statement;
		}
	}
}
