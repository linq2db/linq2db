using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	partial class SqlServer2012SqlBuilder
	{
		// TODO: both 2008 and 2012 builders inherit from same base class which leads to duplicate builder logic
		// I think we should have single builder with versioning support as inheritance definitely suck for this case
		protected override void BuildMergeInto(NullabilityContext nullability, SqlMergeStatement merge)
		{
			StringBuilder
				.Append("MERGE INTO ");

			BuildTableName(merge.Target, true, false);

			StringBuilder.Append(' ');

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

		protected override void BuildMergeOperationDeleteBySource(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			StringBuilder
				.Append("WHEN NOT MATCHED BY SOURCE");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition : true);
			}

			StringBuilder.AppendLine(" THEN DELETE");
		}

		protected override void BuildMergeTerminator(NullabilityContext nullability, SqlMergeStatement merge)
		{
			// merge command must be terminated with semicolon
			StringBuilder.AppendLine(";");

			// for identity column insert - disable explicit insert support
			if (merge.HasIdentityInsert)
				BuildIdentityInsert(nullability, merge.Target, false);
		}

		protected override void BuildMergeOperationUpdateBySource(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN NOT MATCHED By Source");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition : true);
			}

			StringBuilder.AppendLine(" THEN UPDATE");

			var update = new SqlUpdateClause();
			update.Items.AddRange(operation.Items);
			BuildUpdateSet(null, update);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			// for identity column insert - enable explicit insert support
			if (merge.HasIdentityInsert)
			{
				var nullability = new NullabilityContext(merge.SelectQuery);
				BuildIdentityInsert(nullability, merge.Target, true);
			}

			base.BuildMergeStatement(merge);
		}
	}
}
