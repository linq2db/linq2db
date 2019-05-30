namespace LinqToDB.DataProvider.Oracle
{
	using SqlProvider;
	using SqlQuery;
	using System.Text;

	partial class OracleSqlBuilder
	{
		// Oracle doesn't support TABLE_ALIAS(COLUMN_ALIAS, ...) syntax
		protected override bool MergeSupportsColumnAliasesInSource => false;

		// It doesn't make sense to fix empty source generation as it will take too much effort for nothing
		protected override bool MergeEmptySourceSupported => false;

		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		// bad thing that user can change this table, but broken merge will be minor issue in this case
		protected override string FakeTable => "dual";

		// dual table owner
		protected override string FakeTableSchema => "sys";

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
			StringBuilder.AppendLine();
		}

		protected override void BuildMergeOperationInsert(SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("WHEN NOT MATCHED THEN")
				.Append("INSERT")
				;

			var insertClause = new SqlInsertClause();
			insertClause.Items.AddRange(operation.Items);

			BuildInsertClause(new SqlInsertOrUpdateStatement(null), insertClause, null, false, false);

			if (operation.Where.Conditions.Count != 0)
			{
				StringBuilder.Append(" WHERE ");
				BuildSearchCondition(Precedence.Unknown, operation.Where);
			}
		}

		protected override void BuildMergeOperationUpdate(SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("WHEN MATCHED THEN")
				.AppendLine("UPDATE")
				;

			var update = new SqlUpdateClause();
			update.Items.AddRange(operation.Items);
			BuildUpdateSet(null, update);

			if (operation.Where.Conditions.Count != 0)
			{
				StringBuilder
					.AppendLine("WHERE")
					.Append("\t")
					;
				BuildSearchCondition(Precedence.Unknown, operation.Where);
			}
		}
	}
}
