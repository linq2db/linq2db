using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Specialized SQL-tree optimizer for YDB.
	/// Removes unnecessary aliases and prevents the generation of unsupported constructs.
	/// </summary>
	public class YdbSqlOptimizer : BasicSqlOptimizer
	{
		public YdbSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags) { }

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
			=> new YdbSqlExpressionConvertVisitor(allowModify);

		/// <summary>
		/// Adjusts the SQL tree after the base transformation:
		/// • For DELETE and UPDATE, clears the alias on the target table
		///   so that the resulting SQL matches YDB/YQL syntax
		///   (<c>UPDATE Table SET ... WHERE ...</c>, without “t1.”).
		/// </summary>
		public override SqlStatement TransformStatement(
			SqlStatement statement,
			DataOptions dataOptions,
			MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			return statement.QueryType switch
			{
				QueryType.Delete => CleanDeleteAlias(
						GetAlternativeDelete(
							(SqlDeleteStatement)statement)),

				QueryType.Update => CleanUpdateAlias((SqlUpdateStatement)statement),

				_ => statement
			};
		}

		// -----------------------------------------------------------------
		// DELETE  (remove alias after FROM)
		// -----------------------------------------------------------------
		private static SqlDeleteStatement CleanDeleteAlias(SqlDeleteStatement stmt)
		{
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
		// UPDATE  (remove alias after UPDATE)
		// -----------------------------------------------------------------
		private static SqlUpdateStatement CleanUpdateAlias(SqlUpdateStatement stmt)
		{
			if (stmt.SelectQuery.From.Tables.Count == 1)
			{
				var ts = stmt.SelectQuery.From.Tables[0];
				ts.Alias = null;
				if (ts.Source is SqlTable tbl)
					tbl.Alias = null;
			}

			return stmt;
		}
	}
}
