using System.Linq;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.Sybase
{
	sealed class SybaseSqlOptimizer : BasicSqlOptimizer
	{
		public SybaseSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SybaseSqlExpressionConvertVisitor(allowModify);
		}

		protected override SqlStatement FinalizeUpdate(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (statement.QueryType is QueryType.Update or QueryType.Delete)
			{
				if (statement.SelectQuery!.Select.TakeValue != null && !statement.SelectQuery.Select.OrderBy.IsEmpty)
					throw new LinqToDBException(
						statement.QueryType == QueryType.Update
							? ErrorHelper.Sybase.Error_UpdateWithTopOrderBy
							: ErrorHelper.Sybase.Error_DeleteWithTopOrderBy);

				if (statement.SelectQuery.Select.SkipValue != null)
					throw new LinqToDBException(
						statement.QueryType == QueryType.Update
							? ErrorHelper.Sybase.Error_UpdateWithSkip
							: ErrorHelper.Sybase.Error_DeleteWithSkip);
			}

			if (statement.QueryType == QueryType.Update)
				return CorrectSybaseUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema);

			return base.FinalizeUpdate(statement, dataOptions, mappingSchema);
		}

		SqlUpdateStatement CorrectSybaseUpdate(SqlUpdateStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			CorrectUpdateSetters(statement);

			var isInCompatible = QueryHelper.EnumerateAccessibleSources(statement.SelectQuery).Any(t =>
			{
				if (t != statement.SelectQuery && t is SelectQuery)
					return true;
				if (t is SqlTable table && !ReferenceEquals(table, statement.Update.Table) && QueryHelper.IsEqualTables(table, statement.Update.Table))
					return true;
				return false;
			});

			if (isInCompatible)
			{
				statement = GetAlternativeUpdate(statement, dataOptions, mappingSchema);
			}
			else
			{
				var hasTableInQuery = QueryHelper.HasTableInQuery(statement.SelectQuery, statement.Update.Table!);
				if (hasTableInQuery && !RemoveUpdateTableIfPossible(statement.SelectQuery, statement.Update.Table!, out _))
					statement = GetAlternativeUpdate(statement, dataOptions, mappingSchema);
			}

			return statement;
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = CorrectMultiTableQueries(statement);

			return base.TransformStatement(statement, dataOptions, mappingSchema);
		}
	}
}
