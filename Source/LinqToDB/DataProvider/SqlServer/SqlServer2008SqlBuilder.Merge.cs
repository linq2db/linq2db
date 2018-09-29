namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;

	partial class SqlServer2008SqlBuilder
	{
		protected override void BuildMergeInto(SqlMergeStatement merge)
		{
			StringBuilder
				.Append("MERGE INTO ");

			BuildTableName(merge.Target, true, false);

			StringBuilder.Append(" ");

			if (merge.Hint != null)
			{
				StringBuilder
					.Append("WITH(")
					.Append(merge.Hint)
					.Append(") ");
			}

			BuildTableName(merge.Target, false, true);
			StringBuilder.AppendLine();
		}

		protected override void BuildMergeOperationDeleteBySource(SqlMergeOperationClause operation)
		{
			StringBuilder
				.Append("WHEN NOT MATCHED BY SOURCE");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where);
			}

			StringBuilder.AppendLine(" THEN DELETE");
		}

		protected override void BuildMergeTerminator()
		{
			// merge command must be terminated with semicolon
			StringBuilder.AppendLine(";");

			// disable explicit identity insert
			//if (_hasIdentityInsert)
			//	StringBuilder.AppendFormat("SET IDENTITY_INSERT {0} OFF", TargetTableName).AppendLine();
		}
	}
}
