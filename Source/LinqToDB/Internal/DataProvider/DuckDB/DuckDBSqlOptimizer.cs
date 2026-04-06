using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSqlOptimizer : BasicSqlOptimizer
	{
		public DuckDBSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new DuckDBSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
				{
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					break;
				}
				case QueryType.Update:
				{
					statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					break;
				}
			}

			// DuckDB does not support prepared parameters in RETURNING clauses.
			// Force inline all parameters in OUTPUT clause.
			InlineParametersInOutputClause(statement);

			return statement;
		}

		/// <summary>
		/// DuckDB does not support prepared parameters in RETURNING clauses.
		/// This method finds all parameters in output clauses and marks them for inlining.
		/// </summary>
		static void InlineParametersInOutputClause(SqlStatement statement)
		{
			var output = statement switch
			{
				SqlDeleteStatement   del => del.OutputClause,
				SqlInsertStatement   ins => ins.OutputClause,
				SqlUpdateStatement   upd => upd.OutputClause,
				SqlMergeStatement  merge => merge.OutputClause,
				_ => null,
			};

			if (output?.HasOutput != true)
				return;

			var visitor = new InlineOutputParametersVisitor();
			visitor.Visit(output);
		}

		sealed class InlineOutputParametersVisitor : SqlQueryVisitor
		{
			public InlineOutputParametersVisitor() : base(VisitMode.ReadOnly, null)
			{
			}

			protected internal override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
			{
				sqlParameter.IsQueryParameter = false;
				return base.VisitSqlParameter(sqlParameter);
			}
		}
	}
}
