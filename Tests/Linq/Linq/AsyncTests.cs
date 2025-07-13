using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Internal.Async;

using NUnit.Framework;

using Tests.Model;
using Tests.UserTests;

namespace Tests.Linq
{
	[TestFixture]
	public class AsyncTests : TestBase
	{
		[Test]
		public async Task Test([DataSources(false)] string context)
		{
			await TestImpl(context);
		}

		async Task TestImpl(string context)
		{
			Test1(context);

			if (TestConfiguration.DisableRemoteContext)
				Assert.Inconclusive("Remote context disabled");

			using (var db = GetDataContext(context + LinqServiceSuffix))
			{
				var list = await db.Parent.ToArrayAsync();
				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test]
		public void Test1([DataSources(false)] string context)
		{
			if (TestConfiguration.DisableRemoteContext)
				Assert.Inconclusive("Remote context disabled");

			using (var db = GetDataContext(context + LinqServiceSuffix))
			{
				var list = db.Parent.ToArrayAsync().Result;
				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test]
		public async Task TestForEach([DataSources(false)] string context)
		{
			await TestForEachImpl(context);
		}

		async Task TestForEachImpl(string context)
		{
			if (TestConfiguration.DisableRemoteContext)
				Assert.Inconclusive("Remote context disabled");

			using (var db = GetDataContext(context + LinqServiceSuffix))
			{
				var list = new List<Parent>();

				await db.Parent.ForEachAsync(list.Add);

				Assert.That(list, Is.Not.Empty);
			}
		}

		[Test]
		public async Task TestExecute1([DataSources(false)] string context)
		{
			await TestExecute1Impl(context);
		}

		async Task TestExecute1Impl(string context)
		{
			using (var conn = GetDataConnection(context))
			{
				conn.InlineParameters = true;

				var sql = conn.Person
					.Where(p => p.ID == 1)
					.Select(p => p.FirstName)
					.Take(1)
					.ToSqlQuery().Sql;

				var res = await conn.SetCommand(sql).ExecuteAsync<string>();

				Assert.That(res, Is.EqualTo("John"));
			}
		}

		[Test]
		public void TestExecute2([DataSources(false)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				conn.InlineParameters = true;

				var sql = conn.Person
					.Where(p => p.ID == 1)
					.Select(p => p.FirstName)
					.Take(1)
					.ToSqlQuery().Sql;

				var res = conn.SetCommand(sql).ExecuteAsync<string>().Result;

				Assert.That(res, Is.EqualTo("John"));
			}
		}

		[Test]
		public async Task TestQueryToArray([DataSources(false)] string context)
		{
			await TestQueryToArrayImpl(context);
		}

		async Task TestQueryToArrayImpl(string context)
		{
			using (var conn = GetDataConnection(context))
			{
				conn.InlineParameters = true;

				var sql = conn.Person
					.Where(p => p.ID == 1)
					.Select(p => p.FirstName)
					.Take(1)
					.ToSqlQuery().Sql;

				await using (var rd = await conn.SetCommand(sql).ExecuteReaderAsync())
				{
					var list = await rd.QueryToArrayAsync<string>();

					Assert.That(list[0], Is.EqualTo("John"));
				}
			}
		}

		[Test]
		public async Task TestQueryToAsyncEnumerable([DataSources(false)] string context)
		{
			await TestQueryToAsyncEnumerableImpl(context);
		}

		async Task TestQueryToAsyncEnumerableImpl(string context)
		{
			using (var conn = GetDataConnection(context))
			{
				conn.InlineParameters = true;

				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToSqlQuery().Sql;

				var list = await AsyncEnumerableToListAsync(
					conn.SetCommand(sql)
						.QueryToAsyncEnumerable<string>());

				Assert.That(list[0], Is.EqualTo("John"));
			}
		}

		[Test]
		public async Task FirstAsyncTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var person = await db.Person.FirstAsync(p => p.ID == 1);

				Assert.That(person.ID, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task ContainsAsyncTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p = new Person { ID = 1 };

				var r = await db.Person.ContainsAsync(p);

				Assert.That(r, Is.True);
			}
		}

		[Test]
		public async Task TestFirstOrDefault([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var param = 4;
				var resultQuery =
						from o in db.Parent
						where Sql.Ext.In(o.ParentID, 1, 2, 3, (int?)null) || o.ParentID == param
						select o;

				var _ = await resultQuery.FirstOrDefaultAsync();
			}
		}

		[Test]
		public async Task TakeSkipTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var resultQuery = db.Parent.OrderBy(p => p.ParentID).Skip(1).Take(2);

				AreEqual(
					resultQuery.ToArray(),
					await resultQuery.ToArrayAsync());
			}
		}

		[Test]
		public async Task AsAsyncEnumerable1Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var resultQuery = db.Parent.AsAsyncEnumerable();
			var list = new List<Parent>();
			await foreach (var row in resultQuery)
				list.Add(row);

			AreEqual(Parent, list);
		}

		[Test]
		public async Task AsAsyncEnumerable2Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var resultQuery = db.Parent.Where(x => x.ParentID > 1).AsAsyncEnumerable();
			var list = new List<Parent>();
			await foreach (var row in resultQuery)
				list.Add(row);

			AreEqual(Parent.Where(x => x.ParentID > 1), list);
		}

		[Test]
		public async Task AsyncEnumerableCast1Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var resultQuery = (IAsyncEnumerable<Parent>)db.Parent;
			var list = new List<Parent>();
			await foreach (var row in resultQuery)
				list.Add(row);

			AreEqual(Parent, list);
		}

		[Test]
		public async Task AsyncEnumerableCast2Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var resultQuery = (IAsyncEnumerable<Parent>)db.Parent.Where(x => x.ParentID > 1);
			var list = new List<Parent>();
			await foreach (var row in resultQuery)
				list.Add(row);

			AreEqual(Parent.Where(x => x.ParentID > 1), list);
		}

		[Test]
		public void CancellableAsyncEnumerableTest([DataSources] string context)
		{
			using var cts = new CancellationTokenSource();
			var cancellationToken = cts.Token;
			cts.Cancel();

			using var db = GetDataContext(context);

			var resultQuery = db.Parent.AsAsyncEnumerable().WithCancellation(cancellationToken);

			Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				try
				{
					await foreach (var row in resultQuery)
					{ }
				}
				catch (OperationCanceledException)
				{
					// this casts any exception that inherits from OperationCanceledException
					//   to a OperationCanceledException to pass the assert check above
					//   (needed for TaskCanceledException)
					throw new OperationCanceledException();
				}
				catch (Exception ex) when (ex.Message.Contains("ORA-01013") && context.IsAnyOf(TestProvName.AllOracleManaged))
				{
					// ~Aliens~ Oracle
					throw new OperationCanceledException();
				}
			});
		}

		[Test]
		public async Task ToLookupAsyncTest([DataSources] string context)
		{
			await using var db = GetDataContext(context);

			var q =
				from c in db.Child
				orderby c.ParentID, c.ChildID
				select c;

			var g1 = (await q.ToListAsync()).ToLookup(c => c.ParentID);
			var g2 = await db.Child.ToLookupAsync(c => c.ParentID);

			Assert.That(g1, Has.Count.EqualTo(g2.Count));

			foreach (var g in g1)
				AreEqual(g1[g.Key], g2[g.Key]);
		}

		[Test]
		public async Task ToLookupElementAsyncTest([DataSources] string context)
		{
			await using var db = GetDataContext(context);

			var q =
				from c in db.Child
				orderby c.ParentID, c.ChildID
				select c;

			var g1 = (await q.ToListAsync()).ToLookup(c => c.ParentID, c => c.ChildID);
			var g2 = await db.Child.ToLookupAsync(c => c.ParentID, c => c.ChildID);

			Assert.That(g1, Has.Count.EqualTo(g2.Count));

			foreach (var g in g1)
				AreEqual(g1[g.Key], g2[g.Key]);
		}
	}
}
