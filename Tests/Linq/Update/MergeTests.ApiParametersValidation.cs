using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Tests.Model;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		public static IEnumerable<TestCaseData> _nullParameterCases
		{
			get
			{
				return new TestDelegate[]
				{
					() => MergeExtensions.From<Child, Child>(null, new Child[0], (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), (IEnumerable<Child>)null, (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), new Child[0], null),

					() => MergeExtensions.From<Child, Child>(null, new Child[0].AsQueryable(), (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), (IQueryable<Child>)null, (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), new Child[0].AsQueryable(), null),

					() => MergeExtensions.FromSame<Child>(null, new Child[0]),
					() => MergeExtensions.FromSame<Child>(new FakeTable<Child>(), (IEnumerable<Child>)null),

					() => MergeExtensions.FromSame<Child>(null, new Child[0].AsQueryable()),
					() => MergeExtensions.FromSame<Child>(new FakeTable<Child>(), (IQueryable<Child>)null),

					() => MergeExtensions.FromSame<Child>(null, new Child[0], (t, s) => true),
					() => MergeExtensions.FromSame<Child>(new FakeTable<Child>(), (IEnumerable<Child>)null, (t, s) => true),
					() => MergeExtensions.FromSame<Child>(new FakeTable<Child>(), new Child[0], null),

					() => MergeExtensions.FromSame<Child>(null, new Child[0].AsQueryable(), (t, s) => true),
					() => MergeExtensions.FromSame<Child>(new FakeTable<Child>(), (IQueryable<Child>)null, (t, s) => true),
					() => MergeExtensions.FromSame<Child>(new FakeTable<Child>(), new Child[0].AsQueryable(), null),

					() => MergeExtensions.Insert<Child>(null),

					() => MergeExtensions.Insert<Child>(null, c => true),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, bool>>)null),

					() => MergeExtensions.Insert<Child>(null, c => c),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.Insert<Child>(null, c => true, c => c),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), null, c => c),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), c => true, null),

					() => MergeExtensions.Insert<Child, Child>(null, c => c),
					() => MergeExtensions.Insert<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.Insert<Child, Child>(null, c => true, c => c),
					() => MergeExtensions.Insert<Child, Child>(new FakeMergeSource<Child, Child>(), null, c => c),
					() => MergeExtensions.Insert<Child, Child>(new FakeMergeSource<Child, Child>(), c => true, null),

					() => MergeExtensions.Update<Child>(null),

					() => MergeExtensions.Update<Child>(null, (t, s) => true),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.Update<Child>(null, (t, s) => t),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, Child>>)null),

					() => MergeExtensions.Update<Child>(null, (t, s) => true, (t, s) => t),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), null, (t, s) => t),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), (t, s) => true, null),

					() => MergeExtensions.Update<Child, Child>(null, (t, s) => t),
					() => MergeExtensions.Update<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child, Child>>)null),

					() => MergeExtensions.Update<Child, Child>(null, (t, s) => true, (t, s) => t),
					() => MergeExtensions.Update<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => t),
					() => MergeExtensions.Update<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null),

					() => MergeExtensions.UpdateWithDelete<Child>(null, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.UpdateWithDelete<Child>(null, (t, s) => true, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, bool>>)null, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), (t, s) => true, (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.UpdateWithDelete<Child>(null, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, Child>>)null, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), (t, s) => t, (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.UpdateWithDelete<Child>(null, (t, s) => true, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), null, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), (t, s) => true, null, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child>(new FakeMergeSource<Child>(), (t, s) => true, (t, s) => t, (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.UpdateWithDelete<Child, Child>(null, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child, Child>>)null, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => t, (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.UpdateWithDelete<Child, Child>(null, (t, s) => true, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => t, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null, (t, s) => true),
					() => MergeExtensions.UpdateWithDelete<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, (t, s) => t, (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.Delete<Child>(null),

					() => MergeExtensions.Delete<Child>(null, (t, s) => true),
					() => MergeExtensions.Delete<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.Delete<Child, Child>(null),

					() => MergeExtensions.Delete<Child, Child>(null, (t, s) => true),
					() => MergeExtensions.Delete<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.UpdateBySource<Child>(null, t => t),
					() => MergeExtensions.UpdateBySource<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.UpdateBySource<Child>(null, t => true, t => t),
					() => MergeExtensions.UpdateBySource<Child>(new FakeMergeSource<Child>(), null, t => t),
					() => MergeExtensions.UpdateBySource<Child>(new FakeMergeSource<Child>(), t => true, null),

					() => MergeExtensions.UpdateBySource<Child, Child>(null, t => t),
					() => MergeExtensions.UpdateBySource<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.UpdateBySource<Child, Child>(null, t => true, t => t),
					() => MergeExtensions.UpdateBySource<Child, Child>(new FakeMergeSource<Child, Child>(), null, t => t),
					() => MergeExtensions.UpdateBySource<Child, Child>(new FakeMergeSource<Child, Child>(), t => true, null),

					() => MergeExtensions.DeleteBySource<Child>(null),

					() => MergeExtensions.DeleteBySource<Child>(null, t => true),
					() => MergeExtensions.DeleteBySource<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, bool>>)null),

					() => MergeExtensions.DeleteBySource<Child, Child>(null),

					() => MergeExtensions.DeleteBySource<Child, Child>(null, t => true),
					() => MergeExtensions.DeleteBySource<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, bool>>)null),

					() => MergeExtensions.Merge<Child>(null),

					() => MergeExtensions.Merge<Child, Child>(null)
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.API.Null Parameters.{i}"));
			}
		}

		[TestCaseSource(nameof(_nullParameterCases))]
		public void MergeApiNullParameter(TestDelegate action)
		{
			Assert.Throws<ArgumentNullException>(action);
		}

		private class FakeMergeSource<TTarget, TSource> : IMergeSource<TTarget, TSource>
		{ }

		private class FakeMergeSource<TEntity> : IMergeSource<TEntity>
		{ }

		private class FakeTable<TEntity> : ITable<TEntity>
		{
			IDataContextInfo IExpressionQuery.DataContextInfo
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
