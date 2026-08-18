using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class Firebird6SqlOptimizer : Firebird3SqlOptimizer
	{
		public Firebird6SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new Firebird6SqlExpressionConvertVisitor(allowModify);
		}
	}
}
