namespace LinqToDB.DataProvider.Oracle
{
	using SqlProvider;
	using SqlQuery;
	using System.Text;

	partial class OracleSqlBuilder
	{
		// Oracle doesn't support TABLE_ALIAS(COLUMN_ALIAS, ...) syntax
		protected override bool MergeSupportsColumnAliasesInSource => false;

		protected override void BuildMergeInto(SqlMergeStatement merge)
		{
			StringBuilder.Append("MERGE ");

			if (merge.Hint != null)
			{
				StringBuilder
					.Append("/*+ ")
					.Append(merge.Hint)
					.Append(" */ ");
			}

			StringBuilder.Append("INTO ");
			BuildTableName(merge.Target, true, true);
		}
	}
}
