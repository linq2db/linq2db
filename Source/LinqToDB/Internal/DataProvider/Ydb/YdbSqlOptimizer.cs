using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbSqlOptimizer : BasicSqlOptimizer
	{
		public YdbSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags) { }

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
			=> new YdbSqlExpressionConvertVisitor(allowModify);

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
					// disable table alias
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
			}

			return statement;
		}

		//public override SqlStatement TransformStatement(
		//	SqlStatement statement,
		//	DataOptions dataOptions,
		//	MappingSchema mappingSchema)
		//{
		//	statement = base.TransformStatement(statement, dataOptions, mappingSchema);

		//	return statement.QueryType switch
		//	{
		//		QueryType.Delete => CleanDeleteAlias(GetAlternativeDelete((SqlDeleteStatement)statement)),
		//		QueryType.Update => CleanUpdateAlias((SqlUpdateStatement)statement),

		//		_ => statement
		//	};
		//}

		//private static SqlDeleteStatement CleanDeleteAlias(SqlDeleteStatement stmt)
		//{
		//	if (stmt.SelectQuery.From.Tables.Count == 1)
		//	{
		//		var ts = stmt.SelectQuery.From.Tables[0];
		//		ts.Alias = null;
		//		if (ts.Source is SqlTable tbl)
		//			tbl.Alias = null;
		//	}

		//	return stmt;
		//}

		//private static SqlUpdateStatement CleanUpdateAlias(SqlUpdateStatement stmt)
		//{
		//	if (stmt.SelectQuery.From.Tables.Count == 1)
		//	{
		//		var ts = stmt.SelectQuery.From.Tables[0];
		//		ts.Alias = null;
		//		if (ts.Source is SqlTable tbl)
		//			tbl.Alias = null;
		//	}

		//	return stmt;
		//}
	}
}
