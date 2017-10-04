using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;

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
					() => MergeExtensions.Merge<Child>(null),

					() => MergeExtensions.MergeInto<Child, Child>(null, new FakeTable<Child>()),
					() => MergeExtensions.MergeInto<Child, Child>(new Child[0].AsQueryable(), null),

					() => MergeExtensions.Using<Child, Child>(null, new Child[0].AsQueryable()),
					() => MergeExtensions.Using<Child, Child>(new FakeMergeUsing<Child>(), null),

					() => MergeExtensions.Using<Child, Child>(null, new Child[0]),
					() => MergeExtensions.Using<Child, Child>(new FakeMergeUsing<Child>(), null),

					() => MergeExtensions.UsingTarget<Child>(null),

					() => MergeExtensions.On<Child, Child, int>(null, t => 1, s => 1),
					() => MergeExtensions.On<Child, Child, int>(new FakeMergeOn<Child, Child>(), null, s => 1),
					() => MergeExtensions.On<Child, Child, int>(new FakeMergeOn<Child, Child>(), t => 1, null),

					() => MergeExtensions.On<Child, Child>(null, (t, s) => true),
					() => MergeExtensions.On<Child, Child>(new FakeMergeOn<Child, Child>(), null),

					() => MergeExtensions.OnTargetKey<Child>(null),

					() => MergeExtensions.InsertWhenNotMatched<Child>(null),

					() => MergeExtensions.InsertWhenNotMatchedAnd<Child>(null, c => true),
					() => MergeExtensions.InsertWhenNotMatchedAnd<Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.InsertWhenNotMatched<Child, Child>(null, c => c),
					() => MergeExtensions.InsertWhenNotMatched<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.InsertWhenNotMatchedAnd<Child, Child>(null, c => true, c => c),
					() => MergeExtensions.InsertWhenNotMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null, c => c),
					() => MergeExtensions.InsertWhenNotMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), c => true, null),

					() => MergeExtensions.UpdateWhenMatched<Child>(null),

					() => MergeExtensions.UpdateWhenMatchedAnd<Child>(null, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedAnd<Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.UpdateWhenMatched<Child, Child>(null, (t, s) => t),
					() => MergeExtensions.UpdateWhenMatched<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.UpdateWhenMatchedAnd<Child, Child>(null, (t, s) => true, (t, s) => t),
					() => MergeExtensions.UpdateWhenMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => t),
					() => MergeExtensions.UpdateWhenMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null),

					() => MergeExtensions.UpdateWhenMatchedThenDelete<Child>(null, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedThenDelete<Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.UpdateWhenMatchedAndThenDelete<Child>(null, (t, s) => true, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedAndThenDelete<Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedAndThenDelete<Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null),

					() => MergeExtensions.UpdateWhenMatchedThenDelete<Child, Child>(null, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => t, null),

					() => MergeExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(null, (t, s) => true, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null, (t, s) => true),
					() => MergeExtensions.UpdateWhenMatchedAndThenDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, (t, s) => t, null),

					() => MergeExtensions.DeleteWhenMatched<Child, Child>(null),

					() => MergeExtensions.DeleteWhenMatchedAnd<Child, Child>(null, (t, s) => true),
					() => MergeExtensions.DeleteWhenMatchedAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.UpdateWhenNotMatchedBySource<Child, Child>(null, t => t),
					() => MergeExtensions.UpdateWhenNotMatchedBySource<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.UpdateWhenNotMatchedBySourceAnd<Child, Child>(null, t => true, t => t),
					() => MergeExtensions.UpdateWhenNotMatchedBySourceAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null, t => t),
					() => MergeExtensions.UpdateWhenNotMatchedBySourceAnd<Child, Child>(new FakeMergeSource<Child, Child>(), t => true, null),

					() => MergeExtensions.DeleteWhenNotMatchedBySource<Child, Child>(null),

					() => MergeExtensions.DeleteWhenNotMatchedBySourceAnd<Child, Child>(null, t => true),
					() => MergeExtensions.DeleteWhenNotMatchedBySourceAnd<Child, Child>(new FakeMergeSource<Child, Child>(), null),

					() => MergeExtensions.Merge<Child, Child>(null)
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.API.Null Parameters.{i}"));
			}
		}

		[TestCaseSource(nameof(_nullParameterCases))]
		public void MergeApiNullParameter(TestDelegate action)
		{
			Assert.Throws<ArgumentNullException>(action);
		}

		private class FakeMergeSource<TTarget, TSource> : IMergeableSource<TTarget, TSource>
		{ }

		private class FakeMergeUsing<TTarget> : IMergeableUsing<TTarget>
		{ }

		private class FakeMergeOn<TTarget, TSource> : IMergeableOn<TTarget, TSource>
		{ }

		private class FakeTable<TEntity> : ITable<TEntity>
		{
			IDataContext IExpressionQuery.DataContext
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Type IQueryable.ElementType
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Expression IExpressionQuery.Expression
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Expression IQueryable.Expression
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Expression IExpressionQuery<TEntity>.Expression
			{
				get
				{
					throw new NotImplementedException();
				}

				set
				{
					throw new NotImplementedException();
				}
			}

			IQueryProvider IQueryable.Provider
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			string IExpressionQuery.SqlText
			{
				get
				{
					throw new NotImplementedException();
				}
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

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
			{
				throw new NotImplementedException();
			}
		}
	}
}
