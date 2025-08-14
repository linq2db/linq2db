using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Tests.Model;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class DeleteTests : TestBase
	{
		[Test]
		public void Delete1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Delete(parent);
				db.Insert(parent);

				Assert.That(db.Parent.Count (p => p.ParentID == parent.ParentID), Is.EqualTo(1));
				var cnt = db.Parent.Delete(p => p.ParentID == parent.ParentID);
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
				Assert.That(db.Parent.Count (p => p.ParentID == parent.ParentID), Is.Zero);
			}
		}

		[Test]
		public void Delete2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Delete(parent);
				db.Insert(parent);

				Assert.That(db.Parent.Count(p => p.ParentID == parent.ParentID), Is.EqualTo(1));
				var cnt = db.Parent.Where(p => p.ParentID == parent.ParentID).Delete();
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
				Assert.That(db.Parent.Count(p => p.ParentID == parent.ParentID), Is.Zero);
			}
		}

		[Test]
		public void Delete3([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db.Child.Delete(c => new[] { 1001, 1002 }.Contains(c.ChildID));

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = 1001 });
				db.Child.Insert(() => new Child { ParentID = 1, ChildID = 1002 });
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ParentID == 1), Is.EqualTo(3));
					Assert.That(db.Child.Where(c => c.Parent!.ParentID == 1 && new[] { 1001, 1002 }.Contains(c.ChildID)).Delete(), Is.EqualTo(2));
				}

				Assert.That(db.Child.Count(c => c.ParentID == 1), Is.EqualTo(1));
			}
		}

		[Test]
		public void Delete4([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db.GrandChild1.Delete(gc => new[] { 1001, 1002 }.Contains(gc.GrandChildID!.Value));

				db.GrandChild.Insert(() => new GrandChild { ParentID = 1, ChildID = 1, GrandChildID = 1001 });
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1, ChildID = 2, GrandChildID = 1002 });
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.GrandChild1.Count(gc => gc.ParentID == 1), Is.EqualTo(3));
					Assert.That(db.GrandChild1.Where(gc => gc.Parent!.ParentID == 1 && new[] { 1001, 1002 }.Contains(gc.GrandChildID!.Value)).Delete(), Is.EqualTo(2));
				}

				Assert.That(db.GrandChild1.Count(gc => gc.ParentID == 1), Is.EqualTo(1));
			}
		}

		[Test]
		public void Delete5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var values = new[] { 1001, 1002 };

				db.Parent.Delete(_ => _.ParentID > 1000);

				try
				{
					db.Parent.Delete(_ => _.ParentID > 1000);
				}
				finally
				{
					db.Parent.Insert(() => new Parent { ParentID = values[0], Value1 = 1 });
					db.Parent.Insert(() => new Parent { ParentID = values[1], Value1 = 1 });

					Assert.That(db.Parent.Count(_ => _.ParentID > 1000), Is.EqualTo(2));
					var cnt = db.Parent.Delete(_ => values.Contains(_.ParentID));
					if (context.SupportsRowcount())
						Assert.That(cnt, Is.EqualTo(2));
					Assert.That(db.Parent.Count(_ => _.ParentID > 1000), Is.Zero);
				}
			}
		}

		[Test]
		public void AlterDelete([DataSources(false, TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch != null && ch.ParentID == -1 || ch == null && p.ParentID == -1
					select p;

				q.Delete();
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.ClickHouse.Error_CorrelatedDelete)]
		public void DeleteMany1([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Insert(() => new Parent { ParentID = 1001 });
				db.Child. Insert(() => new Child  { ParentID = 1001, ChildID = 1 });
				db.Child. Insert(() => new Child  { ParentID = 1001, ChildID = 2 });

				try
				{
					var q =
						from p in db.Parent
						where p.ParentID >= 1000
						select p;

					var n = q.SelectMany(p => p.Children).Delete();

					Assert.That(n, Is.GreaterThanOrEqualTo(2));
				}
				finally
				{
					db.Child. Delete(c => c.ParentID >= 1000);
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void DeleteMany2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.    Insert(() => new Parent     { ParentID = 1001 });
				db.Child.     Insert(() => new Child      { ParentID = 1001, ChildID = 1 });
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 1, GrandChildID = 1});
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 1, GrandChildID = 2});
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 1, GrandChildID = 3});
				db.Child.     Insert(() => new Child      { ParentID = 1001, ChildID = 2 });
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 2, GrandChildID = 1});
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 2, GrandChildID = 2});
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 2, GrandChildID = 3});

				try
				{
					var q =
						from p in db.Parent
						where p.ParentID >= 1000
						select p;

					var n1 = q.SelectMany(p => p.Children.SelectMany(c => c.GrandChildren)).Delete();
					var n2 = q.SelectMany(p => p.Children).                                 Delete();
					using (Assert.EnterMultipleScope())
					{
						Assert.That(n1, Is.EqualTo(6));
						Assert.That(n2, Is.EqualTo(2));
					}
				}
				finally
				{
					db.GrandChild.Delete(c => c.ParentID >= 1000);
					db.Child.     Delete(c => c.ParentID >= 1000);
					db.Parent.    Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.ClickHouse.Error_CorrelatedDelete)]
		[Test]
		public void DeleteMany3([DataSources] string context)
		{
			var ids = new[] { 1001 };

			using (var db = GetDataContext(context))
			{
				db.GrandChild.Delete(c => c.ParentID >= 1000);
				db.Child.     Delete(c => c.ParentID >= 1000);
				db.Parent.    Delete(c => c.ParentID >= 1000);

				db.Parent.    Insert(() => new Parent     { ParentID = 1001 });
				db.Child.     Insert(() => new Child      { ParentID = 1001, ChildID = 1 });
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 1, GrandChildID = 1});
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1001, ChildID = 1, GrandChildID = 2});

				try
				{
					var q =
						from p in db.Parent
						where ids.Contains(p.ParentID)
						select p;

					var n1 = q.SelectMany(p => p.Children).SelectMany(gc => gc.GrandChildren).Delete();

					Assert.That(n1, Is.EqualTo(2));
				}
				finally
				{
					db.GrandChild.Delete(c => c.ParentID >= 1000);
					db.Child.     Delete(c => c.ParentID >= 1000);
					db.Parent.    Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2549")]
		public void DeleteTakeNotOrdered(
			[DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllInformix,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSapHana,
			ProviderName.SqlCe,
			ProviderName.Ydb,
			ProviderName.SQLiteMS
			)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Parent.Delete(c => c.ParentID >= 1000);

					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });

					var rowsAffected = db.Parent
						.Where(p => p.ParentID >= 1000)
						.Take(5)
						.Delete();

					Assert.That(rowsAffected, Is.EqualTo(5));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2549")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_DeleteWithTopOrderBy)]
		public void DeleteTakeOrdered([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllInformix,
			ProviderName.SqlCe,
			ProviderName.Ydb,
			ProviderName.SQLiteMS,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSapHana,
			TestProvName.AllOracle
			)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					using (new DisableLogging())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
					}

					var entities =
						from x in db.Parent
						where x.ParentID > 1000
						orderby x.ParentID descending
						select x;

					var rowsAffected = entities
						.Take(5)
						.Delete();

					Assert.That(rowsAffected, Is.EqualTo(5));
					var data = db.Parent.Where(p => p.ParentID >= 1000).OrderBy(p => p.ParentID).Select(r => r.Value1!.Value).ToArray();
					Assert.That(data, Is.EqualTo(new int[] { 1000, 1001, 1002, 1003, 1004 }));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_DeleteWithTopOrderBy)]
		public void DeleteSkipTakeOrdered([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllInformix,
			ProviderName.SqlCe,
			ProviderName.Ydb,
			ProviderName.SQLiteMS,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSapHana,
			TestProvName.AllOracle
			)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					using (new DisableLogging())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
					}

					var entities =
						from x in db.Parent
						where x.ParentID > 1000
						orderby x.ParentID descending
						select x;

					var rowsAffected = entities
						.Skip(2)
						.Take(5)
						.Delete();

					Assert.That(rowsAffected, Is.EqualTo(5));
					var data = db.Parent.Where(p => p.ParentID >= 1000).OrderBy(p => p.ParentID).Select(r => r.Value1!.Value).ToArray();
					Assert.That(data, Is.EqualTo(new int[] { 1000, 1001, 1002, 1008, 1009 }));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_DeleteWithSkip)]
		public void DeleteSkipTakeNotOrdered([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllInformix,
			ProviderName.SqlCe,
			ProviderName.Ydb,
			ProviderName.SQLiteMS,
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSapHana,
			TestProvName.AllOracle
			)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					using (new DisableLogging())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
					}

					var entities =
						from x in db.Parent
						where x.ParentID > 1000
						select x;

					var rowsAffected = entities
						.Skip(6)
						.Take(5)
						.Delete();

					Assert.That(rowsAffected, Is.EqualTo(3));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void DeleteOrdered([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			ProviderName.SqlCe,
			ProviderName.Ydb,
			TestProvName.AllDB2,
			TestProvName.AllInformix,
			TestProvName.AllSQLite,
			TestProvName.AllOracle,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSapHana,
			TestProvName.AllSqlServer,
			TestProvName.AllSybase
			)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					using (new DisableLogging())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
					}

					var entities =
						from x in db.Parent
						where x.ParentID > 1000
						orderby x.ParentID descending
						select x;

					var rowsAffected = entities
						.Delete();

					Assert.That(rowsAffected, Is.EqualTo(9));
					var data = db.Parent.Where(p => p.ParentID >= 1000).OrderBy(p => p.ParentID).Select(r => r.Value1!.Value).ToArray();
					Assert.That(data, Is.EqualTo(new int[] { 1000 }));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		string ContainsJoin1Impl(TestDataConnection db, int[] arr)
		{
			var id = 1000;

			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				where c.ParentID == id && !arr.Contains(c.ChildID)
				select p
			).Delete();

			return db.LastQuery!;
		}

		[Test]
		public void ContainsJoin1([DataSources(false, TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1000;

				db.Insert(new Parent { ParentID = id });

				for (var i = 0; i < 3; i++)
					db.Insert(new Child { ParentID = id, ChildID = 1000 + i });

				var sql1 = ContainsJoin1Impl(db, new [] { 1000, 1001 });
				var sql2 = ContainsJoin1Impl(db, new [] { 1002       });

				Assert.That(sql1, Is.Not.EqualTo(sql2));
			}
		}

		[Test]
		public void MultipleDelete([DataSources(false, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Parent.Delete(c => c.ParentID >= 1000);

				try
				{
					var list = new[] { new Parent { ParentID = 1000 }, new Parent { ParentID = 1001 } };

					db.BulkCopy(GetDefaultBulkCopyOptions(context), list);

					var ret = db.Parent.Delete(p => list.Contains(p) );

					if (context.SupportsRowcount())
						Assert.That(ret, Is.EqualTo(2));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void DeleteByTableName([DataSources] string context)
		{
			var tableName  = TestUtils.GetTableName(context, "1a");

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<Person>(tableName))
			{
				var iTable = (ITable<Person>)table;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(iTable.TableName, Is.EqualTo(tableName));
					Assert.That(iTable.SchemaName, Is.Null);
				}

				var person = new Person()
				{
					FirstName = "Steven",
					LastName = "King",
					Gender = Gender.Male,
				};

				// insert a row into the table
				db.Insert(person, tableName);
				var newCount = table.Count();
				Assert.That(newCount, Is.EqualTo(1));

				var personForDelete = table.Single();

				db.Delete(personForDelete, tableName);

				Assert.That(table.Count(), Is.Zero);
			}
		}

		[Test]
		public async Task DeleteByTableNameAsync([DataSources] string context)
		{
			const string? schemaName = null;
			var tableName  = TestUtils.GetTableName(context, "30");

			using (var db = GetDataContext(context))
			{
				await db.DropTableAsync<Person>(tableName, schemaName: schemaName, throwExceptionIfNotExists:false);

				try
				{
					var table = await db.CreateTableAsync<Person>(tableName, schemaName: schemaName);
					using (Assert.EnterMultipleScope())
					{
						Assert.That(table.TableName, Is.EqualTo(tableName));
						Assert.That(table.SchemaName, Is.EqualTo(schemaName));
					}

					var person = new Person()
					{
						FirstName = "Steven",
						LastName  = "King",
						Gender    = Gender.Male,
					};

					// insert a row into the table
					await db.InsertAsync(person, tableName: tableName, schemaName: schemaName);
					var newCount = await table.CountAsync();
					Assert.That(newCount, Is.EqualTo(1));

					var personForDelete = await table.SingleAsync();

					await db.DeleteAsync(personForDelete, tableName: tableName, schemaName: schemaName);

					Assert.That(await table.CountAsync(), Is.Zero);

					await table.DropAsync();
				}
				catch
				{
					await db.DropTableAsync<Person>(tableName, schemaName: schemaName);
					throw;
				}
			}
		}

		// based on TestDeleteFrom test in EFCore tests project, it should be reenabled after fix
		[ActiveIssue(Configurations = [TestProvName.AllClickHouse, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySql, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllSapHana, ProviderName.SqlCe, TestProvName.AllSQLite])]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[Test]
		public void DeleteFromWithTake([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = new RestoreBaseTables(db);

			db.Insert(new Parent() { ParentID = 1001 });
			db.Insert(new Parent() { ParentID = 1002 });
			db.Insert(new Parent() { ParentID = 1003 });

			var query = db.Parent.OrderBy(c => c.ParentID).Where(c => c.ParentID > 1000).Take(2);

			// note how query used twice
			var cnt = query.Where(p => query.Select(c => c.ParentID).Contains(p.ParentID)).Delete();

			var left = db.Parent.Count(c => c.ParentID > 1000);
			var deleted = db.Parent.Delete(c => c.ParentID == 1003) == 1;
			using (Assert.EnterMultipleScope())
			{
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(2));
				Assert.That(left, Is.EqualTo(1));
				if (context.SupportsRowcount())
					Assert.That(deleted, Is.True);
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllClickHouse, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySql, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllSapHana, ProviderName.SqlCe, TestProvName.AllSQLite, TestProvName.AllSybase])]
		[Test]
		public void DeleteFromWithTake_NoSort([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = new RestoreBaseTables(db);

			db.Insert(new Parent() { ParentID = 1001 });
			db.Insert(new Parent() { ParentID = 1002 });
			db.Insert(new Parent() { ParentID = 1003 });

			var query = db.Parent.Where(c => c.ParentID > 1000).Take(2);

			// note how query used twice
			var cnt = query.Where(p => query.Select(c => c.ParentID).Contains(p.ParentID)).Delete();

			var left = db.Parent.Count(c => c.ParentID > 1000);
			var deleted = db.Parent.Delete(c => c.ParentID > 1000) == 1;
			using (Assert.EnterMultipleScope())
			{
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(2));
				Assert.That(left, Is.EqualTo(1));
				if (context.SupportsRowcount())
					Assert.That(deleted, Is.True);
			}
		}
	}
}
