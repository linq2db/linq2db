using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
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
						using (var db = GetDataContext(context))
						{
							var id = (n % 6) + 1;
							results[n, 0] = id;
							results[n, 1] = query(db, id);
						}
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
						using (var db = GetDataContext(context))
						{
							var id = (n % 6) + 1;
							results[n, 0] = id;
							results[n, 1] = query(db, id, id);
						}
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
						using (var db = GetDataContext(context))
						{
							var id = (n % 6) + 1;
							results[n, 0] = id;
							results[n, 1] = db.Parent.Where(p => p.ParentID == id).First().ParentID;
						}
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
						using (var db = GetDataContext(context))
						{
							var id = (n % 6) + 1;
							results[n, 0] = id;
							results[n, 1] = db.Parent.Where(p => p.ParentID == id && id >= 0).First().ParentID;
						}
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
			using (var db = GetDataContext(context))
			{
				var query = CompiledQuery.Compile((ITestDataContext xdb, int id) => Filter(xdb, id).FirstOrDefault());

				query(db, 1);
			}
		}

		[Test]
		public async Task CompiledQueryWithExpressionMethoAsyncdTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = CompiledQuery.Compile((ITestDataContext xdb, int id) => Filter(xdb, id).FirstOrDefaultAsync(default));

				await query(db, 1);
			}
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

		[ActiveIssue]
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

		[ActiveIssue]
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

		[ActiveIssue]
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
	}
}
