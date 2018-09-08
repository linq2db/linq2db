using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Async;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	public partial class MergeTests
	{
		public static IEnumerable<TestCaseData> _nullParameterCases
		{
			get
			{
				return new TestDelegate[]
				{
					() => LinqExtensions.Merge<Child>(null),

					() => LinqExtensions.Merge<Child>(null, "hint"),
					() => LinqExtensions.Merge<Child>(new FakeTable<Child>(), null),

					() => LinqExtensions.MergeInto<Child, Child>(null, new FakeTable<Child>()),
					() => LinqExtensions.MergeInto<Child, Child>(new Child[0].AsQueryable(), null),

					() => LinqExtensions.MergeInto<Child, Child>(null, new FakeTable<Child>(), "hint"),
					() => LinqExtensions.MergeInto<Child, Child>(new Child[0].AsQueryable(), null, "hint"),
					() => LinqExtensions.MergeInto<Child, Child>(new Child[0].AsQueryable(), new FakeTable<Child>(), null),

					() => LinqExtensions.Using<Child, Child>(null, new Child[0].AsQueryable()),
					() => LinqExtensions.Using<Child, Child>(new FakeMergeUsing<Child>(), null),

					() => LinqExtensions.Using<Child, Child>(null, new Child[0]),
					() => LinqExtensions.Using<Child, Child>(new FakeMergeUsing<Child>(), null),

					() => LinqExtensions.UsingTarget<Child>(null),

					() => LinqExtensions.On<Child, Child, int>(null, t => 1, s => 1),
					() => LinqExtensions.On<Child, Child, int>(new FakeMergeOn<Child, Child>(), null, s => 1),
					() => LinqExtensions.On<Child, Child, int>(new FakeMergeOn<Child, Child>(), t => 1, null),

					() => LinqExtensions.On<Child, Child>(null, (t, s) => true),
					() => LinqExtensions.On<Child, Child>(new FakeMergeOn<Child, Child>(), null),

					() => LinqExtensions.OnTargetKey<Child>(null),

					() => LinqExtensions.InsertWhenNotMatched<Child>(null),

					() => LinqExtensions.InsertWhenNotMatchedAnd<Child>(null, c => true),
					() => LinqExtensions.InsertWhenNotMatchedAnd<Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.InsertWhenNotMatched<Child, Child>(null, c => c),
					() => LinqExtensions.InsertWhenNotMatched<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.InsertWhenNotMatchedAnd<Child, Child>(null, c => true, c => c),
					() => LinqExtensions.InsertWhenNotMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null, c => c),
					() => LinqExtensions.InsertWhenNotMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), c => true, null),

					() => LinqExtensions.UpdateWhenMatched<Child>(null),

					() => LinqExtensions.UpdateWhenMatchedAnd<Child>(null, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedAnd<Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.UpdateWhenMatched<Child, Child>(null, (t, s) => t),
					() => LinqExtensions.UpdateWhenMatched<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.UpdateWhenMatchedAnd<Child, Child>(null, (t, s) => true, (t, s) => t),
					() => LinqExtensions.UpdateWhenMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => t),
					() => LinqExtensions.UpdateWhenMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null),

					() => LinqExtensions.UpdateWhenMatchedThenDelete<Child>(null, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedThenDelete<Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.UpdateWhenMatchedAndThenDelete<Child>(null, (t, s) => true, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedAndThenDelete<Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedAndThenDelete<Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null),

					() => LinqExtensions.UpdateWhenMatchedThenDelete<Child, Child>(null, (t, s) => t, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => t, null),

					() => LinqExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(null, (t, s) => true, (t, s) => t, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => t, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null, (t, s) => true),
					() => LinqExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, (t, s) => t, null),

					() => LinqExtensions.DeleteWhenMatched<Child, Child>(null),

					() => LinqExtensions.DeleteWhenMatchedAnd<Child, Child>(null, (t, s) => true),
					() => LinqExtensions.DeleteWhenMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.UpdateWhenNotMatchedBySource<Child, Child>(null, t => t),
					() => LinqExtensions.UpdateWhenNotMatchedBySource<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.UpdateWhenNotMatchedBySourceAnd<Child, Child>(null, t => true, t => t),
					() => LinqExtensions.UpdateWhenNotMatchedBySourceAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null, t => t),
					() => LinqExtensions.UpdateWhenNotMatchedBySourceAnd<Child, Child>(new FakeMergeSource<Child, Child>(), t => true, null),

					() => LinqExtensions.DeleteWhenNotMatchedBySource<Child, Child>(null),

					() => LinqExtensions.DeleteWhenNotMatchedBySourceAnd<Child, Child>(null, t => true),
					() => LinqExtensions.DeleteWhenNotMatchedBySourceAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => LinqExtensions.Merge<Child, Child>(null)
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.API.Null Parameters.{i}"));
			}
		}

		[TestCaseSource(nameof(_nullParameterCases))]
		public void MergeApiNullParameter(TestDelegate action)
		{
			Assert.Throws<ArgumentNullException>(action);
		}

		class FakeMergeSource<TTarget, TSource> : IMergeableSource<TTarget, TSource>
		{ }

		class FakeMergeUsing<TTarget> : IMergeableUsing<TTarget>
		{ }

		class FakeMergeOn<TTarget, TSource> : IMergeableOn<TTarget, TSource>
		{ }

		class FakeTable<TEntity> : ITable<TEntity>
		{
			IDataContext   IExpressionQuery.DataContext => throw new NotImplementedException();
			Expression     IExpressionQuery.Expression  => throw new NotImplementedException();
			string         IExpressionQuery.SqlText     => throw new NotImplementedException();
			Type           IQueryable.      ElementType => throw new NotImplementedException();
			Expression     IQueryable.      Expression  => throw new NotImplementedException();
			IQueryProvider IQueryable.      Provider    => throw new NotImplementedException();

			Expression IExpressionQuery<TEntity>.Expression
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			IQueryable IQueryProvider.CreateQuery(Expression expression)
			{
				throw new NotImplementedException();
			}

			IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
			{
				throw new NotImplementedException();
			}

			object IQueryProvider.Execute(Expression expression)
			{
				throw new NotImplementedException();
			}

			TResult IQueryProvider.Execute<TResult>(Expression expression)
			{
				throw new NotImplementedException();
			}

			Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken token)
			{
				throw new NotImplementedException();
			}

			IAsyncEnumerable<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression)
			{
				throw new NotImplementedException();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			public string DatabaseName { get; }
			public string SchemaName   { get; }
			public string TableName    { get; }

			public string GetTableName()
			{
				throw new NotImplementedException();
			}
		}
	}
}
