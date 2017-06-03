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
				return true;
			}
		}
	}
}
