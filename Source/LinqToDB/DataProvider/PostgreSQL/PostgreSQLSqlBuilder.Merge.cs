namespace LinqToDB.DataProvider.PostgreSQL
{
	using SqlQuery;

	public partial class PostgreSQLSqlBuilder
	{
		// we enable MERGE in base pgsql builder class intentionally
		// this will allow users to use older dialects with merge at the same time
		// (e.g. to use non-merge insertorreplace implementation)

		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source,
			IReadOnlyList<ISqlExpression[]> rows, int row, int column)
		{
			return row < 0
				// if column contains NULL in all rows, pgsql will type is as "text"
				|| (row == 0 && rows.All(r => r[column] is SqlValue value && value.Value == null));
		}
	}
}
