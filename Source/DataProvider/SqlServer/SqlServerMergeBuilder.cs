using LinqToDB.Data;
using System.Linq;
using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServerMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		protected override bool BySourceOperationsSupported => true;

		protected override bool IsIdentityInsertSupported => true;

		private bool _hasIdentityInsert;

		protected override int MaxOperationsCount => 3;

		protected override bool SameTypeOperationsAllowed => false;

		public SqlServerMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override void GenerateInsert(
			Expression<Func<TSource, bool>> predicate,
			Expression<Func<TSource, TTarget>> create)
		{
			// no need to check if we called second time or first ast sql server doesn't support multiple operations
			// of the same type
			_hasIdentityInsert = TargetDescriptor.Columns.Any(c => c.IsIdentity);

			if (_hasIdentityInsert)
				Command.Insert(0, string.Format("SET IDENTITY_INSERT {0} ON{1}", TargetTableName, Environment.NewLine));

			base.GenerateInsert(predicate, create);
		}

		protected override void GenerateTerminator()
		{
			Command.Append(";");
			if (_hasIdentityInsert)
				Command.AppendFormat("SET IDENTITY_INSERT {0} OFF", TargetTableName).AppendLine();
		}
	}
}
