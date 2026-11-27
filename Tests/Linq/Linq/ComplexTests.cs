using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text.Json;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ComplexTests : TestBase
	{
		[Test]
		public void Contains1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void Contains2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
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
					where gc == null || (gc.GrandChildID != 111 && gc.GrandChildID != 222)
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
					where gc == null || (gc.GrandChildID != 111 && gc.GrandChildID != 222)
					select new
					{
						Parent       = p,
						GrandChildID = gc,
						Value        = GetValue(gc != null ? gc.ChildID : int.MaxValue)
					};

			AreEqual(expected, result);
		}

		static int GetValue(int? value)
		{
			return value ?? 777;
		}

		[Test]
		public void Contains3([DataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void Contains4([DataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void Contains5([DataSources(TestProvName.AllNativeAccess, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Where(c => Parent.Skip(1).Take(100).Select(p => p.ParentID).Contains(c.ParentID)),
				db.Child.Where(c => db.Parent.Skip(1).Take(100).Select(p => p.ParentID).Contains(c.ParentID))
				);
		}

		[Test]
		public void Contains6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.Where(c => Parent.Select(p => p.ParentID).Contains(c.ParentID)),
				db.Child.Where(c => db.Parent.Select(p => p.ParentID).Contains(c.ParentID))
				);
		}

		[Test]
		public void Join1([DataSources] string context)
		{
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
			var q =
					from o in GetData(db, 1)
					from g in o.Parent!.GrandChildren
					select new { o, g };

			var _ = q.ToList();
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

			using var db = new NorthwindDB(context);
			var _ = db.Customer.Count(final);
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

			using var db = new TestDataConnection();
			Assert.That(db.Parent.Count(final), Is.EqualTo(1));
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

		public class TestEntity2 : TestEntityBase;

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
			using var db = new DataConnection();
			var query =
					from rc in db.GetTable<TestEntity>()
					join li in db.GetTable<LookupEntity>() on rc.Id equals li.InnerEntity!.Id
					where rc.EntityType == TestEntityType.Type1
					select rc;

			var _ = query.ToList();
		}

		[Test]
		public void IEnumerableTest2()
		{
			using var db = new DataConnection();
			var zones =
					from z in db.GetTable<TestEntity2>()
					join o in db.GetTable<SuperAccount>() on z.Owner!.Id equals o.Id
					select z;

			var _ = zones.ToList();
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/413")]
		public void ChainedJoinsAcrossThreeTables([DataSources(false)] string context)
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
					Id = 1,
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
			using var db = GetDataContext(context);
			using var users = db.CreateLocalTable<User>();
			var query = users.Select(u => u.Residence!.City);
			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(1));

			query.ToList();

			query = users.Select(u => u.Residence!.Street);
			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(1));

			query.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2590")]
		public void SelectCompositeTypeAllColumnsTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var users = db.CreateLocalTable(User.TestData);
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

			public override readonly int GetHashCode()
			{
				return HashCode.Combine(City, Street, Building);
			}

			public override readonly bool Equals(object? obj)
			{
				if (obj is not AddressStruct other)
					return false;

				return Equals(other);
			}

			public readonly bool Equals(AddressStruct other)
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
					Id = 1,
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
		public void SelectCompositePropertyMapped_Class_OnlyRequiredColumns([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);

			var query = db.GetTable<User>().Select(u => u.Residence);

			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(3));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4568")]
		public void SelectCompositePropertyMapped_Struct_OnlyRequiredColumns([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			ms.SetScalarType(typeof(AddressStruct), false);

			using var db    = GetDataContext(context, ms);

			var query = db.GetTable<UserStruct>().Select(u => u.Residence);

			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(3));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4568")]
		public void SelectCompositePropertyMapped_Class_Distinct_OnlyRequiredColumns([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetTable<User>().Select(u => u.Residence).Distinct();

			query.GetSelectQuery().Select.Columns.Count.ShouldBe(3);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4568")]
		public void SelectCompositePropertyMapped_Class_Projected_OnlyRequiredColumns([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetTable<User>().Select(u => new { Residence = u.Residence });

			query.GetSelectQuery().Select.Columns.Count.ShouldBe(3);
		}

		public class CoordinateDepth
		{
			public int Z1 { get; set; }
			public int Z2 { get; set; }
		}

		public class Coordinate
		{
			public int               X { get; set; }
			public int               Y { get; set; }
			public CoordinateDepth?  Z { get; set; }
		}

		[Column("x",  "Coord.X")]
		[Column("y",  "Coord.Y")]
		[Column("z1", "Coord.Z.Z1")]
		[Column("z2", "Coord.Z.Z2")]
		public class NestedCoordEntity
		{
			[PrimaryKey] public long Id { get; set; }

			public Coordinate? Coord { get; set; }

			public static readonly NestedCoordEntity[] TestData = new []
			{
				new NestedCoordEntity()
				{
					Id    = 1,
					Coord = new Coordinate()
					{
						X = 11,
						Y = 22,
						Z = new CoordinateDepth() { Z1 = 33, Z2 = 44 }
					}
				}
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4568")]
		public void SelectNestedCompositeProperty_OnlyRequiredColumns([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var root = db.GetTable<NestedCoordEntity>().Select(t => t.Coord);
			root.GetSelectQuery().Select.Columns.Count.ShouldBe(4);

			var nested = db.GetTable<NestedCoordEntity>().Select(t => t.Coord!.Z);
			nested.GetSelectQuery().Select.Columns.Count.ShouldBe(2);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4568")]
		public void SelectNestedCompositeProperty([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NestedCoordEntity.TestData);

			var coord = table.Select(t => t.Coord).Single();

			coord.ShouldNotBeNull();
			coord.X.ShouldBe(11);
			coord.Y.ShouldBe(22);
			coord.Z.ShouldNotBeNull();
			coord.Z.Z1.ShouldBe(33);
			coord.Z.Z2.ShouldBe(44);

			var depth = table.Select(t => t.Coord!.Z).Single();

			depth.ShouldNotBeNull();
			depth.Z1.ShouldBe(33);
			depth.Z2.ShouldBe(44);
		}

		// The message opens with a TableContext id that changes every run, so the fragment starts after it. One
		// cause across all 112 cases, direct and remote.
		[ActiveIssue(4139, ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "ComplexColumnTable).Parent.Parent' is not an association.",
			Details = "Issue number taken from the test's own Description, which the bare attribute did not carry. A nested association reached through a complex column is not recognised as one.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4139")]
		public void LoadWithNestedAssociationThroughComplexColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(ComplexColumnTable.Data);

			var records = tb.LoadWith(t => t.Parent!.Parent).OrderBy(r => r.Id).ToArray();

			Assert.That(records.Count(), Is.EqualTo(2));
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
		sealed class ComplexColumnTable
		{
			[PrimaryKey] public int Id { get; set; }

			[Column("ParentId", ".ParentId")]
			// TODO: missing ctor
			//[Association(".Parent", ThisKey = ".ParentId", OtherKey = "Id")]
			public ComplexColumnParent? Parent { get; set; }

			public static readonly ComplexColumnTable[] Data = new[]
			{
				new ComplexColumnTable() { Id = 1 },
				new ComplexColumnTable() { Id = 2, Parent = new() { ParentId = 1 } }
			};
		}

		sealed class ComplexColumnParent
		{
			public int? ParentId { get; set; }
			public ComplexColumnTable? Parent { get; set; }
		}

		#region Record Constructors
		static class RecordTests
		{
			public sealed class Table
			{
				[PrimaryKey]
				public int Id { get; set; }

				public Struct Struct { get; set; }

				public Class Class { get; set; } = null!;
			}

			[StructLayout(LayoutKind.Auto)]
			public struct Struct
			{
				public int Value1 { get; set; }
				public int Value2 { get; set; }
			}

			public sealed class Class
			{
				public int Value1 { get; set; }
				public int Value2 { get; set; }
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5056")]
		public void ChildRecordTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool asScalars, [Values] bool useInit)
		{
			if (!asScalars && !useInit)
				Assert.Inconclusive("Unsupported case");

			var ms = new MappingSchema();

			if (asScalars)
			{
				ms.AddScalarType(typeof(RecordTests.Struct), DataType.Int32);
				ms.AddScalarType(typeof(RecordTests.Class), DataType.Int32);

				ms.SetConverter<RecordTests.Struct, DataParameter>(p => new DataParameter(null, p.Value1 + p.Value2));
				ms.SetConverter<RecordTests.Class, DataParameter>(p => new DataParameter(null, p.Value1 - p.Value2));

				// use long as sqlite returns integers as Int64 values
				ms.SetConverter<long, RecordTests.Struct>(v => new RecordTests.Struct() { Value1 = (int)v + 4, Value2 = (int)v - 5 });
				ms.SetConverter<long, RecordTests.Class>(v => new RecordTests.Class() { Value1 = (int)v - 3, Value2 = (int)v + 2 });

				// remote context serialization
				if (context.IsRemote())
				{
					ms.SetConverter<string, RecordTests.Struct>(v => new RecordTests.Struct() { Value1 = int.Parse(v.Split(':')[0]), Value2 = int.Parse(v.Split(':')[1]) });
					ms.SetConverter<string, RecordTests.Class>(v => new RecordTests.Class() { Value1 = int.Parse(v.Split(':')[0]), Value2 = int.Parse(v.Split(':')[1]) });
					ms.SetConverter<RecordTests.Struct, string>(v => $"{v.Value1}:{v.Value2}");
					ms.SetConverter<RecordTests.Class, string>(v => $"{v.Value1}:{v.Value2}");
				}
			}
			else
			{
				new FluentMappingBuilder(ms)
					.Entity<RecordTests.Table>()
						.Property(e => e.Struct.Value1).HasColumnName("struct_value1")
						.Property(e => e.Struct.Value2).HasColumnName("struct_value2")
						.Property(e => e.Class.Value1).HasColumnName("class_value1")
						.Property(e => e.Class.Value2).HasColumnName("class_value2")
					.Build()
					;
			}

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<RecordTests.Table>();

			if (useInit)
			{
				tb.Insert(() => new RecordTests.Table()
				{
					Id = 1,
					Struct = new RecordTests.Struct()
					{
						Value1 = 5,
						Value2 = 8
					},
					Class = new RecordTests.Class()
					{
						Value1 = -4,
						Value2 = -12
					}
				});
			}
			else
			{
				var structValue = new RecordTests.Struct()
				{
					Value1 = 5,
					Value2 = 8
				};
				var classValue = new RecordTests.Class()
				{
					Value1 = -4,
					Value2 = -12
				};

				tb.Insert(() => new RecordTests.Table()
				{
					Id = 1,
					Struct = structValue,
					Class = classValue
				});
			}

			var record = tb.Single();
			Assert.That(record.Class, Is.Not.Null);

			if (asScalars)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(record.Struct.Value1, Is.EqualTo(17));
					Assert.That(record.Struct.Value2, Is.EqualTo(8));
					Assert.That(record.Class.Value1, Is.EqualTo(5));
					Assert.That(record.Class.Value2, Is.EqualTo(10));
				}
			}
			else
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(record.Struct.Value1, Is.EqualTo(5));
					Assert.That(record.Struct.Value2, Is.EqualTo(8));
					Assert.That(record.Class.Value1, Is.EqualTo(-4));
					Assert.That(record.Class.Value2, Is.EqualTo(-12));
				}
			}

			if (useInit)
			{
				tb.Update(r => new RecordTests.Table()
				{
					Id = 1,
					Struct = new RecordTests.Struct()
					{
						Value1 = 12,
						Value2 = -11
					},
					Class = new RecordTests.Class()
					{
						Value1 = -3,
						Value2 = 5
					}
				});
			}
			else
			{
				var structValue = new RecordTests.Struct()
				{
					Value1 = 12,
					Value2 = -11
				};
				var classValue = new RecordTests.Class()
				{
					Value1 = -3,
					Value2 = 5
				};

				tb.Update(r => new RecordTests.Table()
				{
					Id = 1,
					Struct = structValue,
					Class = classValue
				});
			}

			record = tb.Single();

			if (asScalars)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(record.Struct.Value1, Is.EqualTo(5));
					Assert.That(record.Struct.Value2, Is.EqualTo(-4));
					Assert.That(record.Class.Value1, Is.EqualTo(-11));
					Assert.That(record.Class.Value2, Is.EqualTo(-6));
				}
			}
			else
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(record.Struct.Value1, Is.EqualTo(12));
					Assert.That(record.Struct.Value2, Is.EqualTo(-11));
					Assert.That(record.Class.Value1, Is.EqualTo(-3));
					Assert.That(record.Class.Value2, Is.EqualTo(5));
				}
			}
		}
		#endregion

		static class RequiredMembersScalarModel
		{
			public sealed class Table
			{
				[PrimaryKey]
				public required int Id { get; init; }

				[ValueConverter(ConverterType = typeof(ComplexTypeConverter))]
				[Column(DataType = DataType.NVarChar)]
				public required ComplexType ComplexType { get; init; }
			}

			[ScalarType]
			public sealed class ComplexType
			{
				public required int Id { get; init; }
			}

			sealed class ComplexTypeConverter() : ValueConverter<ComplexType, string>(
				obj => JsonSerializer.Serialize(obj, JsonSerializerOptions.Default),
				json => JsonSerializer.Deserialize<ComplexType>(json, JsonSerializerOptions.Default)!,
				false);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5056")]
		public void InsertScalarTypeWithRequiredMembers([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<RequiredMembersScalarModel.Table>();

			tb.Insert(() => new RequiredMembersScalarModel.Table()
			{
				Id = 1,
				ComplexType = new RequiredMembersScalarModel.ComplexType()
				{
					Id = 2,
				}
			});

			var inserted = tb.Single();

			Assert.That(inserted.ComplexType.Id, Is.EqualTo(2));
		}

		[ActiveIssue(Configuration = TestProvName.AllAccessLibRed, ErrorMessage = "Column 'vpcc.Id' was not found",
			Details = "no-issue: LibRed's hash join cannot resolve this query's join key. LibRed.Ado 11.0.0-alpha.4 and beta.1; re-check when a newer LibRed ships.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4336")]
		public void ComposedViewsWithGroupedOuterJoins([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(new[] {new CapacityProduct {Id = 1, Name = "p", CategoryId = 1}});
			using var t2 = db.CreateLocalTable(new[] {new CapacityProductCategory {Id = 1, Name = "c", PeriodOrderLimit = 5}});
			using var t3 = db.CreateLocalTable<CapacityOrderHeader>();
			using var t4 = db.CreateLocalTable<CapacityOrderItem>();
			using var t5 = db.CreateLocalTable(new[] {new CapacityOrderPeriod {Id = 1}});
			using var t6 = db.CreateLocalTable(new[] {new CapacityPeriodProduct {Id = 1, OrderPeriodId = 1, ProductId = 1}});

			var q = (from r in ViewCapacityAndOrderedByPeriod(db) select r).Take(10);
			var l = q.ToList();

			l.ShouldBe(new[] {new ViewCapacityAndOrderedByPeriodRow(1, 1, 1, 5, 0, 5, 5, 0, 5)});
		}

		[Table]
		public class CapacityProduct
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(CanBeNull = false)] public string Name { get; set; } = null!;
			[Column(CanBeNull = false)] public int? CategoryId { get; set; }
		}

		[Table]
		public class CapacityProductCategory
		{
			[PrimaryKey] public int Id { get; set; } // integer
			[Column(CanBeNull = false)] public string Name { get; set; } = null!;
			[Column] public short ProductOrderLimit { get; set; }
			[Column] public short CustomerOrderLimit { get; set; }
			[Column] public short PeriodOrderLimit { get; set; }
		}

		[Table]
		public class CapacityOrderHeader
		{
			[PrimaryKey] public int Id { get; set; } // integer
			[Column] public int PeriodId { get; set; } // integer

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CapacityOrderItem.OrderHeaderId))]
			public IEnumerable<CapacityOrderItem> OrderItems { get; set; } = null!;
		}

		[Table]
		public partial class CapacityOrderItem
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int OrderHeaderId { get; set; }
			[Column] public int ProductId { get; set; }
			[Column] public short Quantity { get; set; }

		}

		[Table]
		public class CapacityOrderPeriod
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CapacityOrderHeader.PeriodId))]
			public IEnumerable<CapacityOrderHeader> OrderHeaders { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CapacityPeriodProduct.OrderPeriodId))]
			public IEnumerable<CapacityPeriodProduct> ProductsPerOrderPeriods { get; set; } = null!;
		}

		[Table]
		public class CapacityPeriodProduct
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int OrderPeriodId { get; set; }
			[Column] public int ProductId { get; set; }
		}

		public record ViewProductCategoryCapacityRow(int Id, string Name, short ProductOrderLimit, short CustomerOrderLimit, short PeriodOrderLimit);
		public record ViewProductCapacityRow(int ProductId, int? CategoryId, string Name, short ProductOrderLimit, short CustomerOrderLimit, short PeriodOrderLimit);
		public record ViewSumOfOrderedProductsByPeriodRow(int OrderPeriodId, int ProductId, int Quantity);
		public record ViewCapacityAndOrderedByPeriodAndProductRow(int OrderPeriodId, int ProductId, int? CategoryId, int MaxCapacity, int Quantity, int FreeCapacity);
		public record ViewSumOfOrderedCategoriesByPeriodRow(int OrderPeriodId, int? CategoryId, int Quantity);
		public record ViewCapacityAndOrderedByPeriodAndProductCategoryRow(int OrderPeriodId, int? ProductCategoryId, string ProductCategoryName, int MaxCapacity, int Quantity, int FreeCapacity);
		public record ViewCapacityAndOrderedByPeriodRow(int OrderPeriodId, int ProductId, int? ProductCategoryId, int ProductMaxCapacity, int ProductQuantity, int ProductFreeCapacity, int CategoryMaxCapacity, int CategoryQuantity, int CategoryFreeCapacity);

		IQueryable<ViewProductCategoryCapacityRow> ViewProductCategoryCapacity(IDataContext db) =>
		   from pc in db.GetTable<CapacityProductCategory>()
		   select new ViewProductCategoryCapacityRow(pc.Id, pc.Name, pc.ProductOrderLimit, pc.CustomerOrderLimit, pc.PeriodOrderLimit);

		IQueryable<ViewProductCapacityRow> ViewProductCapacity(IDataContext db) =>
		   from p in db.GetTable<CapacityProduct>()
		   from pcc in ViewProductCategoryCapacity(db).Where(o => o.Id == p.CategoryId).DefaultIfEmpty()
		   select new ViewProductCapacityRow(p.Id, p.CategoryId, p.Name, pcc.ProductOrderLimit, pcc.CustomerOrderLimit, pcc.PeriodOrderLimit);

		IQueryable<ViewSumOfOrderedProductsByPeriodRow> ViewSumOfOrderedProductsByPeriod(IDataContext db) =>
					from op in db.GetTable<CapacityOrderPeriod>()
					from oh in op.OrderHeaders.DefaultIfEmpty()
					from oi in oh.OrderItems.DefaultIfEmpty()
					group new { oi.Quantity } by new { op.Id, oi.ProductId } into agroup
					select new ViewSumOfOrderedProductsByPeriodRow(agroup.Key.Id, agroup.Key.ProductId, Coalesce(agroup.Sum(a => Coalesce(a.Quantity, (short)0)), 0));

		IQueryable<ViewCapacityAndOrderedByPeriodAndProductRow> ViewCapacityAndOrderedByPeriodAndProduct(IDataContext db) =>
						  from op in db.GetTable<CapacityOrderPeriod>()
						  from pop in op.ProductsPerOrderPeriods
						  from vpc in ViewProductCapacity(db).Where(o => o.ProductId == pop.ProductId).DefaultIfEmpty()
						  from vsp in ViewSumOfOrderedProductsByPeriod(db).Where(o => o.OrderPeriodId == op.Id && o.ProductId == pop.ProductId).DefaultIfEmpty()
						  let PeriodOrderLimit = Coalesce(vpc.PeriodOrderLimit, 0)
						  let Quantity = Coalesce(vsp.Quantity, 0)
						  select new ViewCapacityAndOrderedByPeriodAndProductRow(op.Id, pop.ProductId, vpc.CategoryId, PeriodOrderLimit, Quantity, PeriodOrderLimit - Quantity);

		IQueryable<ViewSumOfOrderedCategoriesByPeriodRow> ViewSumOfOrderedCategoriesByPeriod(IDataContext db) =>
		   from op in db.GetTable<CapacityOrderPeriod>()
		   from oh in op.OrderHeaders.DefaultIfEmpty()
		   from oi in oh.OrderItems.DefaultIfEmpty()
		   from p in db.GetTable<CapacityProduct>().Where(o => o.Id == oi.ProductId).DefaultIfEmpty()
		   group new { oi.Quantity } by new { op.Id, p.CategoryId } into agroup
		   select new ViewSumOfOrderedCategoriesByPeriodRow(agroup.Key.Id, agroup.Key.CategoryId, agroup.Sum(a => a.Quantity));

		 IQueryable<ViewCapacityAndOrderedByPeriodAndProductCategoryRow> ViewCapacityAndOrderedByPeriodAndProductCategory(IDataContext db) =>
		   from op in db.GetTable<CapacityOrderPeriod>()
		   from vpcc in ViewProductCategoryCapacity(db)
		   from vsopc in ViewSumOfOrderedCategoriesByPeriod(db).Where(o => o.OrderPeriodId == op.Id && o.CategoryId == vpcc.Id).DefaultIfEmpty()
		   let PeriodOrderLimit = Coalesce(vpcc.PeriodOrderLimit, 0)
		   let Quantity = vsopc.Quantity
		   select new ViewCapacityAndOrderedByPeriodAndProductCategoryRow(op.Id, vpcc.Id, vpcc.Name, PeriodOrderLimit, Quantity, PeriodOrderLimit - Quantity);

		IQueryable<ViewCapacityAndOrderedByPeriodRow> ViewCapacityAndOrderedByPeriod(IDataContext db) =>
		   from v1 in ViewCapacityAndOrderedByPeriodAndProduct(db)
		   from v2 in ViewCapacityAndOrderedByPeriodAndProductCategory(db).Where(o => o.OrderPeriodId == v1.OrderPeriodId && o.ProductCategoryId == v1.CategoryId).DefaultIfEmpty()
		   select new ViewCapacityAndOrderedByPeriodRow(v1.OrderPeriodId, v1.ProductId, v1.CategoryId, v1.MaxCapacity, v1.Quantity, v1.FreeCapacity, v2.MaxCapacity, v2.Quantity, v2.FreeCapacity);

		[Sql.Extension("COALESCE({expr},{nullValue})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		static T Coalesce<T>([ExprParameter] T expr, [ExprParameter] T nullValue)
		{
			throw new ServerSideOnlyException(nameof(Coalesce));
		}
	}
}
