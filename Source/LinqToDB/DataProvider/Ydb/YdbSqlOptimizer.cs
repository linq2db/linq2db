using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ydb
{
	sealed class YdbSqlOptimizer : BasicSqlOptimizer
	{
		public YdbSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags) { }

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
			=> new YdbSqlExpressionConvertVisitor(allowModify);

		public override SqlStatement TransformStatement(
			SqlStatement statement,
			DataOptions dataOptions,
			MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			return statement.QueryType switch
			{
				QueryType.Delete => CleanDeleteAlias(
						(SqlDeleteStatement)GetAlternativeDelete(
							(SqlDeleteStatement)statement, dataOptions)),
				QueryType.Update => CorrectYdbUpdate(
						(SqlUpdateStatement)statement, dataOptions, mappingSchema),
				_ => statement
			};
		}

		// -----------------------------------------------------------------
		// DELETE  (delete alias after FROM)
		// -----------------------------------------------------------------
		private static SqlDeleteStatement CleanDeleteAlias(SqlDeleteStatement stmt)
		{
			// Target‑table всегда первая в списке From.Tables
			if (stmt.SelectQuery.From.Tables.Count == 1)
			{
				var ts = stmt.SelectQuery.From.Tables[0];
				ts.Alias = null;
				if (ts.Source is SqlTable tbl)
					tbl.Alias = null;
			}

			return stmt;
		}

		// -----------------------------------------------------------------
		// UPDATE
		// -----------------------------------------------------------------
		private SqlStatement CorrectYdbUpdate(SqlUpdateStatement statement,
			DataOptions dataOptions,
			MappingSchema mappingSchema)
			=> GetAlternativeUpdate(statement, dataOptions, mappingSchema);
	}
}
