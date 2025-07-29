using System;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Informix
{
	public class InformixSqlOptimizer : BasicSqlOptimizer
	{
		public InformixSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new InformixSqlExpressionConvertVisitor(allowModify);
		}

		public override bool IsParameterDependedElement(NullabilityContext nullability, IQueryElement element, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (base.IsParameterDependedElement(nullability, element, dataOptions, mappingSchema))
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

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement.VisitAll(SetQueryParameter);

			statement = base.Finalize(mappingSchema, statement, dataOptions);

			return statement;
		}

		protected override SqlStatement FixSetOperationValues(MappingSchema mappingSchema, SqlStatement statement)
		{
			statement = base.FixSetOperationValues(mappingSchema, statement);

			// IBM.Data.Db2 provider has a bug, where it could type nullable column in set as non-nullable, because first
			// part of set use non-nullable column
			// see tests: Issue4220Test, UnionCteWithFilter
			// as workaround we apply "NVL(x, NULL)" wrap to force column being nullable
			// Required pre-conditions:
			// 1. column in SET nullable
			// 2. column is first part of SET is not nullable
			// 3. column is field

			// IFX doesn't support output/returning, so only SELECT could return columns
			if (statement.QueryType == QueryType.Select)
			{
				statement.VisitParentFirst(mappingSchema, static (mappingSchema, e) =>
				{
					if (e.ElementType == QueryElementType.SqlQuery)
					{
						var query = (SelectQuery)e;
						if (query.HasSetOperators)
						{
							var firstSet           = query.SetOperators[0];
							var nullabilityContext = NullabilityContext.GetContext(query);

							for (var i = 0; i < query.Select.Columns.Count; i++)
							{
								var column = query.Select.Columns[i];

								if (column.Expression.ElementType != QueryElementType.SqlField || column.Expression.CanBeNullable(nullabilityContext))
									continue;

								foreach (var setOperator in query.SetOperators)
								{
									if (!setOperator.SelectQuery.Select.Columns[i].Expression.CanBeNullable(nullabilityContext))
										continue;

									var expression    = column.Expression;
									var dbType        = QueryHelper.GetDbDataType(expression, mappingSchema);
									column.Expression = new SqlFunction(dbType, "NVL", expression, new SqlValue(dbType, null)) { CanBeNull = true };
									break;
								}
							}
						}
					}

					return true;
				});
			}

			return statement;
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);
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

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.FinalizeStatement(statement, context, dataOptions, mappingSchema);
			statement = WrapParameters(statement, context);
			return statement;
		}

		internal static TElement WrapParameters<TElement>(TElement statement, EvaluationContext context)
			where TElement: IQueryElement
		{
			// Known cases:
			// - derived columns (column of CTE query)

			var visitor = new WrapParametersVisitor(VisitMode.Modify);

			statement = (TElement)visitor.WrapParameters(
				statement,
				WrapParametersVisitor.WrapFlags.InSelect |
				WrapParametersVisitor.WrapFlags.InBinary |
				WrapParametersVisitor.WrapFlags.InFunctionParameters);

			return statement;
		}
	}
}
