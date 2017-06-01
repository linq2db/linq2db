using LinqToDB.Data;

namespace LinqToDB.DataProvider.SapHana
{
	class SapHanaMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public SapHanaMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override bool DeleteOperationSupported
		{
			get
			{
				return false;
			}
		}

		protected override bool OperationPredicateSupported
		{
			get
			{
				return false;
			}
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
				return "DUMMY";
			}
		}

		protected override bool SupportsColumnAliasesInTableAlias
		{
			get
			{
				return false;
			}
		}

		public override void Validate()
		{
			base.Validate();

			// it is not stated in documentation
			if (Merge.Operations.Length == 2 && Merge.Operations[0].Type == MergeOperationType.Insert)
				throw new LinqToDBException(string.Format("Merge Insert operation must be placed after Update operation for {0} provider.", ProviderName));
		}
	}
}
