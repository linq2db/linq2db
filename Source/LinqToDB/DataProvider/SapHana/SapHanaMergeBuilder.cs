using LinqToDB.Data;
using LinqToDB.Linq;

namespace LinqToDB.DataProvider.SapHana
{
	class SapHanaMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public SapHanaMergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				// INSERT FROM not supported
				return true;
			}
		}

		protected override bool DeleteOperationSupported
		{
			get
			{
				// delete operations not supported
				return false;
			}
		}

		protected override bool OperationPredicateSupported
		{
			get
			{
				// operation conditions not supported
				return false;
			}
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				// VALUES(...) syntax in MERGE source not supported
				return false;
			}
		}

		protected override string FakeSourceTable
		{
			get
			{
				// predefined table with 1 record
				// unfortunatelly, user could change this table
				return "DUMMY";
			}
		}

		protected override bool SupportsColumnAliasesInTableAlias
		{
			get
			{
				// TABLE_ALIAS(COLUMN_ALIAS, ...) syntax not supported
				return false;
			}
		}

		protected override bool EmptySourceSupported
		{
			get
			{
				// It doesn't make sense to fix empty source generation as it will take too much effort for nothing
				return false;
			}
		}

		public override void Validate()
		{
			base.Validate();

			// it is not documented, but Update should go first
			if (Merge.Operations.Length == 2 && Merge.Operations[0].Type == MergeOperationType.Insert)
				throw new LinqToDBException(string.Format("Merge Insert operation must be placed after Update operation for {0} provider.", ProviderName));
		}
	}
}
