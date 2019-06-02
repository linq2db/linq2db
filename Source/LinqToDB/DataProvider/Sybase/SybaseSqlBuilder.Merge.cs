using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Sybase
{
	partial class SybaseSqlBuilder
	{
		// It doesn't make sense to fix empty source generation as it will take too much effort for nothing
		protected override bool MergeEmptySourceSupported => false;

		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		// Sybase have issues with some types
		// also it doesn't like empty source SELECT NULL query, which uses NULL for non-nullable column
		protected override bool MergeSourceTypesRequired => true;

		protected override void BuildMergeTerminator(SqlMergeStatement merge)
		{
			// for identity column insert - disable explicit insert support
			if (merge.HasIdentityInsert)
				BuildIdentityInsert(merge.Target, false);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			// for identity column insert - enable explicit insert support
			if (merge.HasIdentityInsert)
				BuildIdentityInsert(merge.Target, true);

			base.BuildMergeStatement(merge);
		}

		protected override void BuildTypedExpression(SqlDataType dataType, ISqlExpression value)
		{
			var buildType = !dataType.CanBeNull && value is SqlValue sqlValue && sqlValue.Value == null;

			if (buildType)
				StringBuilder.Append("CAST(");

			BuildExpression(value);

			if (buildType)
				StringBuilder.Append(" AS ");

			BuildDataType(dataType, false);

			if (buildType)
				StringBuilder.Append(")");
		}
	}
}
