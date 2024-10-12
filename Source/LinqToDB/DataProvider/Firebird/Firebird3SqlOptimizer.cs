namespace LinqToDB.DataProvider.Firebird
{
	using LinqToDB.Internal.SqlProvider;

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
