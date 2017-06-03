using LinqToDB.Data;

namespace LinqToDB.DataProvider.Firebird
{
	class FirebirdMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public FirebirdMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				return false;
			}
		}

		protected override string FakeSourceTable
		{
			get
			{
				return "rdb$database";
			}
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
