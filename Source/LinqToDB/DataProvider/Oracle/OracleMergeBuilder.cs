using LinqToDB.Data;
using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.Oracle
{
	class OracleMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public OracleMergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		protected override bool UpdateWithDeleteOperationSupported
		{
			get
			{
				// this is Oracle-specific operation
				return true;
			}
		}

		protected override int MaxOperationsCount
		{
			get
			{
				// Oracle can have one insert and one update clause only
				return 2;
			}
		}

		protected override bool DeleteOperationSupported
		{
			get
			{
				// there is no independent delete in Oracle merge
				return false;
			}
		}

		protected override bool SameTypeOperationsAllowed
		{
			get
			{
				// Oracle can have one insert and one update clause only
				return false;
			}
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				// VALUES(...) clause is not supported in MERGE source
				return false;
			}
		}

		protected override string FakeSourceTable
		{
			get
			{
				// table with exactly one record for client-side source generation
				// bad thing that user can change this table, but broken merge will be minor issue in this case
				return "dual";
			}
		}

		protected override string FakeSourceTableSchema
		{
			get
			{
				// dual table owner
				return "sys";
			}
		}

		protected override bool SupportsColumnAliasesInTableAlias
		{
			get
			{
				// Oracle doesn't support TABLE_ALIAS(COLUMN_ALIAS, ...) syntax
				return false;
			}
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				// oracle doesn't support INSERT FROM
				return true;
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
				.AppendLine("WHEN NOT MATCHED THEN")
				.Append("INSERT")
				;

			if (create != null)
				BuildCustomInsert(create);
			else
				BuildDefaultInsert();

			if (predicate != null)
			{
				Command
					.AppendLine("WHERE")
					.Append("\t")
					;
				BuildSingleTablePredicate(predicate, SourceAlias, true);
			}
		}

		protected override void BuildUpdate(
			Expression<Func<TTarget, TSource, bool>> predicate,
			Expression<Func<TTarget, TSource, TTarget>> update)
		{
			Command
				.AppendLine()
				.AppendLine("WHEN MATCHED THEN")
				.AppendLine("UPDATE")
				;

			if (update != null)
				BuildCustomUpdate(update);
			else
				BuildDefaultUpdate();

			if (predicate != null)
			{
				Command
					.AppendLine("WHERE")
					.Append("\t")
					;
				BuildPredicateByTargetAndSource(predicate);
			}
		}
	}
}
