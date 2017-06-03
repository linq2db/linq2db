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
				return true;
			}
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				return false;
			}
		}

		protected override MergeDefinition<TTarget, TSource> AddExtraCommands(MergeDefinition<TTarget, TSource> merge)
		{
			if (merge.Operations.Length > 0 && merge.Operations.All(_ => _.Type == MergeOperationType.Delete))
			{
				// Sybase doesn't like merge with only delete commands, so we will add fake update
				// if it will fail - user can always add it manually
				return (MergeDefinition<TTarget, TSource>)((IMerge<TTarget, TSource>)merge)
					.Update((t, s) => false, FakeUpdate());
			}

			return merge;
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

		private Expression<Func<TTarget, TSource, TTarget>> FakeUpdate()
		{
			var targetParam = Expression.Parameter(typeof(TTarget), "t");
			var sourceParam = Expression.Parameter(typeof(TSource), "s");

			var body = Expression.MemberInit(
				Expression.New(typeof(TTarget)),
				TargetDescriptor.Columns.Where(_ => !_.SkipOnUpdate && !_.IsIdentity)
				.Select(c => Expression.Bind(c.MemberInfo, Expression.PropertyOrField(targetParam, c.MemberName))));

			return Expression.Lambda<Func<TTarget, TSource, TTarget>>(body, targetParam, sourceParam);
		}
	}
}
