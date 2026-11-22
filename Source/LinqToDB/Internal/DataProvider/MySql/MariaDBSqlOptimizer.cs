using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.DataProvider.MySql
{
	/// <summary>
	/// MariaDB differs from MySQL proper on window function requirements, so it gets its own convert visitor.
	/// </summary>
	/// <remarks>
	/// A subclass rather than a version passed to <see cref="MySqlSqlOptimizer"/>: a remote data context builds
	/// its optimizer reflectively and accepts only a <c>(SqlProviderFlags)</c> or <c>(SqlProviderFlags, DataOptions)</c>
	/// constructor, so a version parameter would leave the type unconstructible over a <c>LinqService</c> connection.
	/// </remarks>
	public class MariaDBSqlOptimizer : MySqlSqlOptimizer
	{
		public MariaDBSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new MariaDBSqlExpressionConvertVisitor(allowModify);
		}
	}
}
