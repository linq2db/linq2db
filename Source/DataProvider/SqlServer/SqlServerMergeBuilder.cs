using LinqToDB.Data;
using System;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServerMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		protected override bool BySourceOperationsSupported
		{
			get
			{
				return true;
			}
		}

		protected override bool IsIdentityInsertSupported
		{
			get
			{
				return true;
			}
		}

		private bool _hasIdentityInsert;

		protected override int MaxOperationsCount
		{
			get
			{
				return 3;
			}
		}

		protected override bool SameTypeOperationsAllowed
		{
			get
			{
				return false;
			}
		}

		public SqlServerMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override void OnInsertWithIdentity()
		{
			if (!_hasIdentityInsert)
			{
				_hasIdentityInsert = true;
				Command.Insert(0, string.Format("SET IDENTITY_INSERT {0} ON{1}", TargetTableName, Environment.NewLine));
			}
		}

		protected override void GenerateTerminator()
		{
			Command.Append(";");
			if (_hasIdentityInsert)
				Command.AppendFormat("SET IDENTITY_INSERT {0} OFF", TargetTableName).AppendLine();
		}
	}
}
