using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.DataProvider.Firebird
{
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
