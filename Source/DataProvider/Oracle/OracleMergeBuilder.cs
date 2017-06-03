using System;
using System.Linq.Expressions;
using LinqToDB.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	class OracleMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public OracleMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override bool UpdateWithDeleteOperationSupported
		{
			get
			{
				return true;
			}
		}

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
				return "dual";
			}
		}

		protected override string FakeSourceTableOwner
		{
			get
			{
				return "sys";
			}
		}

		protected override bool SupportsColumnAliasesInTableAlias
		{
			get
			{
				return false;
			}
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				return true;
			}
		}

		protected override void BuildUpdateWithDelete(
			Expression<Func<TTarget, TSource, bool>> updatePredicate,
			Expression<Func<TTarget, TSource, TTarget>> updateExpression,
			Expression<Func<TTarget, TSource, bool>> deletePredicate)
		{
			Command
				.AppendLine()
				.AppendLine("WHEN MATCHED THEN UPDATE");

			if (updateExpression != null)
				BuildCustomUpdate(updateExpression);
			else
				BuildDefaultUpdate();

			if (updatePredicate != null)
			{
				Command.Append(" WHERE ");
				BuildPredicateByTargetAndSource(updatePredicate);
			}

			Command.Append(" DELETE WHERE ");
			BuildPredicateByTargetAndSource(deletePredicate);
		}

		protected override void BuildInsert(
			Expression<Func<TSource, bool>> predicate,
			Expression<Func<TSource, TTarget>> create)
		{
			Command
				.AppendLine()
				.Append("WHEN NOT MATCHED THEN INSERT");

			if (create != null)
				BuildCustomInsert(create);
			else
				BuildDefaultInsert();

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				BuildSingleTablePredicate(predicate, SourceAlias, true);
			}
		}

		protected override void BuildDelete(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			GenerateFakeUpdate(predicate);

			Command
				.AppendLine()
				.Append(" DELETE");

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				BuildPredicateByTargetAndSource(predicate);
			}
		}

		private void GenerateFakeUpdate(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			Command
				.AppendLine()
				.AppendLine("WHEN MATCHED THEN UPDATE");

			var targetParam = Expression.Parameter(typeof(TTarget), "t");
			var sourceParam = Expression.Parameter(typeof(TSource), "s");

			var body = Expression.MemberInit(
				Expression.New(typeof(TTarget)),
				TargetDescriptor.Columns.Where(_ => !_.SkipOnUpdate && !_.IsIdentity)
				.Select(c => Expression.Bind(c.MemberInfo, Expression.PropertyOrField(targetParam, c.MemberName))));

			var update = Expression.Lambda<Func<TTarget, TSource, TTarget>>(body, targetParam, sourceParam);
			BuildCustomUpdate(update);

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				BuildPredicateByTargetAndSource(predicate);
			}
		}

		protected override void BuildUpdate(
			Expression<Func<TTarget, TSource, bool>> predicate,
			Expression<Func<TTarget, TSource, TTarget>> update)
		{
			Command
				.AppendLine()
				.AppendLine("WHEN MATCHED THEN UPDATE");

			if (update != null)
				BuildCustomUpdate(update);
			else
				BuildDefaultUpdate();

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				BuildPredicateByTargetAndSource(predicate);
			}
		}
	}
}
