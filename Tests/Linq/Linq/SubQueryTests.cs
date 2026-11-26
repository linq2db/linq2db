using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class SubQueryTests : TestBase
	{
		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID select ch.ChildID).Max(),
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID select ch.ChildID).Max());
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max(),
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max());
		}

		[Test]
		public void Test3([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).SingleOrDefault()
				,
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).SingleOrDefault());
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Test4([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.ParentID != 5
				select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).FirstOrDefault(),
				from p in db.Parent
				where p.ParentID != 5
				select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).FirstOrDefault());
		}

		static int _testValue = 3;

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Test5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			IEnumerable<int> ids = new[] { 1, 2 };

			var eids = Parent
					.Where(p => ids.Contains(p.ParentID))
					.Select(p => p.Value1 == null ? p.ParentID : p.ParentID + 1)
					.Distinct();

			var expected = eids.Select(id =>
					new
					{
						id,
						Count1 = Child.Where(p => p.ParentID == id).Count(),
						Count2 = Child.Where(p => p.ParentID == id && p.ParentID == _testValue).Count(),
					});

			var rids = db.Parent
					.Where(p => ids.Contains(p.ParentID))
					.Select(p => p.Value1 == null ? p.ParentID : p.ParentID + 1)
					.Distinct();

			var result = rids.Select(id =>
					new
					{
						id,
						Count1 = db.Child.Where(p => p.ParentID == id).Count(),
						Count2 = db.Child.Where(p => p.ParentID == id && p.ParentID == _testValue).Count(),
					});

			AreEqual(expected, result);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Test6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var id = 2;
			var b  = false;

			var q = Child.Where(c => c.ParentID == id).OrderBy(c => c.ChildID);
			q = b
				? q.OrderBy(m => m.ParentID)
				: q.OrderByDescending(m => m.ParentID);

			var gc = GrandChild;
			var expected = q.Select(c => new
			{
				ID     = c.ChildID,
				c.ParentID,
				Sum    = gc.Where(g => g.ChildID == c.ChildID && g.GrandChildID > 0).Sum(g => (int)g.ChildID! * g.GrandChildID),
				Count1 = gc.Count(g => g.ChildID == c.ChildID && g.GrandChildID > 0)
			});

			var r = db.Child.Where(c => c.ParentID == id).OrderBy(c => c.ChildID);
			r = b
				? r.OrderBy(m => m.ParentID)
				: r.OrderByDescending(m => m.ParentID);

			var rgc = db.GrandChild;
			var result = r.Select(c => new
			{
				ID     = c.ChildID,
				c.ParentID,
				Sum    = rgc.Where(g => g.ChildID == c.ChildID && g.GrandChildID > 0).Sum(g => (int)g.ChildID! * g.GrandChildID),
				Count1 = rgc.Count(g => g.ChildID == c.ChildID && g.GrandChildID > 0),
			});

			AreEqual(expected, result);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Test7([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from c in Child select new { Count = GrandChild.Where(g => g.ChildID == c.ChildID).Count() },
				from c in db.Child select new { Count = db.GrandChild.Where(g => g.ChildID == c.ChildID).Count() });
		}

		[Test]
		public void Test8([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var parent  =
					from p in db.Parent
					where p.ParentID == 1
					select p.ParentID;

			var chilren =
					from c in db.Child
					where parent.Contains(c.ParentID)
					select c;

			var chs1 = chilren.ToList();

			parent =
				from p in db.Parent
				where p.ParentID == 2
				select p.ParentID;

			chilren =
				from c in db.Child
				where parent.Contains(c.ParentID)
				select c;

			var chs2 = chilren.ToList();

			Assert.That(chs2.Except(chs1).Count(), Is.EqualTo(chs2.Count));
		}

		[Test]
		public void DerivedTake([DataSources]
			string context)
		{
			using var db = GetDataContext(context);
			AssertQuery(db.Parent.Take(1).AsSubQuery());
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllNativeAccess, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_Skip_in_Subquery)]
		public void DerivedSkipTake([DataSources]
			string context)
		{
			using var db = GetDataContext(context);
			AssertQuery(db.Parent.Skip(1).Take(1).AsSubQuery());
		}

		[Test]
		public void ObjectCompare([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				from c in
					from c in
						from c in Child select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1, Parent = c.Parent }
					where c.ChildID > 0
					select c
				where p == c.Parent
				select new { p.ParentID, c.ChildID },
				from p in db.Parent
				from c in
					from c in
						from c in db.Child select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1, Parent = c.Parent }
					where c.ChildID > 0
					select c
				where p == c.Parent
				select new { p.ParentID, c.ChildID });
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void Contains1([DataSources(
			TestProvName.AllInformix,
			TestProvName.AllClickHouse,
			TestProvName.AllSybase,
			TestProvName.AllSapHana,
			TestProvName.AllNativeAccess,
			TestProvName.AllOracle,
			TestProvName.AllMySql,
			ProviderName.DB2)]
			string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where (from p1 in Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
				select p,
				from p in db.Parent
				where (from p1 in db.Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
				select p);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void Contains2([DataSources(
			TestProvName.AllClickHouse,
			TestProvName.AllMySql,
			TestProvName.AllSybase,
			TestProvName.AllSapHana,
			TestProvName.AllNativeAccess,
			TestProvName.AllOracle,
			ProviderName.DB2)]
			string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where (from p1 in Parent where p1.Value1 == p.Value1 select p1.ParentID).Take(3).Contains(p.ParentID)
				select p,
				from p in db.Parent
				where (from p1 in db.Parent where p1.Value1 == p.Value1 select p1.ParentID).Take(3).Contains(p.ParentID)
				select p);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void SubSub1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in
					from p2 in Parent
					select new { p2, ID = p2.ParentID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				},
				from p1 in
					from p2 in db.Parent
					select new { p2, ID = p2.ParentID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				});
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void SubSub2([DataSources(
			TestProvName.AllNativeAccess,
			ProviderName.DB2,
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllInformix)]
			string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in
					from p2 in Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c.c.ParentID + 1 into c
						where c < p1.ID
						select c
					).FirstOrDefault()
				},
				from p1 in
					from p2 in db.Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c.c.ParentID + 1 into c
						where c < p1.ID
						select c
					).FirstOrDefault()
				});
		}

		//[Test]
		//public void SubSub201([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//		AreEqual(
		//			from p1 in
		//				from p2 in Parent
		//				select new { p2, ID = p2.ParentID + 1 } into p3
		//				where p3.ID > 0
		//				select new { p2 = p3, ID = p3.ID + 1 }
		//			where p1.ID > 0
		//			select new
		//			{
		//				Count =
		//				(
		//					from c in p1.p2.p2.Children
		//					select new { c, ID = c.ParentID + 1 } into c
		//					where c.ID < p1.ID
		//					select new { c.c, ID = c.c.ParentID + 1 } into c
		//					where c.ID < p1.ID
		//					select c
		//				).FirstOrDefault()
		//			},
		//			from p1 in
		//				from p2 in db.Parent
		//				select new { p2, ID = p2.ParentID + 1 } into p3
		//				where p3.ID > 0
		//				select new { p2 = p3, ID = p3.ID + 1 }
		//			where p1.ID > 0
		//			select new
		//			{
		//				Count =
		//				(
		//					from c in p1.p2.p2.Children
		//					select new { c, ID = c.ParentID + 1 } into c
		//					where c.ID < p1.ID
		//					select new { c.c, ID = c.c.ParentID + 1 } into c
		//					where c.ID < p1.ID
		//					select c
		//				).FirstOrDefault()
		//			});
		//}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void SubSub21([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in
					from p2 in Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				},
				from p1 in
					from p2 in db.Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				});
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void SubSub211([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in
					from p2 in Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						from g in c.GrandChildren
						select new { g, ID = g.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.g, ID = c.g.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				},
				from p1 in
					from p2 in db.Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Children
						from g in c.GrandChildren
						select new { g, ID = g.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.g, ID = c.g.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				});
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void SubSub212([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in
					from p2 in Child
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Parent!.GrandChildren
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				},
				from p1 in
					from p2 in db.Child
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in p1.p2.p2.Parent!.GrandChildren
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				});
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void SubSub22([DataSources(
			ProviderName.SqlCe, ProviderName.DB2,
			TestProvName.AllOracle, TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in
					from p2 in Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in Child
						where p1.p2.p2.ParentID == c.ParentID
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				},
				from p1 in
					from p2 in db.Parent
					select new { p2, ID = p2.ParentID + 1 } into p3
					where p3.ID > 0
					select new { p2 = p3, ID = p3.ID + 1 }
				where p1.ID > 0
				select new
				{
					Count =
					(
						from c in db.Child
						where p1.p2.p2.ParentID == c.ParentID
						select new { c, ID = c.ParentID + 1 } into c
						where c.ID < p1.ID
						select new { c.c, ID = c.c.ParentID + 1 } into c
						where c.ID < p1.ID
						select c
					).Count()
				});
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Count1([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in
					from p in Parent
					select new
					{
						p.ParentID,
						Sum = p.Children.Where(t => t.ParentID > 0).Sum(t => t.ParentID) / 2,
					}
				where p.Sum > 1
				select p,
				from p in
					from p in db.Parent
					select new
					{
						p.ParentID,
						Sum = p.Children.Where(t => t.ParentID > 0).Sum(t => t.ParentID) / 2,
					}
				where p.Sum > 1
				select p);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void Count2([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in
					from p in Parent
					select new Parent
					{
						ParentID = p.ParentID,
						Value1 = p.Children.Where(t => t.ParentID > 0).Sum(t => t.ParentID) / 2,
					}
				where p.Value1 > 1
				select p,
				from p in
					from p in db.Parent
					select new Parent
					{
						ParentID = p.ParentID,
						Value1 = p.Children.Where(t => t.ParentID > 0).Sum(t => t.ParentID) / 2,
					}
				where p.Value1 > 1
				select p);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Count3([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in
					from p in Parent
					select new
					{
						p.ParentID,
						Sum = p.Children.Sum(t => t.ParentID) / 2,
					}
				where p.Sum > 1
				select p,
				from p in
					from p in db.Parent
					select new
					{
						p.ParentID,
						Sum = p.Children.Sum(t => t.ParentID) / 2,
					}
				where p.Sum > 1
				select p);
		}

		[ActiveIssue(5591, Configuration = TestProvName.AllYdb, ErrorTypeName = "Ydb.Sdk.Ado.YdbException",
			ErrorMessage = "uncompatible coalesce types, first type: Optional<Decimal(35,2)>, second type: Decimal(22,9)",
			Details = "YDB strict-decimal rejects COALESCE of mismatched decimal facets.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1601")]
		public void LetSumReusedInProjection([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			var query = from q in db.Types
						let x = db.Types.Sum(y => y.MoneyValue)
						select new
						{
							Y1 = x < 0 ? 9 : x + 8,
							Y2 = Math.Round(x + x)
						};

			query.ToList();

			Assert.That(System.Text.RegularExpressions.Regex.Matches(db.LastQuery!, "Types"), Has.Count.EqualTo(2));
		}

		[Table]
		sealed class Contract_Distributor_Agent
		{
			[PrimaryKey] public int Agent_Id { get; set; }
			[PrimaryKey] public int Distributor_Id { get; set; }
			[PrimaryKey] public int Contract_Id { get; set; }
			[Column] public string? Distributor_Type_Code { get; set; }
			[Column] public string? Distributor_Agent_Type_Prefix { get; set; }
			[Column] public string? Represents_Type_Prefix { get; set; }

			public static readonly Contract_Distributor_Agent[] Data = new[]
			{
				new Contract_Distributor_Agent() { Agent_Id = 1, Distributor_Id = 1, Contract_Id = 198827882, Distributor_Type_Code = "CC", Distributor_Agent_Type_Prefix = "OFFICE", Represents_Type_Prefix = "REPRESENTS" }
			};
		}

		[Table]
		sealed class Agent
		{
			[PrimaryKey] public int Agent_Id { get; set; }
			[Column] public string? First_Name { get; set; }
			[Column] public string? Last_Name { get; set; }

			public static readonly Agent[] Data = new[]
			{
				new Agent() { Agent_Id = 1, First_Name = "x", Last_Name = "x" }
			};
		}

		[Table]
		sealed class Distributor
		{
			[PrimaryKey] public int Distributor_Id { get; set; }
			[Column] public string? Type_Code { get; set; }
			[Column] public string? Distributor_Name { get; set; }

			public static readonly Distributor[] Data = new[]
			{
				new Distributor() { Distributor_Id = 1, Type_Code = "RE", Distributor_Name = "x" }
			};
		}

		[Table]
		sealed class Distributor_Commercial_Propert
		{
			[PrimaryKey] public int Distributor_Id { get; set; }
			[PrimaryKey] public int Commercial_Property_Id { get; set; }
			[Column] public string? Distributor_Type_Code { get; set; }

			public static readonly Distributor_Commercial_Propert[] Data = new[]
			{
				new Distributor_Commercial_Propert() { Distributor_Id = 1, Commercial_Property_Id = 1, Distributor_Type_Code = "RE" }
			};
		}

		[Table]
		sealed class Commercial_Property
		{
			[PrimaryKey          ] public int     Commercial_Property_Id { get; set; }
			[Column(Length = 100)] public string? Street_Number          { get; set; }
			[Column(Length = 100)] public string? Street_Name            { get; set; }
			[Column(Length = 100)] public string? State                  { get; set; }
			[Column(Length = 100)] public string? Zip_Code               { get; set; }
			[Column(Length = 100)] public string? Zip_Plus_4             { get; set; }
			[Column(Length = 100)] public string? City_Code              { get; set; }

			public static readonly Commercial_Property[] Data = new[]
			{
				new Commercial_Property() { Commercial_Property_Id = 1, Street_Number = "x", Street_Name = "x", State = "x", Zip_Code = "x", Zip_Plus_4 = "x", City_Code = "x" }
			};
		}

		[Table]
		sealed class Contract_Dates
		{
			[PrimaryKey] public int Contract_Id { get; set; }
			[Column] public string? Type_Code { get; set; }
			[Column] public string? Effective_Date { get; set; }

			public static readonly Contract_Dates[] Data = new[]
			{
				new Contract_Dates() { Contract_Id = 198827882, Type_Code = "ESTCOE", Effective_Date = "x" }
			};
		}

		[Table]
		sealed class Cities
		{
			[PrimaryKey, Column(CanBeNull = false, Length = 50)] public string City_Code { get; set; } = null!;
			[Column] public string? City_Name { get; set; }

			public static readonly Cities[] Data = new[]
			{
				new Cities() { City_Code = "x", City_Name = "Urupinsk" }
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/383")]
		public void ManyJoinsCollectionSubqueryInProjection([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Contract_Distributor_Agent.Data))
			using (db.CreateLocalTable(Agent.Data))
			using (db.CreateLocalTable(Distributor.Data))
			using (db.CreateLocalTable(Distributor_Commercial_Propert.Data))
			using (db.CreateLocalTable(Commercial_Property.Data))
			using (db.CreateLocalTable(Contract_Dates.Data))
			using (db.CreateLocalTable(Cities.Data))
			{
				var query = from cda in db.GetTable<Contract_Distributor_Agent>()
							join a in db.GetTable<Agent>() on cda.Agent_Id equals a.Agent_Id
							join d in db.GetTable<Distributor>() on cda.Distributor_Id equals d.Distributor_Id
							join dcp in db.GetTable<Distributor_Commercial_Propert>() on d.Distributor_Id equals dcp.Distributor_Id
							join cp in db.GetTable<Commercial_Property>() on dcp.Commercial_Property_Id equals cp.Commercial_Property_Id
							join cd in db.GetTable<Contract_Dates>() on cda.Contract_Id equals cd.Contract_Id
							where cda.Contract_Id == 198827882
								 && cda.Distributor_Type_Code == "CC"
								 && cda.Distributor_Agent_Type_Prefix == "OFFICE"
								 && cda.Represents_Type_Prefix == "REPRESENTS"
								 && cd.Type_Code == "ESTCOE"
								 && d.Type_Code == "RE"
								 && dcp.Distributor_Type_Code == "RE"
							select new
							{
								a.First_Name,
								a.Last_Name,
								d.Distributor_Name,
								cp.Street_Number,
								cp.Street_Name,
								City_Name = (from c in db.GetTable<Cities>()
											 where c.City_Code == cp.City_Code
											 select new { c.City_Name }),
								cp.State,
								cp.Zip_Code,
								cp.Zip_Plus_4,
								cd.Effective_Date
							};

				var res = query.ToList();

				Assert.That(res, Has.Count.EqualTo(1));
				Assert.That(res[0].City_Name.Single().City_Name, Is.EqualTo("Urupinsk"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/383")]
		public void ManyJoinsSingleSubqueryInProjection([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Contract_Distributor_Agent.Data))
			using (db.CreateLocalTable(Agent.Data))
			using (db.CreateLocalTable(Distributor.Data))
			using (db.CreateLocalTable(Distributor_Commercial_Propert.Data))
			using (db.CreateLocalTable(Commercial_Property.Data))
			using (db.CreateLocalTable(Contract_Dates.Data))
			using (db.CreateLocalTable(Cities.Data))
			{
				var query = from cda in db.GetTable<Contract_Distributor_Agent>()
							join a in db.GetTable<Agent>() on cda.Agent_Id equals a.Agent_Id
							join d in db.GetTable<Distributor>() on cda.Distributor_Id equals d.Distributor_Id
							join dcp in db.GetTable<Distributor_Commercial_Propert>() on d.Distributor_Id equals dcp.Distributor_Id
							join cp in db.GetTable<Commercial_Property>() on dcp.Commercial_Property_Id equals cp.Commercial_Property_Id
							join cd in db.GetTable<Contract_Dates>() on cda.Contract_Id equals cd.Contract_Id
							where cda.Contract_Id == 198827882
								 && cda.Distributor_Type_Code == "CC"
								 && cda.Distributor_Agent_Type_Prefix == "OFFICE"
								 && cda.Represents_Type_Prefix == "REPRESENTS"
								 && cd.Type_Code == "ESTCOE"
								 && d.Type_Code == "RE"
								 && dcp.Distributor_Type_Code == "RE"
							select new
							{
								a.First_Name,
								a.Last_Name,
								d.Distributor_Name,
								cp.Street_Number,
								cp.Street_Name,
								City_Name = (from c in db.GetTable<Cities>()
											 where c.City_Code == cp.City_Code
											 select c.City_Name).Single(),
								cp.State,
								cp.Zip_Code,
								cp.Zip_Plus_4,
								cd.Effective_Date
							};

				var res = query.ToList();

				Assert.That(res, Has.Count.EqualTo(1));
				Assert.That(res[0].City_Name, Is.EqualTo("Urupinsk"));
			}
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void DropOrderByFromNonLimitedSubquery([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.Where(p => db.Child.Where(c => c.ParentID == p.ParentID)
					.Any(c => db.GrandChild.Select(gc => gc.ChildID).OrderBy(id => id).Contains(c.ChildID)));

			AssertQuery(query);
		}

		[Table]
		sealed class StockItem
		{
			[Column(CanBeNull = false, Length = 100), PrimaryKey] public string Id { get; set; } = null!;

			public static readonly StockItem[] Data =
			[
				new StockItem() { Id = "1", },
				new StockItem() { Id = "2", },
				new StockItem() { Id = "3", }
			];
		}

		[Table]
		sealed class WarehouseStock
		{
			[Column(CanBeNull = false, Length = 100), PrimaryKey] public string ItemId { get; set; } = null!;
			[Column] public int QuantityAvailable { get; set; }
			[Column(CanBeNull = false)] public string WarehouseId { get; set; } = null!;

			public static readonly WarehouseStock[] Data =
			[
				new WarehouseStock()
				{
					ItemId = "1",
					QuantityAvailable = 10,
					WarehouseId = "A",
				}
			];
		}

		[Table]
		sealed class Review
		{
			[Column(CanBeNull = false, Length = 100), PrimaryKey] public string ItemId { get; set; } = null!;
			[Column(CanBeNull = false, Length = 100), PrimaryKey] public string UserId { get; set; } = null!;
			[Column] public int Score { get; set; }

			public static readonly Review[] Data =
			[
				new Review()
				{
					ItemId = "1",
					UserId = "1",
					Score = 100,
				},
				new Review()
				{
					ItemId = "1",
					UserId = "2",
					Score = 90,
				},
				new Review()
				{
					ItemId = "2",
					UserId = "1",
					Score = 89,
				},
				new Review()
				{
					ItemId = "2",
					UserId = "4",
					Score = 93,
				},
				new Review()
				{
					ItemId = "3",
					UserId = "5",
					Score = 91,
				}
			];
		}

		sealed class ItemStockSummary
		{
			public string ItemId { get; set; } = null!;
			public int TotalAvailable { get; set; }
			public IEnumerable<Review> Reviews { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4458")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void FilterByNestedCollectionAfterGroupedLeftJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(StockItem.Data);
			using var t2 = db.CreateLocalTable(WarehouseStock.Data);
			using var t3 = db.CreateLocalTable(Review.Data);

			var query = from item in t1
						from stock in t2
						.LeftJoin(s => s.ItemId == item.Id)
						.GroupBy(s => s.ItemId)
						select new ItemStockSummary()
						{
							ItemId = item.Id,
							TotalAvailable = stock.Sum(s => s.QuantityAvailable),
							Reviews = t3.Where(r => r.ItemId == item.Id).OrderBy(r => r.ItemId).ThenBy(r => r.UserId)
						};

			var filteredByScore = query.Where(i => i.Reviews.Any(r => r.Score > 95));

			AssertQuery(filteredByScore);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4458")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void FilterBySubqueryAfterGroupedLeftJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(StockItem.Data);
			using var t2 = db.CreateLocalTable(WarehouseStock.Data);
			using var t3 = db.CreateLocalTable(Review.Data);

			var query = from item in t1
						from stock in t2
						.LeftJoin(s => s.ItemId == item.Id)
						.GroupBy(s => s.ItemId)
						select new ItemStockSummary()
						{
							ItemId = item.Id,
							TotalAvailable = stock.Sum(s => s.QuantityAvailable),
							Reviews = t3.Where(r => r.ItemId == item.Id)
						};

			var filteredByScore = query.Where(i => t3.Where(r => r.ItemId == i.ItemId).Any(r => r.Score > 95));

			var result = filteredByScore.ToArray();
			Assert.That(result, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].ItemId, Is.EqualTo("1"));
				Assert.That(result[0].TotalAvailable, Is.EqualTo(10));
				Assert.That(result[0].Reviews.Count(), Is.EqualTo(2));
			}
		}

		[Table]
		sealed record TransactionEntity
		{
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column]
			public DateTime ValidOn { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(LineEntity.TransactionId))]
			public List<LineEntity> Lines { get; set; } = null!;
		}

		[Table]
		sealed record LineEntity
		{
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column]
			public Guid TransactionId { get; set; }

			[Column]
			public decimal Amount { get; set; }

			[Column]
			public string Currency { get; set; } = null!;

			[Association(ThisKey = nameof(TransactionId), OtherKey = nameof(TransactionEntity.Id), CanBeNull = false)]
			public TransactionEntity Transaction { get; set; } = null!;
		}

		sealed record TransactionDto
		{
			public Guid Id { get; set; }

			public DateTime ValidOn { get; set; }

			public IEnumerable<LineDto> Lines { get; set; } = Enumerable.Empty<LineDto>();

			[ExpressionMethod(nameof(FromEntityExpression))]
			public static TransactionDto FromEntity(TransactionEntity entity)
				=> FromEntityExpression().Compile()(entity);

			static Expression<Func<TransactionEntity, TransactionDto>> FromEntityExpression() =>
				entity => new TransactionDto
				{
					Id = entity.Id,
					ValidOn = entity.ValidOn,
					Lines = entity.Lines.Select(line => LineDto.FromEntity(line))
				};
		}

		sealed record LineDto
		{
			public Guid Id { get; set; }

			public decimal Amount { get; set; }

			public string Currency { get; set; } = null!;

			[ExpressionMethod(nameof(FromEntityExpression))]
			public static LineDto FromEntity(LineEntity entity)
				=> FromEntityExpression().Compile()(entity);

			static Expression<Func<LineEntity, LineDto>> FromEntityExpression()
				=> entity => new LineDto
				{
					Id = entity.Id,
					Amount = entity.Amount,
					Currency = entity.Currency
				};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4347")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void FilterAndOrderByIntersectedDtoCollection([DataSources(TestProvName.AllNativeAccess, TestProvName.AllClickHouse, TestProvName.AllDB2, TestProvName.AllMariaDB)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<TransactionEntity>();
			using var t2 = db.CreateLocalTable<LineEntity>();

			var currencies = new[] { "A", "B" };

			var q = t1
				.Select(x => new
				{
					Entity = x,
					Dto = TransactionDto.FromEntity(x)
				})
				.Where(x => x.Dto.Lines.Select(y => y.Currency).Intersect(currencies).Any())
				.OrderBy(x => x.Dto.ValidOn)
				.Select(x => x.Dto)
				.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4347")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void FilterByIntersectedDtoCollection([DataSources(TestProvName.AllNativeAccess, TestProvName.AllClickHouse, TestProvName.AllDB2, TestProvName.AllMariaDB)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<TransactionEntity>();
			using var t2 = db.CreateLocalTable<LineEntity>();

			var currencies = new[] { "A", "B" };

			var q = t1
				.Select(x => new
				{
					Entity = x,
					Dto = TransactionDto.FromEntity(x)
				})
				.Where(x => x.Dto.Lines.Select(y => y.Currency).Intersect(currencies).Any())
				.Select(x => x.Dto)
				.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4347")]
		public void OrderByExpressionMethodDtoProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<TransactionEntity>();
			using var t2 = db.CreateLocalTable<LineEntity>();

			var q = t1
				.Select(x => new
				{
					Entity = x,
					Dto = TransactionDto.FromEntity(x)
				})
				.OrderBy(x => x.Dto.ValidOn)
				.Select(x => x.Dto)
				.ToList();
		}

		[ActiveIssue(Configurations = [TestProvName.AllOracle], ErrorMessage = "ORA-12704",
			Details = "no-issue: Oracle rejects the CASE expression with a character set mismatch - https://forums.oracle.com/ords/apexds/post/error-ora-12704-character-set-mismatch-in-case-statement-6917. Not 3295, which this test's Description cites: that one is a closed PostgreSQL issue about a let-var FirstOrDefault filter. No exception type is declared because it differs per Oracle client, while ORA-12704 does not.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3295")]
		public void LetFirstOrDefaultProjectionFilter([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query = (from x in db.Person
						 let status = db.Patient.FirstOrDefault(y => y.PersonID == x.ID)
						 select new
						 {
							 Id = status != null ? status.PersonID : x.ID,
							 StatusName = status != null ? status.Diagnosis : "abc",
						 }).Where(x => x.StatusName == "abc");

			query.ToArray();
		}

		// Sybase splits off: everywhere else the query runs and answers wrongly, there it is refused outright.
		[ActiveIssue(3295, ErrorMessage = "Assert.That(actual, Is.EqualTo(expected))",
			Details = "Issue number taken from the test's own Description, which the bare attribute did not carry. #3295 is closed and its own repro is the LetFirstOrDefaultProjectionFilter sibling, which passes; this variant still answers wrongly on every provider that can run it.")]
		[ActiveIssue(3295, Configuration = TestProvName.AllSybase, ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "The LINQ expression could not be converted to SQL.",
			Details = "Sybase refuses the shape rather than answering wrongly.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3295")]
		public void FirstOrDefaultInAssociationFilterCount([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var expected = Parent
				.Where(x => x.Children.Where(y => y.ChildID == 11).Select(y => y.ParentID).FirstOrDefault() == 0)
				.Count();

			var actual = db.Parent
				.Where(x => x.Children.Where(y => y.ChildID == 11).Select(y => y.ParentID).FirstOrDefault() == 0)
				.Count();

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3334")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void GroupJoinOnFirstOrDefaultSubquery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var subquery = db.GetTable<Person>();

			var query = db.GetTable<Person>()
					.Select(entity1 => new
					{
						Entity1 = entity1,
						Entity2 = subquery.FirstOrDefault(entity2 => entity2.ID == entity1.ID)
					})
					.GroupJoin(db.GetTable<Person>(),
						x => x.Entity2!.ID,
						x => x.ID,
						(x, y) => x);

			var result = query.FirstOrDefault();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3365")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void OrderByFirstOrDefaultAssociationProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child.Select(x => new
			{
				Assignee = x.GrandChildren.Select(a => a.ParentID).FirstOrDefault()
			});

			var orderedQuery = query.OrderBy(x => x.Assignee);

			orderedQuery.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4184")]
		public void JoinGroupedRecordSubqueryThrows([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var subquery =
				from p in db.Person
				group p by p.ID
				into gpItem
				select new PcScanId(gpItem.Key, gpItem.Max(s => s.ID));

			var query =
				from ps in subquery
				join pc in db.Patient on ps.PcId equals pc.PersonID
				select new { pc, ps };

			Assert.That(() => query.ToArray(), Throws.Exception.InstanceOf<LinqToDBException>());
		}

		private record PcScanId(int pcId, int scanId)
		{
			public int PcId = pcId;
			public int ScanId = scanId;
		}

		// technically it is not correct as such rownum generation is not guaranteed to follow query ordering
		// that's why many dbs disabled and only sqlserver and oracle work
		[Test]
		public void PreserveOrderInSubqueryWithWindowFunction_WithOrder([DataSources(
			TestProvName.AllSqlServer2008Minus, TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase,
			TestProvName.AllClickHouse, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySql,
			TestProvName.AllPostgreSQL, TestProvName.AllSapHana, TestProvName.AllSQLite, TestProvName.AllDB2,
			TestProvName.AllDuckDB, TestProvName.AllYdb
			)] string context)
		{
			using var db = GetDataContext(context);

			var result = db.Person
				.OrderBy(p => p.FirstName)
				.Select(r =>
				new
				{
					r.ID,
					RowNumber = Sql.Ext.RowNumber().Over().OrderBy(db.Select(() => "unordered")).ToValue()
				})
				.Take(100)
				.Join(db.Person, r => r.ID, r => r.ID, (r, n) => new { r.RowNumber, n.ID })
				.Where(r => r.ID == 2)
				.Single();

			Assert.That(result.RowNumber, Is.EqualTo(4));
		}

		[Test]
		public void PreserveOrderInSubqueryWithWindowFunction_NoOrdering([DataSources(TestProvName.AllNativeAccess, ProviderName.SqlCe, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var result = db.Person
				.Select(r =>
				new
				{
					r.ID,
					RowNumber = Sql.Ext.RowNumber().Over().OrderBy(r.FirstName).ToValue()
				})
				.Join(db.Person, r => r.ID, r => r.ID, (r, n) => new { r.RowNumber, n.ID })
				.Where(r => r.ID == 2)
				.Single();

			Assert.That(result.RowNumber, Is.EqualTo(4));
		}

		public class Trp004
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(Length = 10)] public string? CarNo { get; set; }
			[Column(Length = 10)] public string? RuleNo { get; set; }
			[Column(Length = 10)] public string? LastWorkVal { get; set; }
			[Column(Length = 10)] public string? LastDate { get; set; }
			[Column(Length = 10)] public string? RealLastWorkVal { get; set; }
			[Column(Length = 10)] public string? RealLastDate { get; set; }
			[Column(Length = 10)] public string? Status { get; set; }
			[Column(Length = 10)] public string? TelNo { get; set; }
			[Column(Length = 10)] public string? RecCreator { get; set; }
			[Column(Length = 10)] public string? RecCreateTime { get; set; }
			[Column(Length = 10)] public string? RecRevisor { get; set; }
			[Column(Length = 10)] public string? RecReviseTime { get; set; }
		}

		public class Tdm100
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(Length = 10)] public string? CarSelf { get; set; }
			[Column(Length = 10)] public string? CarNo { get; set; }
			[Column(Length = 10)] public string? CarBrand { get; set; }
			[Column(Length = 10)] public string? RateWgt { get; set; }
			[Column(Length = 10)] public string? MastLeve { get; set; }
			[Column(Length = 10)] public string? ForkPole { get; set; }
			[Column(Length = 10)] public string? ForkPoleLen { get; set; }
		}

		public class Trp003
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(Length = 10)] public string? RuleNo { get; set; }
			[Column(Length = 10)] public string? RuleName { get; set; }
			[Column(Length = 10)] public string? RuleType { get; set; }
			[Column(Length = 10)] public string? RuleVal { get; set; }
			[Column(Length = 10)] public string? RuleUnit { get; set; }
			[Column(Length = 10)] public string? Remark { get; set; }
		}

		public class Trp0041
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(Length = 10)] public string? CarNo { get; set; }
			[Column(Length = 10)] public string? FirstVal { get; set; }
		}

		class Rp002_R_GetPageList_Dto()
		{
			public int Id { get; set; }

			public string? CarNo { get; set; }
			public string? CarSelf { get; set; }
			public string? CarBrand { get; set; }
			public string? RateWgt { get; set; }
			public string? MastLeve { get; set; }
			public string? ForkPole { get; set; }
			public string? FirstVal { get; set; }
			public string? TelNo { get; set; }
			public string? RuleNo { get; set; }
			public string? RuleName { get; set; }
			public string? RuleType { get; set; }
			public string? RuleVal { get; set; }
			public string? RuleUnit { get; set; }
			public string? RecCreator { get; set; }
			public string? RecCreateTime { get; set; }
			public string? RecRevisor { get; set; }
			public string? RecReviseTime { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4751")]
		public void PagedLeftJoinsOverAsSubQuery([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context));
			using var tb1 = db.CreateLocalTable<Tdm100>();
			using var tb2 = db.CreateLocalTable<Trp004>();
			using var tb3 = db.CreateLocalTable<Trp003>();
			using var tb4 = db.CreateLocalTable<Trp0041>();

			var hasRule = string.Empty;
			string? carNo = null;
			string? carBrand = null;

			var query = (from t1 in tb1
						 from t2 in tb2.LeftJoin(x => x.CarNo == t1.CarNo)
						 from t3 in tb3.LeftJoin(x => x.RuleNo == t2.RuleNo)
						 from t4 in tb4.LeftJoin(x => x.CarNo == t1.CarNo)

						 orderby t1.CarNo
						 select new Rp002_R_GetPageList_Dto()
						 {
							 Id = t1.Id,
							 CarNo = t1.CarNo,
							 CarSelf = t1.CarSelf,
							 CarBrand = t1.CarBrand,
							 RateWgt = t1.RateWgt,
							 MastLeve = t1.MastLeve,
							 ForkPole = t1.ForkPole,
							 FirstVal = t4.FirstVal,
							 TelNo = t2.TelNo,
							 RuleNo = t2.RuleNo,
							 RuleName = t3.RuleName,
							 RuleType = t3.RuleType,
							 RuleVal = t3.RuleVal,
							 RuleUnit = t3.RuleUnit,
							 RecCreator =t2.RecCreator,
							 RecCreateTime = t2.RecCreateTime,
							 RecRevisor = t2.RecRevisor,
							 RecReviseTime = t2.RecReviseTime
						 });

			IQueryable<Rp002_R_GetPageList_Dto> query2;

			query2 = (from t in query.AsSubQuery() select t);

			var query3 = query2.Where(x => x.CarNo!.Contains(carNo!) && x.CarBrand!.Contains(carBrand!));

			var items = query3.Skip(20).Take(10).ToList();
			var totalCount = query3.Count();
		}

		[Test]
		public void DistinctSubqueryTest([DataSources(TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				where
					p.ParentID.In(db.Child.Select(c => c.ParentID).Distinct())
				select p;

			var sql = query.ToSqlQuery().Sql;

			Assert.That(sql,
				Should.Contain(
					"WHERE",
					"SELECT",
					"DISTINCT") &
				Should.Not.Contain(
					"WHERE",
					"SELECT",
					"SELECT",
					"DISTINCT"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1298")]
		public void SubqueryByScalarColumnIntoViewType([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			using (db.CreateLocalTable<mega_composites>())
			using (db.CreateLocalTable<Qwerty>())
			{
				db.Insert(new Qwerty() { Id = 1, asdfgh = "res1" });
				db.Insert(new Qwerty() { Id = 100500, asdfgh = "res100500" });

				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 1,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 1
						}
					}
				});

				var ref1 = db.GetTable<mega_composites>()
					.Select(x => new __mega_composites_View
					{
						ref1 = x.ref1,
						__face_ref1 = db.GetTable<Qwerty>().Where(q => q.Id == x.ref1).Select(q => q.asdfgh).FirstOrDefault()
					}).Take(2).ToArray();

				Assert.That(ref1, Is.Not.Null);
			}

		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1298")]
		public void SubqueryByNestedCompositeColumnIntoViewType([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			using (db.CreateLocalTable<mega_composites>())
			using (db.CreateLocalTable<Qwerty>())
			{
				db.Insert(new Qwerty() { Id = 1, asdfgh = "res1" });
				db.Insert(new Qwerty() { Id = 100500, asdfgh = "res100500" });

				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 1,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 1
						}
					}
				});

				var ref1 = db.GetTable<mega_composites>()
					.Select(x => new __mega_composites_View
					{
						y1 = x.y1,
						__face_y1_q1_ref1 = db.GetTable<Qwerty>().Where(q => q.Id == x.y1.q1.ref1).Select(q => q.asdfgh).FirstOrDefault()
					}).Take(2).ToArray();

				Assert.That(ref1, Is.Not.Null);
			}
		}

		[Table("qwerty")]
		public sealed class Qwerty
		{
			[Column]
			public long Id { get; set; }

			[Column]
			public string? asdfgh { get; set; }
		}

		[Table("mega_composites")]
		[Column("y1.q1.ref1", $"{nameof(y1)}.{nameof(mega_composites__y1.q1)}.{nameof(mega_composites__y1.mega_composites__y1__q1.ref1)}")]
		public class mega_composites
		{
			public mega_composites() : base()
			{
				y1 = new mega_composites__y1();
			}

			public virtual mega_composites__y1 y1 { get; set; }

			[Column]
			public long? ref1 { get; set; }

			public sealed class mega_composites__y1
			{
				public mega_composites__y1() : base()
				{
					q1 = new mega_composites__y1__q1();
				}

				public mega_composites__y1__q1 q1 { get; set; }

				public sealed class mega_composites__y1__q1
				{
					//[Column("\"y1.q1.ref1\"")]
					public long? ref1 { get; set; }
				}
			}
		}

		public sealed class __mega_composites_View : mega_composites
		{
			public string? __face_y1_q1_ref1 { get; set; }
			public string? __face_ref1 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2767")]
		public void CorrelatedCollectionProjectionOverLeftJoins([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<DBExercise>(DBExercise.Seed()))
			using (db.CreateLocalTable<DBDescription>())
			using (db.CreateLocalTable<DBExerciseEquipmentLinker>(DBExerciseEquipmentLinker.Seed()))
			using (db.CreateLocalTable<DBEquipment>(DBEquipment.Seed()))
			{
				var language        = "en";
				var currentLanguage = language;
				var OriginId        = new { Exercise = "1", Equipment = "2" };

				var query = from exercise in db.GetTable<DBExercise>()
							join exerciseDescription in db.GetTable<DBDescription>().Where(x => x.Locale == language && x.ObjectClass == OriginId.Exercise && x.Field == "expl") on Sql.Convert<string, int>(exercise.Id) equals exerciseDescription.ForeignKey into exDesc
							from exerciseDescription in exDesc.DefaultIfEmpty()
							join exerciseStartDescription in db.GetTable<DBDescription>().Where(x => x.Locale == language && x.ObjectClass == OriginId.Exercise && x.Field == "startexpl") on Sql.Convert<string, int>(exercise.Id) equals exerciseStartDescription.ForeignKey into exStartDesc
							from exerciseStartDescription in exStartDesc.DefaultIfEmpty()
							orderby exercise.Timestamp descending, exercise.Id descending
							select new ExerciseInfoSelectModel
							{
								Id          = exercise.Id,
								IsPrivate   = exercise.IsPrivate,
								Number      = exercise.Number,
								Level       = exercise.Level,
								Description = exerciseDescription != null ? exerciseDescription.Content : exercise.Description,
								Set         = exercise.Reeks,
								Times       = exercise.Num,
								Time        = exercise.Time,
								Rest        = exercise.Rest,
								Side        = exercise.Side,
								Image       = exercise.Image,
								Video       = exercise.Video,

								Equipments = (from equipmentLinker in db.GetTable<DBExerciseEquipmentLinker>()
											  join equipment in db.GetTable<DBEquipment>() on equipmentLinker.EquipmentId equals equipment.Id
											  join description in db.GetTable<DBDescription>().Where(x => x.Locale == currentLanguage && x.ObjectClass == OriginId.Equipment && x.Field == "name") on Sql.Convert<string, int>(equipment.Id) equals description.ForeignKey into desc
											  from description in desc.DefaultIfEmpty()
											  where exercise.Id == equipmentLinker.ExerciseId
											  select new DescriptionJoinModel<DBEquipment>
											  {
												  Object = equipment,
												  Description = description.Content,
												  DescriptionId = description.Id
											  })
							.ToList(),
								StartPositionDescription = exerciseStartDescription != null ? exerciseStartDescription.Content : exercise.Startexpl,
							};

				var result = query.ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Id, Is.EqualTo(3441));
					Assert.That(result[0].Equipments, Has.Count.EqualTo(1));
					Assert.That(result[0].Equipments.Any(x => x.Object.Id == 41), Is.True);

					Assert.That(result[1].Id, Is.EqualTo(3440));
					Assert.That(result[1].Equipments, Has.Count.EqualTo(1));
					Assert.That(result[1].Equipments.Any(x => x.Object.Id == 59), Is.True);

					Assert.That(result[2].Id, Is.EqualTo(3438));
					Assert.That(result[2].Equipments, Has.Count.EqualTo(2));
					Assert.That(result[2].Equipments.Any(x => x.Object.Id == 50), Is.True);
					Assert.That(result[2].Equipments.Any(x => x.Object.Id == 83), Is.True);
				}
			}
		}

		[Table("exercise")]
		public class DBExercise
		{
			[Column("id")        , PrimaryKey                   ] public int      Id          { get; set; }
			[Column("name")      , Nullable                     ] public string   Name        { get; set; } = null!;
			[Column("number")    , Nullable                     ] public int?     Number      { get; set; }
			[Column("startexpl") , Nullable                     ] public string   Startexpl   { get; set; } = null!;
			[Column("level")     , NotNull                      ] public int      Level       { get; set; }
			[Column("expl")      , NotNull                      ] public string   Description { get; set; } = null!;
			[Column("date")      , NotNull                      ] public DateTime Date        { get; set; } = TestData.DateTime;
			[Column("image")     , Nullable                     ] public string   Image       { get; set; } = null!;
			[Column("video")     , Nullable                     ] public string   Video       { get; set; } = null!;
			[Column("num")       , NotNull                      ] public int      Num         { get; set; }
			[Column("side")      , NotNull                      ] public int      Side        { get; set; }
			[Column("reeks")     , NotNull                      ] public int      Reeks       { get; set; }
			[Column("time")      , NotNull                      ] public int      Time        { get; set; }
			[Column("rest")      , NotNull                      ] public int      Rest        { get; set; }
			[Column("weight")    , NotNull                      ] public int      Weight      { get; set; }
			[Column("is_private"), NotNull                      ] public bool     IsPrivate   { get; set; }
			[Column("timestamp"  , SkipOnUpdate = true), NotNull] public DateTime Timestamp   { get; set; } = TestData.DateTime;

			public static DBExercise[] Seed()
			{
				return new DBExercise[]
				{
					new DBExercise { Id = 3438, Name = "ex1", Description = "desc 1" },
					new DBExercise { Id = 3440, Name = "ex2", Description = "desc 2" },
					new DBExercise { Id = 3441, Name = "ex3", Description = "desc 3" }
				};
			}
		}
		[Table("ext_translations")]
		public class DBDescription
		{
			[Column("id")          , PrimaryKey] public int     Id          { get; set; }
			[Column("locale")      , NotNull   ] public string? Locale      { get; set; }
			[Column("object_class"), NotNull   ] public string  ObjectClass { get; set; } = null!;
			[Column("field")       , NotNull   ] public string  Field       { get; set; } = null!;
			[Column("foreign_key") , NotNull   ] public string? ForeignKey  { get; set; }
			[Column("content")     , Nullable  ] public string  Content     { get; set; } = null!;
		}
		[Table("exercise_equipment_linker")]
		public class DBExerciseEquipmentLinker
		{
			[Column("exercise_id") , PrimaryKey(1), NotNull] public int ExerciseId  { get; set; }
			[Column("equipment_id"), PrimaryKey(2), NotNull] public int EquipmentId { get; set; }

			public static DBExerciseEquipmentLinker[] Seed()
			{
				return new[]
				{
					new DBExerciseEquipmentLinker { ExerciseId = 3438, EquipmentId = 50 },
					new DBExerciseEquipmentLinker { ExerciseId = 3438, EquipmentId = 83 },
					new DBExerciseEquipmentLinker { ExerciseId = 3440, EquipmentId = 59 },
					new DBExerciseEquipmentLinker { ExerciseId = 3441, EquipmentId = 41 }
				};
			}
		}
		[Table("exercise_equipment")]
		public class DBEquipment
		{
			[Column("id")                      , PrimaryKey] public int    Id                     { get; set; }
			[Column("parent_id")               , Nullable  ] public int?   ParentId               { get; set; }
			[Column("name")                    , NotNull   ] public string Name                   { get; set; } = null!;
			[Column("icon")                    , Nullable  ] public string Icon                   { get; set; } = null!;
			[Column("online")                  , NotNull   ] public bool   Online                 { get; set; }
			[Column("user_id")                 , Nullable  ] public int?   UserId                 { get; set; }
			[Column("original_creator_id")     , Nullable  ] public int?   OriginalCreatorId      { get; set; }
			[Column("is_private")              , NotNull   ] public bool   IsPrivate              { get; set; }
			[Column("organisation_id")         , Nullable  ] public int?   OrganisationId         { get; set; }
			[Column("original_organisation_id"), Nullable  ] public int?   OriginalOrganisationId { get; set; }

			public static DBEquipment[] Seed()
			{
				return new DBEquipment[]
				{
					new DBEquipment { Id = 50, Name = "AB wheel" },
					new DBEquipment { Id = 83, Name = "Airex balance beam" },
					new DBEquipment { Id = 59, Name = "Matje (geen materiaal)" },
					new DBEquipment { Id = 41, Name = "Bosu" }
				};
			}
		}

		public class DescriptionJoinModel<T>
		{
			public T                       Object        { get; set; } = default!;
			public int?                    DescriptionId { get; set; }
			public string                  Description   { get; set; } = null!;
			public DescriptionJoinModel<T> Child         { get; set; } = null!;
		}
		public class ExerciseInfoSelectModel
		{
			public int                                     Id                       { get; set; }
			public int?                                    Number                   { get; set; }
			public string                                  Name                     { get; set; } = null!;
			public int                                     Level                    { get; set; }
			public string                                  Description              { get; set; } = null!;
			public int?                                    Set                      { get; set; }
			public int?                                    Times                    { get; set; }
			public int?                                    Time                     { get; set; }
			public int?                                    Rest                     { get; set; }
			public int?                                    Side                     { get; set; }
			public int                                     EffectiveTime            { get; set; }
			public string                                  Image                    { get; set; } = null!;
			public string                                  Video                    { get; set; } = null!;
			public List<DescriptionJoinModel<DBEquipment>> Equipments               { get; set; } = null!;
			public string                                  StartPositionDescription { get; set; } = null!;
			public bool                                    IsPrivate                { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3906")]
		public void SumSubqueryProjectionWithNestedFirstOrDefault([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<SumSubqueryPosition>())
			using (db.CreateLocalTable<SumSubqueryInventory>())
			using (db.CreateLocalTable<SumSubqueryAddon>())
			{
				db.Insert(new SumSubqueryPosition() { Id = 1 });
				db.Insert(new SumSubqueryInventory() { InfeedAdviceID = 1, Quantity = 9 });
				db.Insert(new SumSubqueryAddon() { Id = 1, Nr = 77 });
				var qryA = from infeed in db.GetTable<SumSubqueryPosition>()
						   select new
						   {
							   InfeedAdvicePosition = infeed,
							   CurrentQuantity = db.GetTable<SumSubqueryInventory>().Where(x => x.InfeedAdviceID == infeed.Id).Sum(x => x.Quantity),
						   };

				var qryB = from oto in qryA
						   select new
						   {
							   oto,
							   InfeedAddons =
								   (from ir in db.GetTable<SumSubqueryAddon>()
									where ir.Id == oto.InfeedAdvicePosition.Id
									select ir).DefaultIfEmpty().FirstOrDefault(),
						   };

				var l = qryB.First();
			}
		}

		[Table]
		public class SumSubqueryPosition
		{
			[Column] public int Id { get; set; }
		}

		[Table]
		public class SumSubqueryInventory
		{
			[Column] public int InfeedAdviceID { get; set; }
			[Column] public int Quantity { get; set; }
		}

		[Table]
		public class SumSubqueryAddon
		{
			[Column] public int Id { get; set; }

			[Column] public int Nr { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4090")]
		public void NestedOuterApplyIdFirstEmptyLeaf([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new NestedApplyRoot { Id1 = 1, Name1 = "Some1" },
				new NestedApplyRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new NestedApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new NestedApplyMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new NestedApplyMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(Array.Empty<NestedApplyLeaf>());
			var ret = db.GetTable<NestedApplyRoot>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<NestedApplyMiddle>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Id2 = t2.Id2,
							Value2 = db.GetTable<NestedApplyLeaf>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Null);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4090")]
		public void NestedOuterApplyIdFirst([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new NestedApplyRoot { Id1 = 1, Name1 = "Some1" },
				new NestedApplyRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new NestedApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new NestedApplyMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new NestedApplyMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new NestedApplyLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new NestedApplyLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new NestedApplyLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<NestedApplyRoot>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<NestedApplyMiddle>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Id2 = t2.Id2,
							Value2 = db.GetTable<NestedApplyLeaf>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Not.Null);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4090")]
		public void NestedOuterApplyIdLastEmptyLeaf([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new NestedApplyRoot { Id1 = 1, Name1 = "Some1" },
				new NestedApplyRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new NestedApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new NestedApplyMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new NestedApplyMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(Array.Empty<NestedApplyLeaf>());
			var ret = db.GetTable<NestedApplyRoot>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<NestedApplyMiddle>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Value2 = db.GetTable<NestedApplyLeaf>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Id2 = t2.Id2,
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Null);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4090")]
		public void NestedOuterApplyIdLast([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new NestedApplyRoot { Id1 = 1, Name1 = "Some1" },
				new NestedApplyRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new NestedApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new NestedApplyMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new NestedApplyMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new NestedApplyLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new NestedApplyLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new NestedApplyLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<NestedApplyRoot>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<NestedApplyMiddle>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Value2 = db.GetTable<NestedApplyLeaf>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Id2 = t2.Id2,
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Not.Null);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4090")]
		public void NestedOuterApplyNullableFieldsWithIds([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new NestedApplyRoot { Id1 = 1, Name1 = "Some1" },
				new NestedApplyRoot { Id1 = 2, Name1 = null },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new NestedApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new NestedApplyMiddle { Id2 = 12, ParentId2 = 2, Name2 = "Child12" },
				new NestedApplyMiddle { Id2 = 13, ParentId2 = null, Name2 = "Child13" },
				new NestedApplyMiddle { Id2 = 14, ParentId2 = 1, Name2 = null },
				new NestedApplyMiddle { Id2 = 15, ParentId2 = 2, Name2 = null },
				new NestedApplyMiddle { Id2 = 16, ParentId2 = null, Name2 = null },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new NestedApplyLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new NestedApplyLeaf { Id3 = 22, ParentId3 = 12, Name3 = "Child22" },
				new NestedApplyLeaf { Id3 = 23, ParentId3 = 13, Name3 = "Child23" },
				new NestedApplyLeaf { Id3 = 24, ParentId3 = 14, Name3 = "Child24" },
				new NestedApplyLeaf { Id3 = 25, ParentId3 = 15, Name3 = "Child25" },
				new NestedApplyLeaf { Id3 = 26, ParentId3 = 16, Name3 = "Child26" },
				new NestedApplyLeaf { Id3 = 27, ParentId3 = null, Name3 = "Child27" },
			});
			var query = db.GetTable<NestedApplyLeaf>()
				.OrderBy(x => x.Id3)
				.Select(t3 => new
				{
					Name3 = t3.Name3,
					Value2 = db.GetTable<NestedApplyMiddle>()
						.Where(x => x.Id2 == t3.ParentId3)
						.Select(t2 => new
						{
							//first cross apply
							Value1 = db.GetTable<NestedApplyRoot>()
								.Where(x => x.Id1 == t2.ParentId2)
								.Select(t1 => new
								{
									//nested cross apply
									Name1 = t1.Name1,
									Id1 = t1.Id1,
								})
								.FirstOrDefault(),
							Name2 = t2.Name2,
							Id2 = t2.Id2,
						})
						.FirstOrDefault()
			});

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4090")]
		public void NestedOuterApplyNullableFieldsWithoutIds([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new NestedApplyRoot { Id1 = 1, Name1 = "Some1" },
				new NestedApplyRoot { Id1 = 2, Name1 = null },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new NestedApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new NestedApplyMiddle { Id2 = 12, ParentId2 = 2, Name2 = "Child12" },
				new NestedApplyMiddle { Id2 = 13, ParentId2 = null, Name2 = "Child13" },
				new NestedApplyMiddle { Id2 = 14, ParentId2 = 1, Name2 = null },
				new NestedApplyMiddle { Id2 = 15, ParentId2 = 2, Name2 = null },
				new NestedApplyMiddle { Id2 = 16, ParentId2 = null, Name2 = null },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new NestedApplyLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new NestedApplyLeaf { Id3 = 22, ParentId3 = 12, Name3 = "Child22" },
				new NestedApplyLeaf { Id3 = 23, ParentId3 = 13, Name3 = "Child23" },
				new NestedApplyLeaf { Id3 = 24, ParentId3 = 14, Name3 = "Child24" },
				new NestedApplyLeaf { Id3 = 25, ParentId3 = 15, Name3 = "Child25" },
				new NestedApplyLeaf { Id3 = 26, ParentId3 = 16, Name3 = "Child26" },
				new NestedApplyLeaf { Id3 = 27, ParentId3 = null, Name3 = "Child27" },
			});
			var ret = db.GetTable<NestedApplyLeaf>()
				.OrderBy(x => x.Id3)
				.Select(t3 => new
				{
					Name3 = t3.Name3,
					Value2 = db.GetTable<NestedApplyMiddle>()
						.Where(x => x.Id2 == t3.ParentId3)
						.Select(t2 => new
						{
							//first cross apply
							Value1 = db.GetTable<NestedApplyRoot>()
								.Where(x => x.Id1 == t2.ParentId2)
								.Select(t1 => new
								{
									//nested cross apply
									Name1 = t1.Name1,
								})
								.FirstOrDefault(),
							Name2 = t2.Name2,
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(7));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ret[0].Name3!, Is.EqualTo("Child21"));
				Assert.That(ret[0].Value2, Is.Not.Null);
				Assert.That(ret[0].Value2!.Name2!, Is.EqualTo("Child11"));
				Assert.That(ret[0].Value2!.Value1, Is.Not.Null);
				Assert.That(ret[0].Value2!.Value1!.Name1!, Is.EqualTo("Some1"));

				Assert.That(ret[1].Name3!, Is.EqualTo("Child22"));
				Assert.That(ret[1].Value2, Is.Not.Null);
				Assert.That(ret[1].Value2!.Name2!, Is.EqualTo("Child12"));
				Assert.That(ret[1].Value2!.Value1, Is.Not.Null);
				Assert.That(ret[1].Value2!.Value1!.Name1, Is.Null);

				Assert.That(ret[2].Name3!, Is.EqualTo("Child23"));
				Assert.That(ret[2].Value2, Is.Not.Null);
				Assert.That(ret[2].Value2!.Name2!, Is.EqualTo("Child13"));
				Assert.That(ret[2].Value2!.Value1, Is.Null);

				Assert.That(ret[3].Name3!, Is.EqualTo("Child24"));
				Assert.That(ret[3].Value2, Is.Not.Null);
				Assert.That(ret[3].Value2!.Name2!, Is.Null);
				Assert.That(ret[3].Value2!.Value1, Is.Not.Null);
				Assert.That(ret[3].Value2!.Value1!.Name1!, Is.EqualTo("Some1"));

				Assert.That(ret[4].Name3!, Is.EqualTo("Child25"));
				Assert.That(ret[4].Value2, Is.Not.Null);
				Assert.That(ret[4].Value2!.Name2!, Is.Null);
				Assert.That(ret[4].Value2!.Value1, Is.Not.Null);
				Assert.That(ret[4].Value2!.Value1!.Name1, Is.Null);

				Assert.That(ret[5].Name3!, Is.EqualTo("Child26"));
				Assert.That(ret[5].Value2, Is.Not.Null);
				Assert.That(ret[5].Value2!.Name2!, Is.Null);
				Assert.That(ret[5].Value2!.Value1, Is.Null);

				Assert.That(ret[6].Name3!, Is.EqualTo("Child27"));
				Assert.That(ret[6].Value2, Is.Null);
			}
		}

		[Table("NestedApplyRoot")]
		public partial class NestedApplyRoot
		{
			[Column("ID1"), PrimaryKey, NotNull] public int Id1 { get; set; }
			[Column("NAME1"), Nullable] public string? Name1 { get; set; }
		}

		[Table("NestedApplyMiddle")]
		public partial class NestedApplyMiddle
		{
			[Column("ID2"), PrimaryKey, NotNull] public int Id2 { get; set; }
			[Column("PARENTID2"), Nullable] public int? ParentId2 { get; set; }
			[Column("NAME2"), Nullable] public string? Name2 { get; set; }
		}

		[Table("NestedApplyLeaf")]
		public partial class NestedApplyLeaf
		{
			[Column("ID3"), PrimaryKey, NotNull] public int Id3 { get; set; }
			[Column("PARENTID3"), Nullable] public int? ParentId3 { get; set; }
			[Column("NAME3"), Nullable] public string? Name3 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void FirstOrDefaultSubqueryInAnonymousProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = (from sep in db.Parent
						  select new
						  {
							  Child =
								  (from l in db.Child
								   select new
								   {
									   Id = l.ParentID + 1
								   }).FirstOrDefault()
						  }).ToList();

			var expected = from sep in Parent
						   select new
						   {
							   Child =
								   (from l in Child
									select new
									{
										Id = l.ParentID + 1
									}).FirstOrDefault()
						   };

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void FirstOrDefaultSubqueryInAnonymousProjectionWithParentId([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = (from sep in db.Parent
						  select new
						  {
							  Child =
								  (from l in db.Child
								   select new
								   {
									   Id = l.ParentID + 1,
									   ParentId = l.ParentID
								   }).FirstOrDefault()
						  }).ToList();

			var expected = from sep in Parent
						   select new
						   {
							   Child =
								   (from l in Child
									select new
									{
										Id = l.ParentID + 1,
										ParentId = l.ParentID
									}).FirstOrDefault()
						   };

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void FirstOrDefaultSubqueryInTypedProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = (from sep in db.Parent
						  select new ProjectedChildWrapper
						  {
							  Child =
								  (from l in db.Child
								   select new ProjectedChild
								   {
									   Id = l.ParentID + 1
								   }).FirstOrDefault()
						  }).ToList();

			var expected = from sep in Parent
						   select new ProjectedChildWrapper
						   {
							   Child =
								   (from l in Child
									select new ProjectedChild
									{
										Id = l.ParentID + 1
									}).FirstOrDefault()
						   };

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void FirstOrDefaultSubqueryInTypedProjectionWithParentId([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = (from sep in db.Parent
						  select new ProjectedChildWrapper
						  {
							  Child =
								  (from l in db.Child
								   select new ProjectedChild
								   {
									   Id = l.ParentID + 1,
									   ParentId = l.ParentID
								   }).FirstOrDefault()
						  }).ToList();

			var expected = from sep in Parent
						   select new ProjectedChildWrapper
						   {
							   Child =
								   (from l in Child
									select new ProjectedChild
									{
										Id = l.ParentID + 1,
										ParentId = l.ParentID
									}).FirstOrDefault()
						   };

			AreEqual(expected, result);
		}

		// Sybase do not supports limiting subqueries
		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		public void FirstOrDefaultScalarSubqueryInProjection([DataSources(TestProvName.AllSybase, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			var expected =    Parent.Select(p =>    Child.Select(c => c.ParentID + 1).FirstOrDefault());
			var result   = db.Parent.Select(p => db.Child.Select(c => c.ParentID + 1).FirstOrDefault());

			AreEqual(expected, result);
		}

		// Sybase do not supports limiting subqueries
		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		public void FirstOrDefaultScalarSubqueryInAnonymousColumn([DataSources(TestProvName.AllSybase, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			var expected =    Parent.Select(p => new { Id = p.ParentID, V =    Child.Select(c => c.ParentID + 1).FirstOrDefault() }).ToList().Select(_ => _.V);
			var result   = db.Parent.Select(p => new { Id = p.ParentID, V = db.Child.Select(c => c.ParentID + 1).FirstOrDefault() }).ToList().Select(_ => _.V);

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		public void NestedTypedProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(_ => new ProjectedChildWrapper { Child = new ProjectedChild { Id = _.ParentID + 1 } }),
				db.Parent.Select(_ => new ProjectedChildWrapper { Child = new ProjectedChild { Id = _.ParentID + 1 } })
			);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/461")]
		public void NestedAnonymousProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(_ => new { Child = new { Id = _.ParentID + 1, ParentId = _.ParentID } }),
				db.Parent.Select(_ => new { Child = new { Id = _.ParentID + 1, ParentId = _.ParentID } })
			);
		}

		public class ProjectedChild
		{
			//public ProjectedChild(int id)
			//{
			//	Id = id;
			//}

			//public ProjectedChild()
			//	: this(0)
			//{ }

			public int Id;

			public int ParentId;

			public override bool Equals(object? obj)
			{
				if (!(obj is ProjectedChild vh))
					return false;

				if (ReferenceEquals(this, vh))
					return true;

				return Id == vh.Id && ParentId == vh.ParentId;
			}

			public override int GetHashCode()
			{
				return (Id * 407) ^ (ParentId * -407);
			}
		}

		public class ProjectedChildWrapper
		{
			public ProjectedChild? Child;

			public override bool Equals(object? obj)
			{
				if (!(obj is ProjectedChildWrapper vvh))
					return false;

				if (ReferenceEquals(this, vvh))
					return true;

				if (Child != null)
					return Child.Equals(vvh.Child);

				return vvh.Child == null;
			}

			public override int GetHashCode()
			{
				return Child != null ? Child.GetHashCode() : 0;
			}
		}

		[Table]
		public sealed class NestedFilterTransition
		{
			[PrimaryKey]                public int      ThingId        { get; set; }
			[PrimaryKey]                public DateTime CreatedDate    { get; set; }
			[Column(CanBeNull = false)] public string   TransitionType { get; set; } = null!;
		}

		[Table]
		public sealed class NestedFilterState
		{
			[PrimaryKey] public int       ThingId            { get; set; }
			[Column]     public DateTime? LastTransitionDate { get; set; }
		}

		[Table]
		public sealed class NestedFilterThing
		{
			[PrimaryKey] public int Id { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5193")]
		[ThrowsRequiresCorrelatedSubquery]
		[ThrowsRequiredOuterJoins(TestProvName.AllNativeAccess, TestProvName.AllMariaDB, TestProvName.AllMySql57, TestProvName.AllOracle11)]
		public void NestedSingleOrDefaultSubqueriesInFilter([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<NestedFilterTransition>();
			using var t2 = db.CreateLocalTable<NestedFilterState>();
			using var t3 = db.CreateLocalTable<NestedFilterThing>();

			Expression<Func<NestedFilterTransition?, bool?, bool>> fitsUndeletedStatusFilter =
			(transition, filter) =>
				filter == null || filter == (transition == null || transition.TransitionType != "Delete");

			Expression<Func<NestedFilterState?, bool?, bool>> stateFitsUndeletedStatusFilter =
			(state, filter) =>
				state != null
					? fitsUndeletedStatusFilter.Compile()(
						t1.SingleOrDefault(t =>
							t.ThingId == state.ThingId &&
							t.CreatedDate == state.LastTransitionDate),
						filter)
					: fitsUndeletedStatusFilter.Compile()(null, filter);

			Expression<Func<NestedFilterThing, bool?, bool>> thingFitsUndeletedStatusFilter =
			(thing, filter) => stateFitsUndeletedStatusFilter.Compile()(
				t2.SingleOrDefault(s => s.ThingId == thing.Id), filter);

			var filterQuery = t3.Where(thing => thingFitsUndeletedStatusFilter.Compile()(thing, true));
			filterQuery.Select(thing => thing.Id).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5193")]
		[ThrowsRequiresCorrelatedSubquery]
		[ThrowsRequiredOuterJoins(TestProvName.AllNativeAccess, TestProvName.AllMariaDB, TestProvName.AllMySql57, TestProvName.AllOracle11)]
		public void NestedSingleOrDefaultSubqueriesInFilterMerged([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<NestedFilterTransition>();
			using var t2 = db.CreateLocalTable<NestedFilterState>();
			using var t3 = db.CreateLocalTable<NestedFilterThing>();

			var filterQuery = t3.Where(thing => t2.SingleOrDefault(s => s.ThingId == thing.Id) != null
					? t1.SingleOrDefault(t =>
							t.ThingId == t2.SingleOrDefault(s => s.ThingId == thing.Id)!.ThingId
							&& t.CreatedDate == t2.SingleOrDefault(s => s.ThingId == thing.Id)!.LastTransitionDate) == null
							|| t1.SingleOrDefault(t => t.ThingId == t2.SingleOrDefault(s => s.ThingId == thing.Id)!.ThingId &&
							t.CreatedDate == t2.SingleOrDefault(s => s.ThingId == thing.Id)!.LastTransitionDate)!.TransitionType != "Delete"
					: true);
			filterQuery.Select(thing => thing.Id).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5193")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void NestedSingleOrDefaultSubqueriesInFilterRewrittenWithAny([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<NestedFilterTransition>();
			using var t2 = db.CreateLocalTable<NestedFilterState>();
			using var t3 = db.CreateLocalTable<NestedFilterThing>();

			Expression<Func<NestedFilterThing, bool>> thingHasDeletedState =
			thing => t2
				.Where(state => state.ThingId == thing.Id)
				.SelectMany(state => t1
					.Where(t => t.ThingId == state.ThingId && t.CreatedDate == state.LastTransitionDate))
				.Any(transition => transition.TransitionType == "Delete");
			var filterQuery = t3.Where(thing => !thingHasDeletedState.Compile()(thing));

			filterQuery.Select(thing => thing.Id).ToList();
		}

		[Table("AliasBaseTable")]
		sealed class AliasBaseRow
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int Value { get; set; }
		}

		[Table("AliasDetailTable")]
		sealed class AliasDetailRow
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int OutId { get; set; }
			[Column]     public int Cnt   { get; set; }
		}

		// Behavior pin for PR #5657 alias finalization.
		// Parameter-dependent queries (nullable-param WHERE) set IsParameterDependent = true, which means
		// the build-time ConvertElement(selectQuery.Select) runs in BuildColumns after aliasing.
		// Verifies that correlated field references inside a scalar subquery still resolve to the correct
		// outer-table alias after that build-time convert pass.
		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void ParameterDependentWithCorrelatedSubquery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable<AliasBaseRow>();
			using var inner = db.CreateLocalTable<AliasDetailRow>();

			outer.Insert(() => new AliasBaseRow { Id = 1, Value = 10 });
			outer.Insert(() => new AliasBaseRow { Id = 2, Value = 20 });
			inner.Insert(() => new AliasDetailRow { Id = 1, OutId = 1, Cnt = 3 });
			inner.Insert(() => new AliasDetailRow { Id = 2, OutId = 1, Cnt = 5 });
			inner.Insert(() => new AliasDetailRow { Id = 3, OutId = 2, Cnt = 7 });

			int? minValue = 15;

			var result =
				(from o in outer
				 where o.Value >= minValue
				 select new
				 {
					 o.Id,
					 o.Value,
					 InnerCount = inner.Where(i => i.OutId == o.Id).Count()
				 }).ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(2);
			result[0].InnerCount.ShouldBe(1);
		}

		// Same shape with a derived inner table (GroupBy forces a nested SelectQuery inside the scalar subquery).
		// The GroupBy turns the scalar correlated subquery into a derived-table (OUTER/LATERAL) join: YDB /
		// ClickHouse can express it and return correct results (unlike the simple form they reject), while
		// Sybase rejects the OUTER/LATERAL join it lowers to (a different limitation than the simple form's
		// correlated-subquery rejection).
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void ParameterDependentWithCorrelatedDerivedSubquery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable<AliasBaseRow>();
			using var inner = db.CreateLocalTable<AliasDetailRow>();

			outer.Insert(() => new AliasBaseRow { Id = 1, Value = 10 });
			outer.Insert(() => new AliasBaseRow { Id = 2, Value = 20 });
			inner.Insert(() => new AliasDetailRow { Id = 1, OutId = 1, Cnt = 3 });
			inner.Insert(() => new AliasDetailRow { Id = 2, OutId = 1, Cnt = 5 });
			inner.Insert(() => new AliasDetailRow { Id = 3, OutId = 2, Cnt = 7 });

			int? minValue = 15;

			var result =
				(from o in outer
				 where o.Value >= minValue
				 select new
				 {
					 o.Id,
					 o.Value,
					 SumCnt = inner
						.Where(i => i.OutId == o.Id)
						.GroupBy(i => i.OutId)
						.Select(g => g.Sum(x => x.Cnt))
						.FirstOrDefault()
				 }).ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(2);
			result[0].SumCnt.ShouldBe(7);
		}
	}
}
