namespace LinqToDB.DataProvider.Oracle
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public class Oracle23SqlOptimizer : Oracle12SqlOptimizer
	{
		public Oracle23SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new Oracle23SqlExpressionConvertVisitor(allowModify);
		}
	}
}
