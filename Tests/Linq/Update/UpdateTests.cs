﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
//	[Order(10000)]
	public class UpdateTests : TestBase
	{
		[Test]
		public void Update1([DataSources] string context)
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

		[Test]
		public async Task Update1Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

					await db.Parent.DeleteAsync(p => p.ParentID > 1000);
					await db.InsertAsync(parent);

					Assert.AreEqual(1, await db.Parent.CountAsync (p => p.ParentID == parent.ParentID));
					Assert.AreEqual(1, await db.Parent.UpdateAsync(p => p.ParentID == parent.ParentID, p => new Parent { ParentID = p.ParentID + 1 }));
					Assert.AreEqual(1, await db.Parent.CountAsync (p => p.ParentID == parent.ParentID + 1));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Update2([DataSources] string context)
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

		[Test]
		public async Task Update2Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

					await db.Parent.DeleteAsync(p => p.ParentID > 1000);
					await db.InsertAsync(parent);

					Assert.AreEqual(1, await db.Parent.CountAsync(p => p.ParentID == parent.ParentID));
					Assert.AreEqual(1, await db.Parent.Where(p => p.ParentID == parent.ParentID).UpdateAsync(p => new Parent { ParentID = p.ParentID + 1 }));
					Assert.AreEqual(1, await db.Parent.CountAsync(p => p.ParentID == parent.ParentID + 1));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Update3([DataSources(ProviderName.Informix)] string context)
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

		[Test]
		public void Update4([DataSources(ProviderName.Informix)] string context)
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

		[Test]
		public async Task Update4Async([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					await db.Child.DeleteAsync(c => c.ChildID > 1000);
					await db.Child.InsertAsync(() => new Child { ParentID = 1, ChildID = id});

					Assert.AreEqual(1, await db.Child.CountAsync(c => c.ChildID == id));
					Assert.AreEqual(1,
						await db.Child
							.Where(c => c.ChildID == id && c.Parent.Value1 == 1)
								.Set(c => c.ChildID, c => c.ChildID + 1)
							.UpdateAsync());
					Assert.AreEqual(1, await db.Child.CountAsync(c => c.ChildID == id + 1));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Update5([DataSources(ProviderName.Informix)] string context)
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

		[Test]
		public void Update6([DataSources(ProviderName.Informix)] string context)
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

		[Test]
		public void Update7([DataSources(ProviderName.Informix)] string context)
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

		[Test]
		public void Update8([DataSources] string context)
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

		[Test]
		public void Update9(
			[DataSources(
				ProviderName.Informix,
				ProviderName.SqlCe,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllSQLite,
				ProviderName.Access,
				ProviderName.SapHana)]
			string context)
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

		[Test]
		public void Update10(
			[DataSources(
				ProviderName.Informix,
				ProviderName.SqlCe,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllSQLite,
				ProviderName.Access,
				ProviderName.SapHana)]
			string context)
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

		//[Test]
		public void Update11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var q = db.GetTable<LinqDataTypes2>().Union(db.GetTable<LinqDataTypes2>());

				//db.GetTable<LinqDataTypes2>().Update(_ => q.Contains(_), _ => new LinqDataTypes2 { GuidValue = _.GuidValue });

				q.Update(_ => new LinqDataTypes2 { GuidValue = _.GuidValue });
			}
		}

		[Test]
		public void Update12(
			[DataSources(
				ProviderName.SqlCe,
				ProviderName.DB2,
				ProviderName.Informix,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.SapHana)]
			string context)
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

		[Test]
		public async Task Update12Async(
			[DataSources(
				ProviderName.SqlCe,
				ProviderName.DB2,
				ProviderName.Informix,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.SapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				await (
					from p1 in db.Parent
					join p2 in db.Parent on p1.ParentID equals p2.ParentID
					where p1.ParentID < 3
					select new { p1, p2 }
				)
				.UpdateAsync(q => q.p1, q => new Parent { ParentID = q.p2.ParentID });
			}
		}

		[Test]
		public void Update13(
			[DataSources(
				ProviderName.SqlCe,
				ProviderName.DB2,
				ProviderName.Informix,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.SapHana)]
			string context)
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

		[Test]
		public void UpdateComplex1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(_ => _.FirstName.StartsWith("UpdateComplex")).Delete();
				try
				{

					var id = Convert.ToInt32(db.InsertWithIdentity(
						new ComplexPerson2
						{
							Name = new FullName
							{
								FirstName = "UpdateComplex",
								LastName  = "Empty"
							}
						}));

					var obj = db.GetTable<ComplexPerson2>().First(_ => _.ID == id);
					obj.Name.LastName = obj.Name.FirstName;

					db.Update(obj);

					obj = db.GetTable<ComplexPerson2>().First(_ => _.ID == id);

					Assert.AreEqual(obj.Name.FirstName, obj.Name.LastName);
				}
				finally
				{
					db.Person.Where(_ => _.FirstName.StartsWith("UpdateComplex")).Delete();
				}
			}
		}

		[Test]
		public async Task UpdateComplex1Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				await db.Person.DeleteAsync(_ => _.FirstName.StartsWith("UpdateComplex"));

				try
				{

					var id = Convert.ToInt32(await db.InsertWithIdentityAsync(
						new ComplexPerson2
						{
							Name = new FullName
							{
								FirstName = "UpdateComplex",
								LastName  = "Empty"
							}
						}));

					var obj = await db.GetTable<ComplexPerson2>().FirstAsync(_ => _.ID == id);
					obj.Name.LastName = obj.Name.FirstName;

					await db.UpdateAsync(obj);

					obj = await db.GetTable<ComplexPerson2>().FirstAsync(_ => _.ID == id);

					Assert.AreEqual(obj.Name.FirstName, obj.Name.LastName);
				}
				finally
				{
					await db.Person.Where(_ => _.FirstName.StartsWith("UpdateComplex")).DeleteAsync();
				}
			}
		}

		[Test]
		public void UpdateComplex2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(_ => _.FirstName.StartsWith("UpdateComplex")).Delete();
				try
				{

					var id = Convert.ToInt32(db.InsertWithIdentity(
						new ComplexPerson2()
						{
							Name = new FullName
							{
								FirstName = "UpdateComplex",
								LastName  = "Empty"
							}
						}));

					var cnt = db.GetTable<ComplexPerson2>()
						.Where(_ => _.Name.FirstName.StartsWith("UpdateComplex"))
						.Set(_ => _.Name.LastName, _ => _.Name.FirstName)
						.Update();

					Assert.AreEqual(1, cnt);

					var obj = db.GetTable<ComplexPerson2>().First(_ => _.ID == id);

					Assert.AreEqual(obj.Name.FirstName, obj.Name.LastName);
				}
				finally
				{
					db.Person.Where(_ => _.FirstName.StartsWith("UpdateComplex")).Delete();
				}

			}
		}

		[Test]
		public void UpdateAssociation1([DataSources(TestProvName.AllSybase, ProviderName.Informix)] string context)
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

		[Test]
		public async Task UpdateAssociation1Async([DataSources(TestProvName.AllSybase, ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				try
				{
					await db.Child. DeleteAsync(x => x.ChildID  == childId);
					await db.Parent.DeleteAsync(x => x.ParentID == parentId);

					await db.Parent.InsertAsync(() => new Parent { ParentID = parentId, Value1 = parentId });
					await db.Child. InsertAsync(() => new Child  { ChildID = childId, ParentID = parentId });

					var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

					Assert.AreEqual(1, await parents.UpdateAsync(db.Parent, x => new Parent { Value1 = 5 }));
				}
				finally
				{
					await db.Child. DeleteAsync(x => x.ChildID  == childId);
					await db.Parent.DeleteAsync(x => x.ParentID == parentId);
				}
			}
		}

		[Test]
		public void UpdateAssociation2([DataSources(TestProvName.AllSybase, ProviderName.Informix)] string context)
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

		[Test]
		public void UpdateAssociation3([DataSources(TestProvName.AllSybase, ProviderName.Informix)] string context)
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

		[Test]
		public void UpdateAssociation4([DataSources(TestProvName.AllSybase, ProviderName.Informix)] string context)
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

		[Test]
		public void UpdateAssociation5(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Informix,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				ProviderName.SapHana)]
			string context)
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

		[Test]
		public void AsUpdatableTest([DataSources(ProviderName.Informix)] string context)
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

		[Test]
		public void UpdateNullablePrimaryKey([DataSources] string context)
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

		[Test]
		public void UpdateTop(
			[DataSources(
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Informix,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000,
				ProviderName.SapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Parent.Delete(c => c.ParentID >= 1000);

					using (new DisableLogging())
					{
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent { ParentID = 1000 + i });
					}

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

		[Test]
		public void TestUpdateTakeOrdered(
			[DataSources(
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Informix,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000,
				ProviderName.SapHana,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllMySql,
				TestProvName.AllSybase,
				TestProvName.AllOracle)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					using (new DisableLogging())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent { ParentID = 1000 + i });
					}

					var entities =
						from x in db.Parent
						where x.ParentID > 1000
						orderby x.ParentID descending
						select x;

					var rowsAffected = entities
						.Take(5)
						.Update(x => new Parent { Value1 = 1 });

					Assert.That(rowsAffected, Is.EqualTo(5));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void TestUpdateSkipTake(
			[DataSources(
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Informix,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000,
				ProviderName.SapHana,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllMySql,
				TestProvName.AllSybase,
				TestProvName.AllOracle)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					using (new DisableLogging())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent {ParentID = 1000 + i});
					}

					var entities =
						from x in db.Parent
						where x.ParentID > 1000
						orderby x.ParentID descending
						select x;

					var rowsAffected = entities
						.Skip(1)
						.Take(5)
						.Update(x => new Parent { Value1 = 1 });

					Assert.That(rowsAffected, Is.EqualTo(5));

					Assert.False(db.Parent.Where(p => p.ParentID == 1000 + 9).Single().Value1 == 1);
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void TestUpdateTakeNotOrdered(
			[DataSources(
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Informix,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000,
				ProviderName.SapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					using (new DisableLogging())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						for (var i = 0; i < 10; i++)
							db.Insert(new Parent {ParentID = 1000 + i});
					}

					var entities =
						from x in db.Parent
						where x.ParentID > 1000
						select x;

					var rowsAffected = entities
						.Take(5)
						.Update(x => new Parent { Value1 = 1 });

					Assert.That(rowsAffected, Is.EqualTo(5));
				}
				finally
				{
					db.Parent.Delete(c => c.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void UpdateSetSelect([DataSources(
			ProviderName.Access, ProviderName.Informix, ProviderName.SqlCe)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Delete(_ => _.ParentID > 1000);

				var res =
				(
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					where p.ParentID == 1
					select p
				)
				.Set(p => p.ParentID, p => db.Child.SingleOrDefault(c => c.ChildID == 11).ParentID + 1000)
				.Update();

				Assert.AreEqual(1, res);

				res = db.Parent.Where(_ => _.ParentID == 1001).Set(_ => _.ParentID, 1).Update();
				Assert.AreEqual(1, res);
			}
		}

		[Test]
		public void UpdateIssue319Regression(
			[DataSources(
				ProviderName.Access,
				ProviderName.Informix,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllMySql,
				TestProvName.AllSybase,
				ProviderName.SapHana)]
			string context)
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

		// looks like managed provider handle null bit parameters as false, because it doesn't fail
		// maybe we need to do the same for unmanaged
		[ActiveIssue("AseException : Null value is not allowed in BIT TYPE", Configuration = ProviderName.Sybase)]
		[Test]
		public void UpdateIssue321Regression([DataSources(ProviderName.DB2, ProviderName.Informix, TestProvName.AllFirebird)] string context)
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
						ID         = id,
						MoneyValue = value1,
						IntValue   = value3
					});

					db.GetTable<LinqDataTypes2>()
						.Update(
							_ => _.ID == id,
							_ => new LinqDataTypes2
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

		[Test()]
		public void UpdateMultipleColumns([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ldt = new LinqDataTypes
				{
					ID            = 1001,
					MoneyValue    = 1000,
					SmallIntValue = 100,
				};

				try
				{
					db.Types.Delete(c => c.ID == ldt.ID);
					db.Types
						.Value (t => t.ID,            ldt.ID)
						.Value (t => t.MoneyValue,    () => ldt.MoneyValue)
						.Value (t => t.SmallIntValue, () => ldt.SmallIntValue)
						.Insert()
						;

					db.Types
						.Where (t => t.ID == ldt.ID)
						.Set   (t => t.MoneyValue,    () => 2000)
						.Set   (t => t.SmallIntValue, () => 200)
						.Update()
						;

					var udt = db.Types.Single(t => t.ID == ldt.ID);

					Assert.That(udt.MoneyValue,    Is.Not.EqualTo(ldt.MoneyValue));
					Assert.That(udt.SmallIntValue, Is.Not.EqualTo(ldt.SmallIntValue));
				}
				finally
				{
					db.Types.Delete(t => t.ID == ldt.ID);
				}
			}
		}

		[ActiveIssue(
			Configuration = ProviderName.OracleNative,
			Details       = "ORA-00955: name is already used by an existing object")]
		[Test]
		public void UpdateByTableName([DataSources] string context)
		{
			const string schemaName = null;
			var tableName  = InsertTests.GetTableName(context, "32");

			using (var db = GetDataContext(context))
			{
				db.DropTable<Patient>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
			}

			using (var db = GetDataContext(context))
			{
				var table = db.CreateTable<Person>(tableName, schemaName: schemaName);

				Assert.AreEqual(tableName,  table.TableName);
				Assert.AreEqual(schemaName, table.SchemaName);

				var person = new Person()
				{
					FirstName = "Steven",
					LastName  = "King",
					Gender    = Gender.Male,
				};

				// insert a row into the table
				db.Insert(person, tableName: tableName, schemaName: schemaName);
				var newCount  = table.Count();
				Assert.AreEqual(1, newCount);

				var personForUpdate = table.Single();

				// update that row
				personForUpdate.MiddleName = "None";
				db.Update(personForUpdate, tableName: tableName, schemaName: schemaName);

				var updatedPerson = table.Single();
				Assert.AreEqual("None", updatedPerson.MiddleName);

				table.Drop();
			}
		}

		[ActiveIssue(
			Configuration = ProviderName.OracleNative,
			Details       = "ORA-00955: name is already used by an existing object")]
		[Test]
		public async Task UpdateByTableNameAsync([DataSources] string context)
		{
			const string schemaName = null;
			var tableName  = InsertTests.GetTableName(context, "33");

			using (var db = GetDataContext(context))
			{
				await db.DropTableAsync<Patient>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
			}

			using (var db = GetDataContext(context))
			{
				var table = await db.CreateTableAsync<Person>(tableName, schemaName: schemaName);

				Assert.AreEqual(tableName,  table.TableName);
				Assert.AreEqual(schemaName, table.SchemaName);

				var person = new Person()
				{
					FirstName = "Steven",
					LastName  = "King",
					Gender    = Gender.Male,
				};

				// insert a row into the table
				await db.InsertAsync(person, tableName: tableName, schemaName: schemaName);
				var newCount  = await table.CountAsync();
				Assert.AreEqual(1, newCount);

				var personForUpdate = await table.SingleAsync();

				// update that row
				personForUpdate.MiddleName = "None";
				await db.UpdateAsync(personForUpdate, tableName: tableName, schemaName: schemaName);

				var updatedPerson = await table.SingleAsync();
				Assert.AreEqual("None", updatedPerson.MiddleName);

				await table.DropAsync();
			}
		}
	}
}
