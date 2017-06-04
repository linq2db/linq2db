using LinqToDB.Data;

namespace LinqToDB.DataProvider
{
	class UnsupportedMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public UnsupportedMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		public override void Validate()
		{
			throw new LinqToDBException(string.Format("Merge is not supported by {0} provider.", ProviderName));
		}
	}
}
