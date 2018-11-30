using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.xUpdate
{
	using Model;

	[TestFixture]
	public class DeleteTests : TestBase
	{
		[Test, DataContextSource]
		public void Delete1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Delete(parent);
				db.Insert(parent);

				try
				{
					Assert.AreEqual(1, db.Parent.Count (p => p.ParentID == parent.ParentID));
					Assert.AreEqual(1, db.Parent.Delete(p => p.ParentID == parent.ParentID));
					Assert.AreEqual(0, db.Parent.Count (p => p.ParentID == parent.ParentID));
				}
				finally
				{
					db.Delete(parent);
				}
			}
		}

		[Test, DataContextSource]
		public void Delete2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Delete(parent);
				db.Insert(parent);

				try
				{
					Assert.AreEqual(1, db.Parent.Count(p => p.ParentID == parent.ParentID));
					Assert.AreEqual(1, db.Parent.Where(p => p.ParentID == parent.ParentID).Delete());
					Assert.AreEqual(0, db.Parent.Count(p => p.ParentID == parent.ParentID));
				}
				finally
				{
					db.Delete(parent);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void Delete3(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Child.Delete(c => new[] { 1001, 1002 }.Contains(c.ChildID));

					db.Child.Insert(() => new Child { ParentID = 1, ChildID = 1001 });
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = 1002 });

					Assert.AreEqual(3, db.Child.Count(c => c.ParentID == 1));
					Assert.AreEqual(2, db.Child.Where(c => c.Parent.ParentID == 1 && new[] { 1001, 1002 }.Contains(c.ChildID)).Delete());
					Assert.AreEqual(1, db.Child.Count(c => c.ParentID == 1));
				}
				finally
				{
					db.Child.Delete(c => new[] { 1001, 1002 }.Contains(c.ChildID));
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void Delete4(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.GrandChild1.Delete(gc => new[] { 1001, 1002 }.Contains(gc.GrandChildID.Value));

					db.GrandChild.Insert(() => new GrandChild { ParentID = 1, ChildID = 1, GrandChildID = 1001 });
					db.GrandChild.Insert(() => new GrandChild { ParentID = 1, ChildID = 2, GrandChildID = 1002 });

					Assert.AreEqual(3, db.GrandChild1.Count(gc => gc.ParentID == 1));
					Assert.AreEqual(2, db.GrandChild1.Where(gc => gc.Parent.ParentID == 1 && new[] { 1001, 1002 }.Contains(gc.GrandChildID.Value)).Delete());
					Assert.AreEqual(1, db.GrandChild1.Count(gc => gc.ParentID == 1));
				}
				finally
				{
					db.GrandChild1.Delete(gc => new[] { 1001, 1002 }.Contains(gc.GrandChildID.Value));
				}
			}
		}

		[Test, DataContextSource]
		public void Delete5(string context)
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

					Assert.AreEqual(2, db.Parent.Count(_ => _.ParentID > 1000));
					Assert.AreEqual(2, db.Parent.Delete(_ => values.Contains(_.ParentID)));
					Assert.AreEqual(0, db.Parent.Count(_ => _.ParentID > 1000));
				}
			}
		}

		[Test, DataContextSource(false, ProviderName.Informix)]
		public void AlterDelete(string context)
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

				var sql = ((DataConnection)db).LastQuery;

				if (sql.Contains("EXISTS"))
					Assert.That(sql.IndexOf("(("), Is.GreaterThan(0));
			}
		}

		[Test, DataContextSource(
			ProviderName.Access, ProviderName.DB2, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQL11,
			ProviderName.SqlCe, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.SapHana)]
		public void DeleteMany1(string context)
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

		[Test, DataContextSource(
			ProviderName.Access, ProviderName.DB2, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11,
			ProviderName.SqlCe, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.SapHana
			)]
		public void DeleteMany2(string context)
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

					Assert.That(n1, Is.EqualTo(6));
					Assert.That(n2, Is.EqualTo(2));
				}
				finally
				{
					db.GrandChild.Delete(c => c.ParentID >= 1000);
					db.Child.     Delete(c => c.ParentID >= 1000);
					db.Parent.    Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test, DataContextSource(
			ProviderName.Access, ProviderName.DB2, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQL11,
			ProviderName.SqlCe, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.SapHana
			)]
		public void DeleteMany3(string context)
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

		[Test, DataContextSource(
			ProviderName.Access,
			ProviderName.DB2,
			ProviderName.Firebird, TestProvName.Firebird3,
			ProviderName.Informix,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11,
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
			ProviderName.SqlCe,
			ProviderName.SqlServer2000,
			ProviderName.SapHana
			)]
		public void DeleteTop(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Parent.Delete(c => c.ParentID >= 1000);

					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i });

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

		string ContainsJoin1Impl(TestDataConnection db, int[] arr)
		{
			var id = 1000;

			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				where c.ParentID == id && !arr.Contains(c.ChildID)
				select p
			).Delete();

			return db.LastQuery;
		}

		[Test, DataContextSource(false, ProviderName.Informix)]
		public void ContainsJoin1(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Child. Delete(c => c.ParentID >= 1000);
				db.Parent.Delete(c => c.ParentID >= 1000);

				try
				{
					var id = 1000;

					db.Insert(new Parent { ParentID = id });

					for (var i = 0; i < 3; i++)
						db.Insert(new Child { ParentID = id, ChildID = 1000 + i });

					var sql1 = ContainsJoin1Impl(db, new [] { 1000, 1001 });
					var sql2 = ContainsJoin1Impl(db, new [] { 1002       });

					Assert.That(sql1, Is.Not.EqualTo(sql2));
				}
				finally
				{
					db.Child. Delete(c => c.ParentID >= 1000);
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test, DataContextSource(false, ProviderName.Informix)]
		public void MultipleDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Parent.Delete(c => c.ParentID >= 1000);

				try
				{
					var list = new[] { new Parent { ParentID = 1000 }, new Parent { ParentID = 1001 } };

					db.BulkCopy(list);

					var ret = db.Parent.Delete(p => list.Contains(p) );

					Assert.That(ret, Is.EqualTo(2));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[ActiveIssue(":NEW as parameter", Configuration = ProviderName.OracleNative)]
		[Test, DataContextSource]
		public void DeleteByTableName(string context)
		{
			const string schemaName = null;
			var tableName  = "xxPerson" + TestUtils.GetNext().ToString();

			using (var db = GetDataContext(context))
			using (var table = db.CreateTempTable<Person>(tableName, schemaName: schemaName))
			{
				var iTable = (ITable<Person>)table;
				Assert.AreEqual(tableName,  iTable.TableName);
				Assert.AreEqual(schemaName, iTable.SchemaName);

				var person = new Person()
				{
					FirstName = "Steven",
					LastName = "King",
					Gender = Gender.Male,
				};

				// insert a row into the table
				db.Insert(person, tableName: tableName, schemaName: schemaName);
				var newCount = table.Count();
				Assert.AreEqual(1, newCount);

				var personForDelete = table.Single();

				db.Delete(personForDelete, tableName: tableName, schemaName: schemaName);

				Assert.AreEqual(0, table.Count());
			}
		}

		[ActiveIssue(":NEW as parameter", Configuration = ProviderName.OracleNative)]
		[Test, DataContextSource]
		public async Task DeleteByTableNameAsync(string context)
		{
			const string schemaName = null;
			const string tableName  = "xxPerson";

			using (var db = GetDataContext(context))
			{
				try
				{
					var table = await db.CreateTableAsync<Person>(tableName, schemaName: schemaName);

					Assert.AreEqual(tableName, table.TableName);
					Assert.AreEqual(schemaName, table.SchemaName);

					var person = new Person()
					{
						FirstName = "Steven",
						LastName  = "King",
						Gender    = Gender.Male,
					};

					// insert a row into the table
					await db.InsertAsync(person, tableName: tableName, schemaName: schemaName);
					var newCount = await table.CountAsync();
					Assert.AreEqual(1, newCount);

					var personForDelete = await table.SingleAsync();

					await db.DeleteAsync(personForDelete, tableName: tableName, schemaName: schemaName);

					Assert.AreEqual(0, await table.CountAsync());

					await table.DropAsync();
				}
				catch
				{
					await db.DropTableAsync<Person>(tableName, schemaName: schemaName);
					throw;
				}
			}
		}
	}
}
