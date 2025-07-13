using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	partial class SybaseSqlBuilder
	{
		// VALUES(...) syntax not supported
		protected override bool IsValuesSyntaxSupported => false;

		protected override void BuildMergeTerminator(NullabilityContext nullability, SqlMergeStatement merge)
		{
			// for identity column insert - disable explicit insert support
			if (merge.HasIdentityInsert)
				BuildIdentityInsert(nullability, merge.Target, false);
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
