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

		protected override void GenerateUpdateWithDelete(
			Expression<Func<TTarget, TSource, bool>> updatePredicate,
			Expression<Func<TTarget, TSource, TTarget>> updateExpression,
			Expression<Func<TTarget, TSource, bool>> deletePredicate)
		{
			Command
				.AppendLine("WHEN MATCHED THEN UPDATE");

			if (updateExpression != null)
				GenerateCustomUpdate(updateExpression);
			else
				GenerateDefaultUpdate();

			if (updatePredicate != null)
			{
				Command.Append(" WHERE ");
				GeneratePredicateByTargetAndSource(updatePredicate);
			}

			Command.Append(" DELETE WHERE ");
			GeneratePredicateByTargetAndSource(deletePredicate);
		}

		protected override void GenerateInsert(
			Expression<Func<TSource, bool>> predicate,
			Expression<Func<TSource, TTarget>> create)
		{
			Command
				.Append("WHEN NOT MATCHED THEN INSERT");

			if (create != null)
				GenerateCustomInsert(create);
			else
				GenerateDefaultInsert();

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				GenerateSingleTablePredicate(predicate, SourceAlias, true);
			}
		}

		protected override void GenerateDelete(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			GenerateFakeUpdate(predicate);

			Command.Append(" DELETE");

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				GeneratePredicateByTargetAndSource(predicate);
			}
		}

		private void GenerateFakeUpdate(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			Command
				.AppendLine("WHEN MATCHED THEN UPDATE");

			var targetParam = Expression.Parameter(typeof(TTarget), "t");
			var sourceParam = Expression.Parameter(typeof(TSource), "s");

			var body = Expression.MemberInit(
				Expression.New(typeof(TTarget)),
				TargetDescriptor.Columns.Where(_ => !_.SkipOnUpdate && !_.IsIdentity)
				.Select(c => Expression.Bind(c.MemberInfo, Expression.PropertyOrField(targetParam, c.MemberName))));

			var update = Expression.Lambda<Func<TTarget, TSource, TTarget>>(body, targetParam, sourceParam);
			GenerateCustomUpdate(update);

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				GeneratePredicateByTargetAndSource(predicate);
			}
		}

		protected override void GenerateUpdate(
			Expression<Func<TTarget, TSource, bool>> predicate,
			Expression<Func<TTarget, TSource, TTarget>> update)
		{
			Command
				.AppendLine()
				.AppendLine("WHEN MATCHED THEN UPDATE");

			if (update != null)
				GenerateCustomUpdate(update);
			else
				GenerateDefaultUpdate();

			if (predicate != null)
			{
				Command.Append(" WHERE ");
				GeneratePredicateByTargetAndSource(predicate);
			}
		}
	}
}
