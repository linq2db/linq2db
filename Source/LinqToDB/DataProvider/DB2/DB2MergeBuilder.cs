using LinqToDB.Data;

namespace LinqToDB.DataProvider.DB2
{
	class DB2MergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public DB2MergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				// DB2 doesn't support INSERT FROM (well, except latest DB2 LUW version, but linq2db doesn't support it yet)
				return true;
			}
		}
	}
}
