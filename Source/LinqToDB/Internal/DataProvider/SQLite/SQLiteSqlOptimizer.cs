using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteSqlOptimizer : BasicSqlOptimizer
	{
		public SQLiteSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags)
		{

		}

		public override bool RequiresCastingParametersForSetOperations => false;

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SQLiteSqlExpressionConvertVisitor(allowModify);
		}

		public override bool IsParameterDependedElement(NullabilityContext nullability, IQueryElement element, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (base.IsParameterDependedElement(nullability, element, dataOptions, mappingSchema))
				return true;

			// Dependence is decided on the BETWEEN, before the convert visitor writes it as its comparisons, so it is asked of
			// those comparisons: one of two client values would otherwise be folded only where parameter values are at hand.
			if (element is SqlPredicate.Between between && SQLiteSqlExpressionConvertVisitor.LowersBetween(between, mappingSchema))
			{
				foreach (var comparison in QueryHelper.ConvertBetweenToComparisons(between).Predicates)
				{
					if (base.IsParameterDependedElement(nullability, comparison, dataOptions, mappingSchema))
						return true;
				}
			}

			return false;
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete :
				{
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
				}

				case QueryType.Update :
				{
					if (SqlProviderFlags.IsUpdateFromSupported)
					{
						statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					}
					else
					{
						statement = GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					}

					break;
				}
			}

			return statement;
		}

	}
}
