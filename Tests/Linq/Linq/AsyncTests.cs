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
	using UserTests;

	[TestFixture]
	public class AsyncTests : TestBase
	{
		[Test]
		public void Test([DataSources(false)] string context)
		{
			TestImpl(context);
		}

		async void TestImpl(string context)
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
		public void TestForEach([DataSources(false)] string context)
		{
			TestForEachImpl(context);
		}

		async void TestForEachImpl(string context)
		{
			using (var db = GetDataContext(context + ".LinqService"))
			{
				var list = new List<Parent>();

				await db.Parent.ForEachAsync(list.Add);

				Assert.That(list.Count, Is.Not.EqualTo(0));
			}
		}

		[Test]
		public void TestExecute1([DataSources(false)] string context)
		{
			TestExecute1Impl(context);
		}

		async void TestExecute1Impl(string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString().Replace("-- Access", "");

				var res = await conn.SetCommand(sql).ExecuteAsync<string>();

				Assert.That(res, Is.EqualTo("John"));
			}
		}

		[Test]
		public void TestExecute2([DataSources(false)] string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString().Replace("-- Access", "");

				var res = conn.SetCommand(sql).ExecuteAsync<string>().Result;

				Assert.That(res, Is.EqualTo("John"));
			}
		}

		[Test]
		public void TestQueryToArray([DataSources(false)] string context)
		{
			TestQueryToArrayImpl(context);
		}

		async void TestQueryToArrayImpl(string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString().Replace("-- Access", "");

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
	}
}
