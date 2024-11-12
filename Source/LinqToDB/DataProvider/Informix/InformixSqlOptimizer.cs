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

		protected override SqlStatement FixSetOperationValues(MappingSchema mappingSchema, SqlStatement statement)
		{
			statement = base.FixSetOperationValues(mappingSchema, statement);

			// IBM.Data.Db2 provider has a bug, where it could type nullable column in set as non-nullable, because first
			// part of set use non-nullable column
			// see test: Issue4220Test
			// as workaround we apply "NVL(x, NULL)" wrap to force column being nullable
			// Required pre-conditions:
			// 1. query is top-level SET select
			// 2. column in SET nullable
			// 3. column is first part of SET is not nullable
			if (statement.QueryType == QueryType.Select && statement.SelectQuery?.HasSetOperators == true)
			{
				var query = statement.SelectQuery;

				var firstSet = query.SetOperators[0];

				for (var i = 0; i < query.Select.Columns.Count; i++)
				{
					var column = query.Select.Columns[i];

					if (column.Expression.CanBeNullable(NullabilityContext.NonQuery))
						continue;

					foreach (var setOperator in query.SetOperators)
					{
						if (!setOperator.SelectQuery.Select.Columns[i].Expression.CanBeNullable(NullabilityContext.NonQuery))
							continue;

						var expression    = column.Expression;
						var dbType        = QueryHelper.GetDbDataType(expression, mappingSchema);
						column.Expression = new SqlFunction(dbType, "NVL", expression, new SqlValue(dbType, null)) { CanBeNull = true };
						break;
					}
				}
			}

			return statement;
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
