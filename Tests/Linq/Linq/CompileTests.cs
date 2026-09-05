using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools.EntityServices;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class CompileTests : TestBase
	{
		[Test]
		public void CompiledTest1([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, string n1, int n2) =>
				n1 + n2);

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, "1", 1), Is.EqualTo("11"));
					Assert.That(query(db, "2", 2), Is.EqualTo("22"));
				}
			}
		}

		[Test]
		public void CompiledTest2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).Take(n));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1).ToList(), Has.Count.EqualTo(1));
					Assert.That(query(db, 2).ToList(), Has.Count.EqualTo(2));
				}
			}
		}

		[Test]
		public void CompiledTest3([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.GetTable<Child>().Where(c => c.ParentID == n).Take(n));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1).ToList(), Has.Count.EqualTo(1));
					Assert.That(query(db, 2).ToList(), Has.Count.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task CompiledTest3Async([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.GetTable<Child>().Where(c => c.ParentID == n).Take(n).ToListAsync(default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That((await query(db, 1)), Has.Count.EqualTo(1));
					Assert.That((await query(db, 2)), Has.Count.EqualTo(2));
				}
			}
		}

		[Test]
		public void CompiledTest4([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int[] n) =>
				db.GetTable<Child>().Where(c => n.Contains(c.ParentID)));

			using (var db = GetDataContext(context))
				Assert.That(query(db, new[] { 1, 2 }).ToList(), Has.Count.EqualTo(3));
		}

		[Test]
		public void CompiledTest5([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, object?[] ps) =>
				db.Parent.Where(p => p.ParentID == (int)ps[0]! && p.Value1 == (int?)ps[1]));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, new object[] { 1, 1 }).ToList(), Has.Count.EqualTo(1));
					Assert.That(query(db, new object?[] { 2, null }).ToList(), Has.Count.EqualTo(1));
				}
			}
		}

		[Test]
		public void CompiledTable1([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db) =>
				db.Child);

			using (var db = GetDataContext(context))
			{
				var _ = query(db).ToList().Count;
			}
		}

		[Test]
		public void CompiledTable2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db) =>
				db.GetTable<Child>());

			using (var db = GetDataContext(context))
				query(db).ToList();
		}

		[Test, Order(100)]
		public void ConcurrentTest1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (new DisableBaseline("Multi-threading"))
			{
				var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.GetTable<Parent>().Where(p => p.ParentID == n).First().ParentID);

				const int count = 100;

				var threads = new Task[count];
				var results = new int [count, 2];

				for (var i = 0; i < count; i++)
				{
					var n = i;

					threads[i] = Task.Run(() =>
					{
						using var db = GetDataContext(context);
						var id = (n % 6) + 1;
						results[n, 0] = id;
						results[n, 1] = query(db, id);
					});
				}

				Task.WaitAll(threads);

				for (var i = 0; i < count; i++)
					Assert.That(results[i, 1], Is.EqualTo(results[i, 0]));
			}
		}

		[Test, Order(100)]
		public void ConcurrentTestWithOptmization([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (new DisableBaseline("Multi-threading"))
			{
				var query = CompiledQuery.Compile((ITestDataContext db, int n, int n2) =>
					db.GetTable<Parent>().Where(p => p.ParentID == n && n == n2).First().ParentID);

				const int count = 100;

				var threads = new Task[count];
				var results = new int [count, 2];

				for (var i = 0; i < count; i++)
				{
					var n = i;

					threads[i] = Task.Run(() =>
					{
						using var db = GetDataContext(context);
						var id = (n % 6) + 1;
						results[n, 0] = id;
						results[n, 1] = query(db, id, id);
					});
				}

				Task.WaitAll(threads);

				for (var i = 0; i < count; i++)
					Assert.That(results[i, 1], Is.EqualTo(results[i, 0]));
			}
		}

		[Test]
		public void ConcurrentTest2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (new DisableBaseline("Multi-threading"))
			{
				var threads = new Task[100];
				var results = new int [100,2];

				for (var i = 0; i < 100; i++)
				{
					var n = i;

					threads[i] = Task.Run(() =>
					{
						using var db = GetDataContext(context);
						var id = (n % 6) + 1;
						results[n, 0] = id;
						results[n, 1] = db.Parent.Where(p => p.ParentID == id).First().ParentID;
					});
				}

				Task.WaitAll(threads);

				for (var i = 0; i < 100; i++)
					Assert.That(results[i, 1], Is.EqualTo(results[i, 0]));
			}
		}

		[Test]
		public void ConcurrentTest3([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (new DisableBaseline("Multi-threading"))
			{
				var threadCount = 100;

				var threads = new Task[threadCount];
				var results = new int [threadCount,2];

				for (var i = 0; i < threadCount; i++)
				{
					var n = i;

					threads[i] = Task.Run(() =>
					{
						using var db = GetDataContext(context);
						var id = (n % 6) + 1;
						results[n, 0] = id;
						results[n, 1] = db.Parent.Where(p => p.ParentID == id && id >= 0).First().ParentID;
					});
				}

				Task.WaitAll(threads);

				for (var i = 0; i < threadCount; i++)
					Assert.That(results[i, 1], Is.EqualTo(results[i, 0]));
			}
		}

		[Test]
		public void ParamTest1([DataSources] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Child>>((db, id) =>
				from c in db.Child
				where c.ParentID == id
				select new Child
				{
					ParentID = id,
					ChildID  = c.ChildID
				});

			using (var db = GetDataContext(context))
				Assert.That(query(db, 2).ToList(), Has.Count.EqualTo(2));
		}

		[Test]
		public void ElementTest1([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).First());

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1).ParentID, Is.EqualTo(1));
					Assert.That(query(db, 2).ParentID, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task ElementTestAsync1([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).FirstAsync(default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That((await query(db, 1)).ParentID, Is.EqualTo(1));
					Assert.That((await query(db, 2)).ParentID, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task ElementTestAsync2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.FirstAsync(c => c.ParentID == n, default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That((await query(db, 1)).ParentID, Is.EqualTo(1));
					Assert.That((await query(db, 2)).ParentID, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public void CompiledQueryWithExpressionMethodTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query = CompiledQuery.Compile((ITestDataContext xdb, int id) => Filter(xdb, id).FirstOrDefault());

			query(db, 1);
		}

		[Test]
		public async Task CompiledQueryWithExpressionMethoAsyncdTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query = CompiledQuery.Compile((ITestDataContext xdb, int id) => Filter(xdb, id).FirstOrDefaultAsync(default));

			await query(db, 1);
		}

		[ExpressionMethod(nameof(FilterExpression))]
		private static IQueryable<Parent> Filter(ITestDataContext db, int date)
		{
			throw new NotImplementedException();
		}

		static Expression<Func<ITestDataContext,int,IQueryable<Parent>>> FilterExpression()
		{
			return (db, id) =>
				from x in db.GetTable<Parent>()
				where x.ParentID == id
				orderby x.ParentID descending
				select x;
		}

		[Test]
		public void ContainsTest([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Select(c => c.ParentID).Contains(n));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1), Is.True);
					Assert.That(query(db, -1), Is.False);
				}
			}
		}

		[Test]
		public async Task ContainsTestAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Select(c => c.ParentID).ContainsAsync(n, default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await query(db, 1), Is.True);
					Assert.That(await query(db, -1), Is.False);
				}
			}
		}

		[Test]
		public void AnyTest([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Any(c => c.ParentID == n));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1), Is.True);
					Assert.That(query(db, -1), Is.False);
				}
			}
		}

		[Test]
		public async Task AnyTestAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.AnyAsync(c => c.ParentID == n, default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await query(db, 1), Is.True);
					Assert.That(await query(db, -1), Is.False);
				}
			}
		}

		[Test]
		public void AnyTest2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).Any());

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1), Is.True);
					Assert.That(query(db, -1), Is.False);
				}
			}
		}

		[Test]
		public async Task AnyTestAsync2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).AnyAsync(default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await query(db, 1), Is.True);
					Assert.That(await query(db, -1), Is.False);
				}
			}
		}

		[Test]
		public void CountTest([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Count(c => c.ParentID == n));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1), Is.EqualTo(1));
					Assert.That(query(db, -1), Is.Zero);
				}
			}
		}

		[Test]
		public async Task CountTestAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.LongCountAsync(c => c.ParentID == n, default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await query(db, 1), Is.EqualTo(1L));
					Assert.That(await query(db, -1), Is.Zero);
				}
			}
		}

		[Test]
		public void CountTest2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).Count());

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1), Is.EqualTo(1));
					Assert.That(query(db, -1), Is.Zero);
				}
			}
		}

		[Test]
		public async Task CountTestAsync2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).CountAsync(default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await query(db, 1), Is.EqualTo(1));
					Assert.That(await query(db, -1), Is.Zero);
				}
			}
		}

		[Test]
		public void MaxTest([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).Max(p => (int?)p.ParentID));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1), Is.EqualTo(1));
					Assert.That(query(db, -1), Is.Null);
				}
			}
		}

		[Test]
		public async Task MaxTestAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).MaxAsync(p => (int?)p.ParentID, default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await query(db, 1), Is.EqualTo(1));
					Assert.That(await query(db, -1), Is.Null);
				}
			}
		}

		[Test]
		public void MaxTest2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).Select(p => (int?)p.ParentID).Max());

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query(db, 1), Is.EqualTo(1));
					Assert.That(query(db, -1), Is.Null);
				}
			}
		}

		[Test]
		public async Task MaxTestAsync2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).Select(p => (int?)p.ParentID).MaxAsync(default));

			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await query(db, 1), Is.EqualTo(1));
					Assert.That(await query(db, -1), Is.Null);
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4365")]
		public void IDataContext_CompiledQueryTest_AsList([DataSources(false)] string context)
		{
			using var db  = new TestDataConnection(context);
			using var map = new IdentityMap(db);

			var query = CompiledQuery.Compile(static (TestDataConnection db) => db.Person.Where(p => p.ID == 1).ToList());

			var result1 = query(db);
			var result2 = query(db);

			Assert.That(result2[0], Is.SameAs(result1[0]));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4365")]
		public void CustomContext_CompiledQueryCustomTest_AsList([DataSources(false)] string context)
		{
			using var db  = new TestDataCustomConnection(context);
			using var map = new IdentityMap(db);

			var query = CompiledQuery.Compile(static (TestDataCustomConnection db) => db.Person.Where(p => p.ID == 1).ToList());

			var result1 = query(db);
			var result2 = query(db);

			Assert.That(result2[0], Is.SameAs(result1[0]));
		}

		[Test]
		public void IDataContext_CompiledQueryTest([DataSources(false)] string context)
		{
			using var db  = new TestDataConnection(context);
			using var map = new IdentityMap(db);

			var query = CompiledQuery.Compile(static (TestDataConnection db) => db.Person.Where(p => p.ID == 1));

			var result1 = query(db).ToList();
			var result2 = query(db).ToList();

			Assert.That(result2[0], Is.SameAs(result1[0]));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4365")]
		public void CustomContext_CompiledQueryCustomTest([DataSources(false)] string context)
		{
			using var db  = new TestDataCustomConnection(context);
			using var map = new IdentityMap(db);

			var query = CompiledQuery.Compile(static (TestDataCustomConnection db) => db.Person.Where(p => p.ID == 1));

			var result1 = query(db).ToList();
			var result2 = query(db).ToList();

			Assert.That(result2[0], Is.SameAs(result1[0]));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		public void LoadWithTest([DataSources] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id && Sql.CurrentTimestamp > TestData.Date)
					.LoadWith(p => p.Children));

			using var db = GetDataContext(context);

			var parent = query(db, 1).First();
			var other  = query(db, 2).First();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parent.ParentID, Is.EqualTo(1));
				Assert.That(parent.Children, Has.Count.EqualTo(1));
				Assert.That(other.ParentID,  Is.EqualTo(2));
				Assert.That(other.Children,  Has.Count.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		public void LoadWithThenLoadTest([DataSources] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.ThenLoad(c => c.GrandChildren));

			using var db = GetDataContext(context);

			var parent = query(db, 1).First();
			var other  = query(db, 2).First();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parent.ParentID,                 Is.EqualTo(1));
				Assert.That(parent.Children,                 Has.Count.EqualTo(1));
				Assert.That(parent.Children[0].GrandChildren, Has.Count.EqualTo(1));
				Assert.That(other.ParentID,                  Is.EqualTo(2));
				Assert.That(other.Children,                  Has.Count.EqualTo(2));
				Assert.That(other.Children[0].GrandChildren,  Has.Count.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		public void MultipleLoadWithTest([DataSources] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.LoadWith(p => p.GrandChildren));

			using var db = GetDataContext(context);

			var parent = query(db, 1).First();
			var other  = query(db, 2).First();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parent.ParentID,      Is.EqualTo(1));
				Assert.That(parent.Children,      Has.Count.EqualTo(1));
				Assert.That(parent.GrandChildren, Has.Count.EqualTo(1));
				Assert.That(other.ParentID,       Is.EqualTo(2));
				Assert.That(other.Children,       Has.Count.EqualTo(2));
				Assert.That(other.GrandChildren,  Has.Count.EqualTo(4));
			}
		}

		[Test]
		public void CompiledLoadWithInferredResultTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// TResult infers to ILoadWithQueryable<,>, which the folded compiled table cannot satisfy -
			// it substitutes a Table<T>. Pinned here so the failure stays a diagnosable one.
			var query = CompiledQuery.Compile((ITestDataContext db, int id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children));

			using var db = GetDataContext(context);

			var ex = Assert.Throws<LinqToDBException>(() => query(db, 1));

			using (Assert.EnterMultipleScope())
			{
				// Both halves matter: the declared type is what tells the reader which annotation to change,
				// and the advice is what tells them what to change it to.
				Assert.That(ex!.Message, Contains.Substring("ILoadWithQueryable"));
				Assert.That(ex.Message,  Contains.Substring("IQueryable<T>"));
			}
		}

		[Table]
		sealed class CompiledOutputTable
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int Value { get; set; }
		}

		[Test]
		public void CompiledDeleteWithOutputTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<CompiledOutputTable>>(static (db, id) =>
				db.GetTable<CompiledOutputTable>()
					.Where(t => t.Id == id)
					.DeleteWithOutput());

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[]
			{
				new CompiledOutputTable { Id = 1, Value = 10 },
				new CompiledOutputTable { Id = 2, Value = 20 },
			});

			var deleted = query(db, 1).ToArray();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(deleted,          Has.Length.EqualTo(1));
				Assert.That(deleted[0].Value, Is.EqualTo(10));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		// Sybase excluded by https://github.com/linq2db/linq2db/issues/5865 - the element-form eager-load
		// preamble joins a derived table carrying TOP, which SybaseDataProvider already declares invalid
		// through IsJoinDerivedTableWithTakeInvalid, but the preamble reaches the provider without that
		// check running, so only the first detail row comes back.
		public void ElementFormLoadWithTest([DataSources(TestProvName.AllSybase)] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,Parent>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.First());

			using var db = GetDataContext(context);

			var parent = query(db, 1);
			var other  = query(db, 2);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parent.ParentID, Is.EqualTo(1));
				Assert.That(parent.Children, Has.Count.EqualTo(1));
				Assert.That(other.ParentID,  Is.EqualTo(2));
				Assert.That(other.Children,  Has.Count.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		// Sybase excluded by https://github.com/linq2db/linq2db/issues/5865 - see ElementFormLoadWithTest.
		public void ElementFormLoadWithThenLoadTest([DataSources(TestProvName.AllSybase)] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,Parent>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.ThenLoad(c => c.GrandChildren)
					.First());

			using var db = GetDataContext(context);

			var parent = query(db, 1);
			var other  = query(db, 2);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parent.ParentID,                  Is.EqualTo(1));
				Assert.That(parent.Children,                  Has.Count.EqualTo(1));
				Assert.That(parent.Children[0].GrandChildren, Has.Count.EqualTo(1));
				Assert.That(other.ParentID,                   Is.EqualTo(2));
				Assert.That(other.Children,                   Has.Count.EqualTo(2));
				Assert.That(other.Children[0].GrandChildren,  Has.Count.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		public void ElementFormLoadWithOpensLoadTransaction([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var queryable = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children));

			var element = CompiledQuery.Compile<ITestDataContext,int,Parent>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.First());

			var transactions = 0;

			using var db = GetDataContext(context, o => o.UseTracing(e =>
			{
				if (e.TraceInfoStep == TraceInfoStep.BeforeExecute && e.Operation == TraceOperation.BeginTransaction)
					transactions++;
			}));

			var viaQueryable   = queryable(db, 1).First();
			var afterQueryable = transactions;
			var viaElement     = element(db, 1);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(viaQueryable.Children, Has.Count.EqualTo(1));
				Assert.That(viaElement.Children,   Has.Count.EqualTo(1));

				Assert.That(afterQueryable, Is.EqualTo(1), "queryable form must open the implicit eager-loading transaction");
				Assert.That(transactions,   Is.EqualTo(2), "element form must open one as well");
			}
		}

		[Test]
		public void TwoFoldSitesOfSameTypeDoNotShareQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int id) => new
			{
				Exact  = db.Parent.Where(p => p.ParentID == id).Select(p => p.ParentID).ToList(),
				Larger = db.Parent.Where(p => p.ParentID >  id).Select(p => p.ParentID).ToList(),
			});

			using var db = GetDataContext(context);

			var result = query(db, 1);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Exact,  Is.EqualTo(new[] { 1 }));
				Assert.That(result.Larger, Is.Not.Empty);
				Assert.That(result.Larger, Does.Not.Contain(1));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5854")]
		public void WrappedLoadWithTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWithWrapper(p => p.Children));

			using var db = GetDataContext(context);

			var parent = query(db, 1).First();
			var other  = query(db, 2).First();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parent.ParentID, Is.EqualTo(1));
				Assert.That(parent.Children, Has.Count.EqualTo(1));
				Assert.That(other.ParentID,  Is.EqualTo(2));
				Assert.That(other.Children,  Has.Count.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5854")]
		public void WrappedWhereUsesCurrentArgumentsTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>(static (db, id) =>
				db.Parent
					.Where(p => p.ParentID > 0)
					.WhereWrapper(p => p.ParentID == id));

			using var db = GetDataContext(context);

			var first  = query(db, 1).Select(p => p.ParentID).ToList();
			var second = query(db, 2).Select(p => p.ParentID).ToList();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(first,  Is.EqualTo(new[] { 1 }));
				Assert.That(second, Is.EqualTo(new[] { 2 }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5854")]
		public void WrappedLoadWithOnTableTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// The wrapper's source is ITestDataContext.Parent, declared ITable<Parent> rather than IQueryable<Parent>.
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>(static (db, id) =>
				db.Parent.LoadWithWrapper(p => p.Children));

			using var db = GetDataContext(context);

			var parents = query(db, 1).ToList();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parents.First(p => p.ParentID == 1).Children, Has.Count.EqualTo(1));
				Assert.That(parents.First(p => p.ParentID == 2).Children, Has.Count.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5844#issuecomment-5538235961")]
		public void ClosureValueSurvivesRebalancedPredicateTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// Four conjuncts is where BinaryExpressionAggregatorVisitor rebalances the tree, so the exposed
			// predicate stops matching the one Create hands the table and the closure accessor reads the wrong node.
			var id    = 2;
			var query = CompiledQuery.Compile<ITestDataContext,IEnumerable<Parent>>(d =>
				d.Parent.Where(p => p.ParentID == id && p.ParentID > 0 && p.ParentID < 1000 && p.ParentID != -1));

			Assert.That(query(db).Select(p => p.ParentID).ToList(), Is.EqualTo(new[] { 2 }));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5844#issuecomment-5538235961")]
		public void ClosureValueSurvivesRebalancedPredicateWithLoadWithTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// Same shape through the eager-loading path: preambles are initialised from the same container.
			var id    = 2;
			var query = CompiledQuery.Compile<ITestDataContext,IEnumerable<Parent>>(d =>
				d.Parent
					.Where(p => p.ParentID == id && p.ParentID > 0 && p.ParentID < 1000 && p.ParentID != -1)
					.LoadWith(p => p.Children));

			Assert.That(query(db).Single().Children, Has.Count.EqualTo(2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5844#issuecomment-5538235961")]
		public void ClosureAndArgumentSurviveRebalancedPredicateTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// Both accessor kinds in one rebalanced predicate: the argument is read out of the array, the
			// captured local by walking the tree, and only the second one depends on the two trees agreeing.
			var floor = 0;
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Parent>>((d, id) =>
				d.Parent.Where(p => p.ParentID == id && p.ParentID > floor && p.ParentID < 1000 && p.ParentID != -1));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(query(db, 1).Select(p => p.ParentID).ToList(), Is.EqualTo(new[] { 1 }));
				Assert.That(query(db, 2).Select(p => p.ParentID).ToList(), Is.EqualTo(new[] { 2 }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5844#issuecomment-5538235961")]
		public void ElementFormSurvivesRebalancedPredicateTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// Symmetry guard on the path Create does not take: Execute passes CompiledExpressions itself, so
			// this shape has to stay green whether or not Create hands the table the exposed tree.
			var id    = 2;
			var query = CompiledQuery.Compile<ITestDataContext,Parent>(d =>
				d.Parent.First(p => p.ParentID == id && p.ParentID > 0 && p.ParentID < 1000 && p.ParentID != -1));

			Assert.That(query(db).ParentID, Is.EqualTo(2));
		}

		sealed class FilteredRow
		{
			[PrimaryKey] public int Id      { get; set; }
			             public bool Hidden { get; set; }
		}

		// The IgnoreFilters call - and so the [SqlQueryDependent] position its argument occupies - exists only
		// after this is expanded, which is after the compiled table has already collected its cache-key slots.
		[ExpressionMethod(nameof(VisibleRowsImpl))]
		static IQueryable<FilteredRow> VisibleRows(IDataContext db, Type ignoreFor) => throw new InvalidOperationException();

		static Expression<Func<IDataContext,Type,IQueryable<FilteredRow>>> VisibleRowsImpl()
			=> (db, ignoreFor) => db.GetTable<FilteredRow>().IgnoreFilters(ignoreFor);

		[Test]
		public void DependentArgumentCreatedByExpansionTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<FilteredRow>()
					.HasQueryFilter((q, dc) => q.Where(r => !r.Hidden))
				.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable(
			[
				new FilteredRow { Id = 1, Hidden = false },
				new FilteredRow { Id = 2, Hidden = true  },
			]);

			var query = CompiledQuery.Compile<IDataContext,Type,IEnumerable<FilteredRow>>(
				static (dc, t) => VisibleRows(dc, t));

			var ignored = query(db, typeof(FilteredRow)).Select(r => r.Id).ToList();
			var applied = query(db, typeof(Parent)).Select(r => r.Id).ToList();

			using (Assert.EnterMultipleScope())
			{
				// Ignoring the filter for FilteredRow returns the hidden row too, ignoring it for an unrelated
				// entity leaves the filter in place - so the two invocations cannot share one cached query.
				Assert.That(ignored, Is.EquivalentTo(new[] { 1, 2 }));
				Assert.That(applied, Is.EquivalentTo(new[] { 1 }));
			}
		}
	}

	static class CompiledQueryWrapperExtensions
	{
		// Unlike LoadWithWrapper's selector, this predicate closes over a compiled argument, so it fails when
		// the expansion folds the array into the quote instead of leaving its ps[i] reads alone.
		public static IQueryable<TEntity> WhereWrapper<TEntity>(
			this IQueryable<TEntity>       source,
			Expression<Func<TEntity,bool>> predicate)
		{
			return source.Where(predicate);
		}

		// A user-defined pass-through is not on IsQueryable's declaring-type allowlist, but its IQueryable<T>
		// return type is enough for CompileQuery to fold it into the compiled table anyway. Expose then
		// expands it over its own source, so the argument-array reads inside it survive into the built tree.
		public static IQueryable<TEntity> LoadWithWrapper<TEntity,TProperty>(
			this IQueryable<TEntity>             source,
			Expression<Func<TEntity,TProperty?>> selector)
			where TEntity : class
		{
			return source.LoadWith(selector);
		}
	}
}
