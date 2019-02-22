using LinqToDB.Data;
using LinqToDB.SqlProvider;
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

		// this is Oracle-specific operation
		protected override bool UpdateWithDeleteOperationSupported => true;

		// Oracle can have one insert and one update clause only
		protected override int MaxOperationsCount => 2;

		// there is no independent delete in Oracle merge
		protected override bool DeleteOperationSupported => false;

		// Oracle can have one insert and one update clause only
		protected override bool SameTypeOperationsAllowed => false;

		// VALUES(...) clause is not supported in MERGE source
		protected override bool SupportsSourceDirectValues => false;

		// table with exactly one record for client-side source generation
		// bad thing that user can change this table, but broken merge will be minor issue in this case
		protected override string FakeSourceTable => "dual";

		// dual table owner
		protected override string FakeSourceTableSchema => "sys";

		// Oracle doesn't support TABLE_ALIAS(COLUMN_ALIAS, ...) syntax
		protected override bool SupportsColumnAliasesInTableAlias => false;

		// oracle doesn't support INSERT FROM
		protected override bool ProviderUsesAlternativeUpdate => true;

		// It doesn't make sense to fix empty source generation as it will take too much effort for nothing
		protected override bool EmptySourceSupported => false;

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

		protected override bool MergeHintsSupported => true;

		protected override void BuildMergeInto()
		{
			Command
				.Append("MERGE ");

			if (Merge.Hint != null)
			{
				Command
					.Append("/*+ ")
					.Append(Merge.Hint)
					.Append(" */ ");
			}

			Command
				.Append("INTO ")
				.Append(TargetTableName)
				.Append(" ")
				.AppendLine((string)SqlBuilder.Convert(TargetAlias, ConvertType.NameToQueryTableAlias));
		}
	}
}
