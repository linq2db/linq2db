using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Sybase
{
	partial class SybaseSqlBuilder
	{
		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

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
	}
}
