namespace LinqToDB.DataProvider.Firebird
{
	using SqlProvider;

	public class Firebird3SqlOptimizer : FirebirdSqlOptimizer
	{
		public Firebird3SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new Firebird3SqlExpressionConvertVisitor(allowModify);
		}
	}
}
