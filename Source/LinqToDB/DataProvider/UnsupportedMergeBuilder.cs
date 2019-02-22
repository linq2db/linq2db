using LinqToDB.Data;

namespace LinqToDB.DataProvider
{
	class UnsupportedMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public UnsupportedMergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		public override void Validate()
		{
			throw new LinqToDBException(string.Format("Merge is not supported by {0} provider.", ProviderName));
		}
	}
}
