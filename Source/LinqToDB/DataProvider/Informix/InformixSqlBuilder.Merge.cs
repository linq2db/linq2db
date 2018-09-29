using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using SqlQuery;

	partial class InformixSqlBuilder
	{
		// parameters in source select list not supported
		protected override bool MergeSupportsParametersInSource => false;

		protected override void BuildMergeInto(SqlMergeStatement merge)
		{
			StringBuilder.Append("MERGE ");

			if (merge.Hint != null)
			{
				StringBuilder
					.Append("{+ ")
					.Append(merge.Hint)
					.Append(" } ");
			}

			StringBuilder.Append("INTO ");
			BuildTableName(merge.Target, true, true);
		}
	}
}
