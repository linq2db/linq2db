using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessLibRedSqlOptimizer : AccessSqlOptimizer
	{
		public AccessLibRedSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new AccessLibRedSqlExpressionConvertVisitor(allowModify);
		}

		// LibRed evaluates EXISTS / IN as a projected value, so the Jet rewrite to COUNT(*) > 0 is not needed
		protected override SqlStatement CorrectExistsAndIn(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			return statement;
		}
	}
}
