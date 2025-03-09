using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Informix
{
	partial class InformixSqlBuilder
	{
		// VALUES(...) syntax not supported
		protected override bool IsValuesSyntaxSupported => false;

		// or also we can use sysmaster:sysdual added in 11.70
		// // but SET present even in ancient 9.x versions (but not it 7.x it seems)
		protected override string FakeTable => "table(set{1})";

		// Informix is too lazy to infer types itself from context
		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source,
			IReadOnlyList<ISqlExpression[]>                                      rows, int row, int column) => true;

		protected override void BuildMergeInto(NullabilityContext nullability, SqlMergeStatement merge)
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
