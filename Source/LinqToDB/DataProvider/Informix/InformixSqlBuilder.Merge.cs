using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using SqlQuery;

	partial class InformixSqlBuilder
	{
		// parameters in source select list not supported
		protected override bool MergeSupportsParametersInSource => false;

		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		// or also we can use
		// sysmaster:'informix'.sysdual
		protected override string FakeTable => "table(set{1})";

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
			StringBuilder.AppendLine();
		}
	}
}
