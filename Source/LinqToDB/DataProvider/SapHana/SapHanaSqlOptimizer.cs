using System;

namespace LinqToDB.DataProvider.SapHana
{
	using SqlProvider;
	using SqlQuery;

	class SapHanaSqlOptimizer : BasicSqlOptimizer
	{
		public SapHanaSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{

		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SapHanaSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete: statement = GetAlternativeDelete((SqlDeleteStatement) statement, dataOptions); break;
				case QueryType.Update: statement = GetAlternativeUpdate((SqlUpdateStatement) statement, dataOptions); break;
			}

			return statement;
		}

	}
}
