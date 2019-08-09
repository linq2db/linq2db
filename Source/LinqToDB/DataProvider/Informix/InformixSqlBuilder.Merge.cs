#nullable disable
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using SqlQuery;

	partial class InformixSqlBuilder
	{
		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		// or also we can use
		// sysmaster:'informix'.sysdual
		protected override string FakeTable => "table(set{1})";

		// Informix is too lazy to infer types itself from context
		protected override bool MergeSourceValueTypeRequired(SqlValuesTable sourceEnumerable, int row, int column) => true;

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
