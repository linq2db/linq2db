using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.xUpdate
{
	using Model;

	[TestFixture]
	public class UpdateTest : TestBase
	{
		[Test, DataContextSource]
		public void Update1(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

					db.Parent.Delete(p => p.ParentID > 1000);
					db.Insert(parent);

					Assert.AreEqual(1, db.Parent.Count (p => p.ParentID == parent.ParentID));
					Assert.AreEqual(1, db.Parent.Update(p => p.ParentID == parent.ParentID, p => new Parent { ParentID = p.ParentID + 1 }));
					Assert.AreEqual(1, db.Parent.Count (p => p.ParentID == parent.ParentID + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Update2(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

					db.Parent.Delete(p => p.ParentID > 1000);
					db.Insert(parent);

					Assert.AreEqual(1, db.Parent.Count(p => p.ParentID == parent.ParentID));
					Assert.AreEqual(1, db.Parent.Where(p => p.ParentID == parent.ParentID).Update(p => new Parent { ParentID = p.ParentID + 1 }));
					Assert.AreEqual(1, db.Parent.Count(p => p.ParentID == parent.ParentID + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void Update3(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = id});

					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
					Assert.AreEqual(1, db.Child.Where(c => c.ChildID == id && c.Parent.Value1 == 1).Update(c => new Child { ChildID = c.ChildID + 1 }));
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void Update4(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = id});

					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
					Assert.AreEqual(1,
						db.Child
							.Where(c => c.ChildID == id && c.Parent.Value1 == 1)
								.Set(c => c.ChildID, c => c.ChildID + 1)
							.Update());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void Update5(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = id});

					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
					Assert.AreEqual(1,
						db.Child
							.Where(c => c.ChildID == id && c.Parent.Value1 == 1)
								.Set(c => c.ChildID, () => id + 1)
							.Update());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void Update6(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Parent4.Delete(p => p.ParentID > 1000);
					db.Insert(new Parent4 { ParentID = id, Value1 = TypeValue.Value1 });

					Assert.AreEqual(1, db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value1));
					Assert.AreEqual(1,
						db.Parent4
							.Where(p => p.ParentID == id)
								.Set(p => p.Value1, () => TypeValue.Value2)
							.Update());
					Assert.AreEqual(1, db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value2));
				}
				finally
				{
					db.Parent4.Delete(p => p.ParentID > 1000);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void Update7(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Parent4.Delete(p => p.ParentID > 1000);
					db.Insert(new Parent4 { ParentID = id, Value1 = TypeValue.Value1 });

					Assert.AreEqual(1, db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value1));
					Assert.AreEqual(1,
						db.Parent4
							.Where(p => p.ParentID == id)
								.Set(p => p.Value1, TypeValue.Value2)
							.Update());
					Assert.AreEqual(1, db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value2));

					Assert.AreEqual(1,
						db.Parent4
							.Where(p => p.ParentID == id)
								.Set(p => p.Value1, TypeValue.Value3)
							.Update());
					Assert.AreEqual(1, db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value3));
				}
				finally
				{
					db.Parent4.Delete(p => p.ParentID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Update8(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

					db.Parent.Delete(p => p.ParentID > 1000);
					db.Insert(parent);

					parent.Value1++;

					db.Update(parent);

					Assert.AreEqual(1002, db.Parent.Single(p => p.ParentID == parent.ParentID).Value1);
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource(
			ProviderName.Informix,
			ProviderName.SqlCe,
			ProviderName.DB2,
			ProviderName.Firebird,
			ProviderName.OracleNative,
			ProviderName.OracleManaged,
			ProviderName.PostgreSQL, 
			ProviderName.MySql,
			TestProvName.MariaDB,
			ProviderName.SQLite,
			ProviderName.Access,
			ProviderName.SapHana)]
		public void Update9(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = id});

					var q =
						from c in db.Child
						join p in db.Parent on c.ParentID equals p.ParentID
						where c.ChildID == id && c.Parent.Value1 == 1
						select new { c, p };

					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
					Assert.AreEqual(1, q.Update(db.Child, _ => new Child { ChildID = _.c.ChildID + 1, ParentID = _.p.ParentID }));
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource(
			ProviderName.Informix,
			ProviderName.SqlCe,
			ProviderName.DB2,
			ProviderName.Firebird,
			ProviderName.OracleNative,
			ProviderName.OracleManaged,
			ProviderName.PostgreSQL,
			ProviderName.MySql,
			TestProvName.MariaDB,
			ProviderName.SQLite,
			ProviderName.Access,
			ProviderName.SapHana)]
		public void Update10(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = id});

					var q =
						from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
						where c.ChildID == id && c.Parent.Value1 == 1
						select new { c, p };

					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
					Assert.AreEqual(1, q.Update(db.Child, _ => new Child { ChildID = _.c.ChildID + 1, ParentID = _.p.ParentID }));
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		//[Test, DataContextSource]
		public void Update11(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var q = db.GetTable<LinqDataTypes2>().Union(db.GetTable<LinqDataTypes2>());

				//db.GetTable<LinqDataTypes2>().Update(_ => q.Contains(_), _ => new LinqDataTypes2 { GuidValue = _.GuidValue });

				q.Update(_ => new LinqDataTypes2 { GuidValue = _.GuidValue });
			}
		}

		[Test, DataContextSource(
			ProviderName.SqlCe, ProviderName.SQLite, ProviderName.DB2, ProviderName.Informix,
			ProviderName.Firebird, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL)]
		public void Update12(string context)
		{
			using (var db = GetDataContext(context))
			{
				(
					from p1 in db.Parent
					join p2 in db.Parent on p1.ParentID equals p2.ParentID
					where p1.ParentID < 3
					select new { p1, p2 }
				)
				.Update(q => q.p1, q => new Parent { ParentID = q.p2.ParentID });
			}
		}

		[Test, DataContextSource(
			ProviderName.SqlCe, ProviderName.SQLite, ProviderName.DB2, ProviderName.Informix,
			ProviderName.Firebird, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL)]
		public void Update13(string context)
		{
			using (var db = GetDataContext(context))
			{
				(
					from p1 in db.Parent
					join p2 in db.Parent on p1.ParentID equals p2.ParentID
					where p1.ParentID < 3
					select new { p1, p2 }
				)
				.Update(q => q.p2, q => new Parent { ParentID = q.p1.ParentID });
			}
		}

		[Test, DataContextSource(ProviderName.Sybase, ProviderName.Informix)]
		public void UpdateAssociation1(string context)
		{
			using (var db = GetDataContext(context))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				try
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);

					db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
					db.Child. Insert(() => new Child  { ChildID = childId, ParentID = parentId });

					var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

					Assert.AreEqual(1, parents.Update(db.Parent, x => new Parent { Value1 = 5 }));
				}
				finally
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Sybase, ProviderName.Informix)]
		public void UpdateAssociation2(string context)
		{
			using (var db = GetDataContext(context))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				try
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);

					db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
					db.Child. Insert(() => new Child  { ChildID = childId, ParentID = parentId });

					var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

					Assert.AreEqual(1, parents.Update(x => new Parent { Value1 = 5 }));
				}
				finally
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Sybase, ProviderName.Informix)]
		public void UpdateAssociation3(string context)
		{
			using (var db = GetDataContext(context))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				try
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);

					db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
					db.Child. Insert(() => new Child  { ChildID = childId, ParentID = parentId });

					var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

					Assert.AreEqual(1, parents.Update(x => x.ParentID > 0, x => new Parent { Value1 = 5 }));
				}
				finally
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);
				}
			}
		}

		[Test, DataContextSource(ProviderName.Sybase, ProviderName.Informix)]
		public void UpdateAssociation4(string context)
		{
			using (var db = GetDataContext(context))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				try
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);

					db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
					db.Child. Insert(() => new Child  { ChildID = childId, ParentID = parentId });

					var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

					Assert.AreEqual(1, parents.Set(x => x.Value1, 5).Update());
				}
				finally
				{
					db.Child. Delete(x => x.ChildID  == childId);
					db.Parent.Delete(x => x.ParentID == parentId);
				}
			}
		}

		static readonly Func<TestDataConnection,int,string,int> _updateQuery =
			CompiledQuery.Compile<TestDataConnection,int,string,int>((ctx,key,value) =>
				ctx.Person
					.Where(_ => _.ID == key)
					.Set(_ => _.FirstName, value)
					.Update());

		[Test]
		public void CompiledUpdate()
		{
			using (var ctx = new TestDataConnection())
			{
				_updateQuery(ctx, 12345, "54321");
			}
		}

		[Table("LinqDataTypes")]
		class Table1
		{
			[Column] public int  ID;
			[Column] public bool BoolValue;

			[Association(ThisKey = "ID", OtherKey = "ParentID", CanBeNull = false)]
			public List<Table2> Tables2;
		}

		[Table("Parent")]
		class Table2
		{
			[Column] public int  ParentID;
			[Column] public bool Value1;

			[Association(ThisKey = "ParentID", OtherKey = "ID", CanBeNull = false)]
			public Table1 Table1;
		}

		[Test, DataContextSource(false,
			ProviderName.Access, 
			ProviderName.DB2, 
			ProviderName.Firebird, 
			ProviderName.Informix, 
			ProviderName.OracleNative,
			ProviderName.OracleManaged, 
			ProviderName.PostgreSQL, 
			ProviderName.SqlCe, 
			ProviderName.SQLite, 
			ProviderName.SapHana)]
		public void UpdateAssociation5(string context)
		{
			using (var db = new DataConnection(context))
			{
				var ids = new[] { 10000, 20000 };

				db.GetTable<Table2>()
					.Where (x => ids.Contains(x.ParentID))
					.Select(x => x.Table1)
					.Distinct()
					.Set(y => y.BoolValue, y => y.Tables2.All(x => x.Value1))
					.Update();

				var idx = db.LastQuery.IndexOf("INNER JOIN");

				Assert.That(idx, Is.Not.EqualTo(-1));

				idx = db.LastQuery.IndexOf("INNER JOIN", idx + 1);

				Assert.That(idx, Is.EqualTo(-1));
			}
		}

		[Test, DataContextSource(ProviderName.Informix)]
		public void AsUpdatableTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);
					db.Child.Insert(() => new Child { ParentID = 1, ChildID = id});

					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));

					var q  = db.Child.Where(c => c.ChildID == id && c.Parent.Value1 == 1);
					var uq = q.AsUpdatable();

					uq = uq.Set(c => c.ChildID, c => c.ChildID + 1);

					Assert.AreEqual(1, uq.Update());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id + 1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Table("GrandChild")]
		class Table3
		{
			[PrimaryKey(1)] public int? ParentID;
			[PrimaryKey(2)] public int? ChildID;
			[Column]        public int? GrandChildID;
		}

		[Test, DataContextSource]
		public void UpdateNullablePrimaryKey(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Update(new Table3 { ParentID = 10000, ChildID = null, GrandChildID = 1000 });

				if (db is DataConnection)
					Assert.IsTrue(((DataConnection)db).LastQuery.Contains("IS NULL"));

				db.Update(new Table3 { ParentID = 10000, ChildID = 111, GrandChildID = 1000 });

				if (db is DataConnection)
					Assert.IsFalse(((DataConnection)db).LastQuery.Contains("IS NULL"));
			}
		}

		[Test, DataContextSource(
			ProviderName.Access,
			ProviderName.DB2,
			ProviderName.Firebird,
			ProviderName.Informix,
			ProviderName.PostgreSQL,
			ProviderName.SQLite,
			ProviderName.SqlCe,
			ProviderName.SqlServer2000,
			ProviderName.SapHana
			)]
		public void UpdateTop(string context)
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
						.Set(p => p.Value1, 1)
						.Update();

					Assert.That(rowsAffected, Is.EqualTo(5));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		// TODO: [Test, DataContextSource]
		public void TestUpdateTake(string context)
		{
			using (var db = GetDataContext(context))
			{
				var entities =
					from x in db.Parent
					where x.ParentID > 1000
					orderby x.ParentID descending
					select x;

				entities
					.Take(10)
					.Update(x => new Parent { Value1 = 1 });
			}
		}

		[Test, DataContextSource(ProviderName.Access, ProviderName.Informix, ProviderName.SqlCe)]
		public void UpdateSetSelect(string context)
		{
			using (var db = GetDataContext(context))
			{
				(
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					where p.ParentID < 10000
					select p
				)
				.Set(p => p.ParentID, p => db.Child.SingleOrDefault(c => c.ChildID == 11).ParentID)
				.Update();
			}
		}

		[Test, DataContextSource(
			ProviderName.SQLite, ProviderName.Access, ProviderName.Informix, ProviderName.Firebird, ProviderName.PostgreSQL,
			ProviderName.MySql, TestProvName.MariaDB, ProviderName.Sybase)]
		public void UpdateIssue319Regression(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 100500;
				try
				{
					db.Insert(new Parent1()
					{
						ParentID = id
					});

					var query = db.GetTable<Parent1>()
						.Where(_ => _.ParentID == id)
						.Select(_ => new Parent1()
						{
							ParentID = _.ParentID
						});

					var queryResult = new Lazy<Parent1>(() => query.First());

					var cnt = db.GetTable<Parent1>()
						.Where(_ => _.ParentID == id && query.Count() > 0)
						.Update(_ => new Parent1()
						{
							Value1 = queryResult.Value.ParentID
						});

					Assert.AreEqual(1, cnt);
				}
				finally
				{
					db.GetTable<Parent1>().Delete(_ => _.ParentID == id);
				}
			}
		}

		[Test, DataContextSource(ProviderName.DB2, ProviderName.Informix, ProviderName.Firebird, ProviderName.Sybase)]
		public void UpdateIssue321Regression(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 100500;

				try
				{
					var value1 = 3000m;
					var value2 = 13621m;
					var value3 = 60;

					db.Insert(new LinqDataTypes2()
					{
						ID = id,
						MoneyValue = value1,
						IntValue = value3
					});

					db.GetTable<LinqDataTypes2>()
						.Update(_ => new LinqDataTypes2()
						{
							SmallIntValue = (short)(_.MoneyValue / (value2 / _.IntValue))
						});

					var dbResult = db.GetTable<LinqDataTypes2>()
						.Where(_ => _.ID == id)
						.Select(_ => _.SmallIntValue).First();

					var expected = (short)(value1 / (value2 / value3));

					Assert.AreEqual(expected, dbResult);
				}
				finally
				{
					db.GetTable<LinqDataTypes2>().Delete(c => c.ID == id);
				}
			}
		}
	}
}
