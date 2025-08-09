using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ComplexTests : TestBase
	{
		[Test]
		public void Contains1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from gc1 in GrandChild
						join max in
							from gch in GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc1.GrandChildID equals max
					select gc1;

				var expected =
					from ch in Child
						join p   in Parent on ch.ParentID equals p.ParentID
						join gc2 in q1     on p.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
					where gc3 == null || !new[] { 111, 222 }.Contains(gc3.GrandChildID!.Value)
					select new { p.ParentID, gc3 };

				var q2 =
					from gc1 in db.GrandChild
						join max in
							from gch in db.GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc1.GrandChildID equals max
					select gc1;

				var result =
					from ch in db.Child
						join p   in db.Parent on ch.ParentID equals p.ParentID
						join gc2 in q2        on p.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
				where gc3 == null || !new[] { 111, 222 }.Contains(gc3.GrandChildID!.Value)
				select new { p.ParentID, gc3 };

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Contains2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from gc in GrandChild
						join max in
							from gch in GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var expected =
					from ch in Child
						join p  in Parent on ch.ParentID equals p.ParentID
						join gc in q1     on p.ParentID  equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
					where gc == null || gc.GrandChildID != 111 && gc.GrandChildID != 222
					select new
					{
						Parent       = p,
						GrandChildID = gc,
						Value        = GetValue(gc != null ? gc.ChildID : int.MaxValue)
					};

				var q2 =
					from gc in db.GrandChild
						join max in
							from gch in db.GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var result =
					from ch in db.Child
						join p  in db.Parent on ch.ParentID equals p.ParentID
						join gc in q2        on p.ParentID  equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
				where gc == null || gc.GrandChildID != 111 && gc.GrandChildID != 222
				select new
				{
					Parent       = p,
					GrandChildID = gc,
					Value        = GetValue(gc != null ? gc.ChildID : int.MaxValue)
				};

				AreEqual(expected, result);
			}
		}

		static int GetValue(int? value)
		{
			return value ?? 777;
		}

		[Test]
		public void Contains3([DataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from gc in GrandChild1
						join max in
							from gch in GrandChild1
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var expected =
					from ch in Child
						join p  in Parent on ch.ParentID equals p.ParentID
						join gc in q1     on p.ParentID  equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
					where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID!.Value)
					select new { p.ParentID, gc };

				var q2 =
					from gc in db.GrandChild1
						join max in
							from gch in db.GrandChild1
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var result =
					from ch in db.Child
						join p  in db.Parent on ch.ParentID equals p.ParentID
						join gc in q2        on p.ParentID  equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
					where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID!.Value)
					select new { p.ParentID, gc };

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Contains4([DataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from gc in GrandChild1
						join max in
							from gch in GrandChild1
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var expected =
					from ch in Child
						join gc in q1 on ch.Parent!.ParentID equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
					where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID!.Value)
					select new { ch.Parent, gc };

				var q2 =
					from gc in db.GrandChild1
						join max in
							from gch in db.GrandChild1
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var result =
					from ch in db.Child
						join gc in q2 on ch.Parent!.ParentID equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
				where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID!.Value)
				select new { ch.Parent, gc };

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Contains5([DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Child.Where(c =>    Parent.Skip(1).Take(100).Select(p => p.ParentID).Contains(c.ParentID)),
					db.Child.Where(c => db.Parent.Skip(1).Take(100).Select(p => p.ParentID).Contains(c.ParentID))
					);
			}
		}

		[Test]
		public void Contains6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Child.Where(c =>    Parent.Select(p => p.ParentID).Contains(c.ParentID)),
					db.Child.Where(c => db.Parent.Select(p => p.ParentID).Contains(c.ParentID))
					);
			}
		}

		[Test]
		public void Join1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =
					from p in Parent
						join c in Child      on p.ParentID equals c.ParentID
						join g in GrandChild on p.ParentID equals g.ParentID
					select new { p, c, g };

				var expected =
					from x in q1
					where
					(
						(x.c.ParentID == 2 || x.c.ParentID == 3) && x.g.ChildID != 21 && x.g.ChildID != 33
					) || (
						x.g.ParentID == 3 && x.g.ChildID == 32
					) || (
						x.g.ChildID == 11
					)
					select x;

				var q2 =
					from p in db.Parent
						join c in db.Child      on p.ParentID equals c.ParentID
						join g in db.GrandChild on p.ParentID equals g.ParentID
					select new { p, c, g };

				var result =
					from x in q2
					where
					(
						(x.c.ParentID == 2 || x.c.ParentID == 3) && x.g.ChildID != 21 && x.g.ChildID != 33
					) || (
						x.g.ParentID == 3 && x.g.ChildID == 32
					) || (
						x.g.ChildID == 11
					)
					select x;

					AreEqual(expected, result);
			}
		}

		public class MyObject
		{
			public Parent? Parent;
			public Child?  Child;
		}

		IQueryable<MyObject> GetData(ITestDataContext db, int id)
		{
			var q =
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ChildID
				where p.ParentID == id && c.ChildID > 0
				select new MyObject { Parent = p, Child = c };

			return q;
		}

		[Test]
		public void Join2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from o in GetData(db, 1)
					from g in o.Parent!.GrandChildren
					select new { o, g };

				var _ = q.ToList();
			}
		}

		[Test]
		public void ExpressionTest1([NorthwindDataContext] string context)
		{
			Expression<Func<Northwind.Customer,bool>> pred1 = cust=>cust.Country=="UK";
			Expression<Func<Northwind.Customer,bool>> pred2 = cust=>cust.Country=="France";

			var param = Expression.Parameter(typeof(Northwind.Customer), "x");
			var final = Expression.Lambda<Func<Northwind.Customer, bool>>(
				Expression.OrElse(
					Expression.Invoke(pred1, param),
					Expression.Invoke(pred2, param)
				), param);

			using (var db = new NorthwindDB(context))
			{
				var _ = db.Customer.Count(final);
			}
		}

		[Test]
		public void ExpressionTest2()
		{
			Expression<Func<Parent,bool>> pred1 = _=>_.ParentID == 1;
			Expression<Func<Parent,bool>> pred2 = _=>_.Value1   == 1 || _.Value1 == null;

			var param = Expression.Parameter(typeof(Parent), "x");
			var final = Expression.Lambda<Func<Parent, bool>>(
				Expression.AndAlso(
					Expression.Invoke(pred1, param),
					Expression.Invoke(pred2, param)
				), param);

			using (var db = new TestDataConnection())
			{
				Assert.That(db.Parent.Count(final), Is.EqualTo(1));
			}
		}

		#region IEnumerableTest

		public class Entity
		{
			public int Id { get; set; }
		}

		public enum TestEntityType : byte { Type1, Type2 }

		[Table("GrandChild")]
		[Column("GrandChildID", "Id")]
		[Column("ChildID",      "InnerEntity.Id")]
		[Column("ParentID",     "InnerEntityType")]
		public class LookupEntity : Entity
		{
			public Entity?        InnerEntity     { get; set; }
			public TestEntityType InnerEntityType { get; set; }
		}

		[Table(Name="GrandChild")]
		[Column("GrandChildID", "Id")]
		[Column("ChildID",      "Owner.Id")]
		[Column("ParentID",     "EntityType")]
		public class TestEntityBase : Entity
		{
			public TestEntityType EntityType { get; set; }
			public SuperAccount?  Owner      { get; set; }
		}

		public class TestEntity : TestEntityBase, IEnumerable<object>
		{
			#region IEnumerable<object> Members

			public IEnumerator<object> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		public class TestEntity2 : TestEntityBase
		{
		}

		public enum SuperAccountType { Client, Organization }

		[Table("GrandChild")]
		[Column("GrandChildID", "Id")]
		[Column("ParentID",     "Type")]
		public class SuperAccount : Entity, IEnumerable<object>
		{
			public List<Entity>     InnerAccounts { get; set; } = null!;
			public SuperAccountType Type          { get; set; }

			#region IEnumerable<object> Members

			public IEnumerator<object> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		[Test]
		public void IEnumerableTest1()
		{
			using (var db = new DataConnection())
			{
				var query =
					from rc in db.GetTable<TestEntity>()
					join li in db.GetTable<LookupEntity>() on rc.Id equals li.InnerEntity!.Id
					where rc.EntityType == TestEntityType.Type1
					select rc;

				var _ = query.ToList();
			}
		}

		[Test]
		public void IEnumerableTest2()
		{
			using (var db = new DataConnection())
			{
				var zones =
					from z in db.GetTable<TestEntity2>()
					join o in db.GetTable<SuperAccount>() on z.Owner!.Id equals o.Id
					select z;

				var _ = zones.ToList();
			}
		}

		#endregion

		[Table("T1")]
		public class T1
		{
			[PrimaryKey] public int      InstrumentId         { get; set; }
			[Column]     public string?  InstrumentCode       { get; set; }
			[Column]     public DateTime CreateDate           { get; set; }
			[Column]     public string?  SourceInstrumentCode { get; set; }
		}

		[Table("T2")]
		public class T2
		{
			[Column, PrimaryKey] public int InstrumentId { get; set; }
			[Column, PrimaryKey] public int IndexId { get; set; }

		}

		[Table("T3")]
		public class T3
		{
			[Column, PrimaryKey] public int InstrumentId { get; set; }
			[Column, PrimaryKey] public int IndexId { get; set; }
		}

		[Test]
		public void Issue413Test([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<T1>())
			using (db.CreateLocalTable<T2>())
			using (db.CreateLocalTable<T3>())
			{
				string cond = "aaa";
				DateTime uptoDate = TestData.DateTime;

				db.Insert(new T3 { IndexId = 1, InstrumentId = 1 });
				db.Insert(new T3 { IndexId = 1, InstrumentId = 2 });
				db.Insert(new T3 { IndexId = 1, InstrumentId = 3 });
				db.Insert(new T2 { IndexId = 1, InstrumentId = 1 });
				db.Insert(new T2 { IndexId = 1, InstrumentId = 2 });

				db.Insert(new T1 { InstrumentId = 1, CreateDate = TestData.DateTime.AddDays(-1), InstrumentCode = "aaa1", SourceInstrumentCode = "NOTNULL" });
				db.Insert(new T1 { InstrumentId = 2, CreateDate = TestData.DateTime.AddDays(-1), InstrumentCode = "aaa2", SourceInstrumentCode = null });

				var res = db.GetTable<T1>()
					.Where(_ => _.InstrumentCode!.StartsWith(cond) && _.CreateDate <= uptoDate)
					.Join(db.GetTable<T2>(), _ => _.InstrumentId, _ => _.InstrumentId, (ins, idx) => idx.IndexId)
					.Join(db.GetTable<T3>(), _ => _,              _ => _.IndexId,      (idx, w)   => w.InstrumentId)
					.Join(db.GetTable<T1>(), _ => _,              _ => _.InstrumentId, (w, ins)   => ins.SourceInstrumentCode)
					.Where(_ => _ != null)
					.Distinct()
					.OrderBy(_ => _)
					.ToList();

//				db.GetTable<T1>().Truncate();
//				db.GetTable<T2>().Truncate();
//				db.GetTable<T3>().Truncate();
//
//				_ = db.Person.ToList();

				Assert.That(res, Has.Count.EqualTo(1));
			}
		}

		public class Address
		{
			public string? City { get; set; }
			public string? Street { get; set; }
			public int Building { get; set; }
		}

		[Column("city", "Residence.City")]
		[Column("user_name", "Name")]
		public class User
		{
			[PrimaryKey] public int Id { get; set; }

			public string? Name;

			[Column("street", ".Street")]
			[Column("building_number", MemberName = ".Building")]
			public Address? Residence { get; set; }

			public static readonly User[] TestData = new []
			{
				new User()
				{
					Name = "Freddy",
					Residence = new Address()
					{
						Building = 13,
						City     = "Springwood",
						Street   = "Elm Street"
					}
				}
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/999")]
		public void SelectCompositeTypeSpecificColumnTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var users = db.CreateLocalTable<User>())
			{
				var query = users.Select(u => u.Residence!.City);
				Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(1));

				query.ToList();

				query = users.Select(u => u.Residence!.Street);
				Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(1));

				query.ToList();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2590")]
		public void SelectCompositeTypeAllColumnsTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var users = db.CreateLocalTable(User.TestData))
			{
				var result = users.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Name, Is.EqualTo(User.TestData[0].Name));
					Assert.That(result[0].Residence, Is.Not.Null);
					Assert.That(result[0].Residence!.Building, Is.EqualTo(User.TestData[0].Residence!.Building));
					Assert.That(result[0].Residence!.City, Is.EqualTo(User.TestData[0].Residence!.City));
					Assert.That(result[0].Residence!.Street, Is.EqualTo(User.TestData[0].Residence!.Street));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/940")]
		public void FilterByComposite_Class([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var users = db.CreateLocalTable(User.TestData);

			var record = users.Where(u => u.Residence == User.TestData[0].Residence).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Name, Is.EqualTo("Freddy"));
				Assert.That(record.Residence, Is.Not.Null);
				Assert.That(record.Residence!.Building, Is.EqualTo(User.TestData[0].Residence!.Building));
				Assert.That(record.Residence.City, Is.EqualTo(User.TestData[0].Residence!.City));
				Assert.That(record.Residence.Street, Is.EqualTo(User.TestData[0].Residence!.Street));
			}
		}

		struct AddressStruct : IEquatable<AddressStruct>
		{
			public string? City { get; set; }
			public string? Street { get; set; }
			public int Building { get; set; }

			public static bool operator ==(AddressStruct key1, AddressStruct key2)
			{
				return key1.City == key2.City && key1.Street == key2.Street && key1.Building == key2.Building;
			}

			public static bool operator !=(AddressStruct key1, AddressStruct key2)
			{
				return key1.City != key2.City || key1.Street != key2.Street || key1.Building != key2.Building;
			}

			public override int GetHashCode()
			{
				return City?.GetHashCode() ?? 0 ^ Street?.GetHashCode() ?? 0 ^ Building.GetHashCode();
			}

			public override bool Equals(object? obj)
			{
				if (obj is not AddressStruct other)
					return false;

				return Equals(other);
			}

			public bool Equals(AddressStruct other)
			{
				return City == other.City
					&& Street == other.Street
					&& Building == other.Building;
			}
		}

		[Column("city", "Residence.City")]
		[Column("user_name", "Name")]
		class UserStruct
		{
			[PrimaryKey] public int Id { get; set; }

			public string? Name;

			[Column("street", ".Street")]
			[Column("building_number", MemberName = ".Building")]
			public AddressStruct Residence { get; set; }

			public static readonly UserStruct[] TestData = new []
			{
				new UserStruct()
				{
					Name = "Freddy",
					Residence = new AddressStruct()
					{
						Building = 13,
						City     = "Springwood",
						Street   = "Elm Street"
					}
				}
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/940")]
		public void FilterByComposite_Struct([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetScalarType(typeof(AddressStruct), false);

			using var db    = GetDataContext(context, ms);
			using var users = db.CreateLocalTable(UserStruct.TestData);

			var record = users.Where(u => u.Residence == UserStruct.TestData[0].Residence).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Name, Is.EqualTo("Freddy"));
				Assert.That(record.Residence!.Building, Is.EqualTo(UserStruct.TestData[0].Residence.Building));
				Assert.That(record.Residence.City, Is.EqualTo(UserStruct.TestData[0].Residence.City));
				Assert.That(record.Residence.Street, Is.EqualTo(UserStruct.TestData[0].Residence.Street));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2874")]
		public void SelectCompositePropertyMapped_Class([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var users = db.CreateLocalTable(User.TestData);

			var residence = users.Select(u => u.Residence).Distinct().Single();

			Assert.That(residence, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(residence!.Building, Is.EqualTo(User.TestData[0].Residence!.Building));
				Assert.That(residence.City, Is.EqualTo(User.TestData[0].Residence!.City));
				Assert.That(residence.Street, Is.EqualTo(User.TestData[0].Residence!.Street));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2874")]
		public void SelectCompositePropertyMapped_Struct([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetScalarType(typeof(AddressStruct), false);

			using var db    = GetDataContext(context, ms);
			using var users = db.CreateLocalTable(UserStruct.TestData);

			var residence = users.Select(u => u.Residence).Distinct().Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(residence.Building, Is.EqualTo(UserStruct.TestData[0].Residence.Building));
				Assert.That(residence.City, Is.EqualTo(UserStruct.TestData[0].Residence.City));
				Assert.That(residence.Street, Is.EqualTo(UserStruct.TestData[0].Residence.Street));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4568")]
		public void SelectCompositePropertyMapped_Class_OnlyRequiredColumns([DataSources] string context)
		{
			using var db    = GetDataContext(context);

			var query = db.GetTable<User>().Select(u => u.Residence);

			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(3));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4568")]
		public void SelectCompositePropertyMapped_Struct_OnlyRequiredColumns([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetScalarType(typeof(AddressStruct), false);

			using var db    = GetDataContext(context, ms);

			var query = db.GetTable<UserStruct>().Select(u => u.Residence);

			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(3));
		}

		#region Issue 4139
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4139")]
		public void Issue4139Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4139Table.Data);

			var records = tb.LoadWith(t => t.Parent!.Parent).OrderBy(r => r.Id).ToArray();

			Assert.That(records.Count, Is.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(records[0].Parent, Is.Null);
				Assert.That(records[1].Parent, Is.Not.Null);
				Assert.That(records[1].Parent!.ParentId, Is.EqualTo(1));
				Assert.That(records[1].Parent!.Parent, Is.Not.Null);
			}

			Assert.That(records[1].Parent!.Parent!.Id, Is.EqualTo(1));
		}

		[Table]
		sealed class Issue4139Table
		{
			[PrimaryKey] public int Id { get; set; }

			[Column("ParentId", ".ParentId")]
			// TODO: missing ctor
			//[Association(".Parent", ThisKey = ".ParentId", OtherKey = "Id")]
			public Issue4139Parent? Parent { get; set; }

			public static readonly Issue4139Table[] Data = new[]
			{
				new Issue4139Table() { Id = 1 },
				new Issue4139Table() { Id = 2, Parent = new() { ParentId = 1 } }
			};
		}

		sealed class Issue4139Parent
		{
			public int? ParentId { get; set; }
			public Issue4139Table? Parent { get; set; }
		}
		#endregion
	}
}
