using LinqToDB.Data;

namespace LinqToDB.DataProvider.DB2
{
	class DB2MergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public DB2MergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				// DB2 doesn't support INSERT FROM (well, except latest DB2 LUW version, but linq2db don't support it yet)
				return true;
			}
		}
	}
}
