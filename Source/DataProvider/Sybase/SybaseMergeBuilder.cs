using LinqToDB.Data;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.Sybase
{
	class SybaseMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		private bool _hasIdentityInsert;

		public SybaseMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override bool IsIdentityInsertSupported
		{
			get
			{
				// Sybase supports implicit identify insert
				return true;
			}
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				// Doesn't support VALUES(...) syntax in MERGE source
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

		protected override void BuildTerminator()
		{
			if (_hasIdentityInsert)
				Command.AppendFormat("SET IDENTITY_INSERT {0} OFF", TargetTableName).AppendLine();
		}

		protected override void OnInsertWithIdentity()
		{
			if (!_hasIdentityInsert)
			{
				_hasIdentityInsert = true;

				// this code should be added before MERGE and command already partially generated at this stage
				Command.Insert(0, string.Format("SET IDENTITY_INSERT {0} ON{1}", TargetTableName, Environment.NewLine));
			}
		}

		public override void Validate()
		{
			base.Validate();

			if (Merge.Operations.All(_ => _.Type == MergeOperationType.Delete))
				throw new LinqToDBException(string.Format("Merge only with Delete operations not supported by {0} provider.", ProviderName));
		}
	}
}
