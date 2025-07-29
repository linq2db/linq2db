using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	public class Oracle12SqlOptimizer : Oracle11SqlOptimizer
	{
		public Oracle12SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new Oracle12SqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			CorrectOutputTables(statement);

			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((SqlDeleteStatement) statement, dataOptions); break;
				case QueryType.Update : statement = GetAlternativeUpdate((SqlUpdateStatement) statement, dataOptions, mappingSchema); break;
			}

			if (statement.IsUpdate() || statement.IsInsert() || statement.IsDelete())
				statement = ReplaceTakeSkipWithRowNum(statement, mappingSchema, false);

			return statement;
		}
	}
}
