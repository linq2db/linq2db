using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public partial class SapHanaSqlBuilder
	{
		// HANA's native single-statement upsert:
		//   UPSERT <table> (col, …) VALUES (val, …) WITH PRIMARY KEY
		// applies the same VALUES list to both branches; per-branch divergence is intercepted
		// upstream by IsInsertOrUpdateRequiresAlignedBranches → 3-query emulation.
		// Update-branch predicates (Update.When) are likewise routed away by
		// IsInsertOrUpdateWithPredicateSupported = false. By the time we get here, Insert.Items
		// is guaranteed to align with Update.Items on every non-key column.
		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildTag(insertOrUpdate);
			BuildInsertClause(insertOrUpdate, insertOrUpdate.Insert, "UPSERT ", appendTableName: true, addAlias: false);
			AppendIndent().AppendLine("WITH PRIMARY KEY");
		}
	}
}
