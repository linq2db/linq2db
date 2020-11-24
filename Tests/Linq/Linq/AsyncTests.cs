using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;
	using System.Threading;
	using UserTests;

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

			using (var db = GetDataContext(context + ".LinqService"))
			{
				var list = await db.Parent.ToArrayAsync();
				Assert.That(list.Length, Is.Not.EqualTo(0));
			}
		}

		[Test]
		public void Test1([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context + ".LinqService"))
			{
				var list = db.Parent.ToArrayAsync().Result;
				Assert.That(list.Length, Is.Not.EqualTo(0));
			}
		}

		[Test]
		public async Task TestForEach([DataSources(false)] string context)
		{
			await TestForEachImpl(context);
		}

		async Task TestForEachImpl(string context)
		{
			using (var db = GetDataContext(context + ".LinqService"))
			{
				var list = new List<Parent>();

				await db.Parent.ForEachAsync(list.Add);

				Assert.That(list.Count, Is.Not.EqualTo(0));
			}
		}

		[Test]
		public async Task TestExecute1([DataSources(false)] string context)
		{
			await TestExecute1Impl(context);
		}

		async Task TestExecute1Impl(string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				conn.InlineParameters = true;

				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString()!;
				sql = string.Join(Environment.NewLine, sql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.Where(line => !line.StartsWith("-- Access")));

				var res = await conn.SetCommand(sql).ExecuteAsync<string>();

				Assert.That(res, Is.EqualTo("John"));
			}
		}

		[Test]
		public void TestExecute2([DataSources(false)] string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				conn.InlineParameters = true;

				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString()!;
				sql = string.Join(Environment.NewLine, sql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.Where(line => !line.StartsWith("-- Access")));

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
			using (var conn = new TestDataConnection(context))
			{
				conn.InlineParameters = true;

				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString()!;
				sql = string.Join(Environment.NewLine, sql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.Where(line => !line.StartsWith("-- Access")));

				using (var rd = await conn.SetCommand(sql).ExecuteReaderAsync())
				{
					var list = await rd.QueryToArrayAsync<string>();

					Assert.That(list[0], Is.EqualTo("John"));
				}
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

#if !NET472
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

		[ActiveIssue(Configuration = TestProvName.AllPostgreSQL, Details = "ngpsql 5.0.0 bug")]
		[Test]
		public void CancelableAsyncEnumerableTest([DataSources] string context)
		{
			using var cts = new CancellationTokenSource();
			var cancellationToken = cts.Token;
			cts.Cancel();
			using var db = GetDataContext(context);
			var resultQuery = db.Parent.AsAsyncEnumerable().WithCancellation(cancellationToken);
			var list = new List<Parent>();
			Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				try
				{
					await foreach (var row in resultQuery)
						list.Add(row);
				}
				catch (OperationCanceledException)
				{
					// this casts any exception that inherits from OperationCanceledException
					//   to a OperationCanceledException to pass the assert check above
					//   (needed for TaskCanceledException)
					throw new OperationCanceledException();
				}
			});
		}
#endif
	}
}
