namespace LinqToDB.DataProvider.Sybase
{
	using SqlQuery;

	partial class SybaseSqlBuilder
	{
		// VALUES(...) syntax not supported
		protected override bool IsValuesSyntaxSupported => false;

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
