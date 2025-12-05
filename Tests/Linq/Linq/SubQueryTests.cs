using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

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
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max(),
					from p in db.Parent
					where p.ParentID != 5
					select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max());
		}

		[Test]
		public void Test3([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
			{
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
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Test6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Test7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child select new { Count =    GrandChild.Where(g => g.ChildID == c.ChildID).Count() },
					from c in db.Child select new { Count = db.GrandChild.Where(g => g.ChildID == c.ChildID).Count() });
		}

		[Test]
		[YdbCteAsSource]
		public void Test8([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var parent  =
					from p in db.Parent
					where p.ParentID == 1
					select p.ParentID;

				var chilren =
					from c in db.Child
					where parent.Contains(c.ParentID)
					select c;

				var chs1 = chilren.ToList();

				parent  =
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
		}

		[Test]
		public void DerivedTake([DataSources]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Take(1).AsSubQuery());
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllAccess, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_Skip_in_Subquery)]
		public void DerivedSkipTake([DataSources]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Skip(1).Take(1).AsSubQuery());
			}
		}

		[Test]
		public void ObjectCompare([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
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
		[YdbMemberNotFound]
		public void Contains1([DataSources(
			TestProvName.AllInformix,
			TestProvName.AllClickHouse,
			TestProvName.AllSybase,
			TestProvName.AllSapHana,
			TestProvName.AllAccess,
			TestProvName.AllOracle,
			TestProvName.AllMySql,
			ProviderName.DB2)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where (from p1 in Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
					select p,
					from p in db.Parent
					where (from p1 in db.Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
					select p);
		}

		[Test]
		[YdbMemberNotFound]
		public void Contains2([DataSources(
			TestProvName.AllClickHouse,
			TestProvName.AllMySql,
			TestProvName.AllSybase,
			TestProvName.AllSapHana,
			TestProvName.AllAccess,
			TestProvName.AllOracle,
			ProviderName.DB2)]
			string context)
		{
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
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
			TestProvName.AllAccess,
			ProviderName.DB2,
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext(context))
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
		public void Count2([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in
						from p in Parent
						select new Parent
						{
							ParentID = p.ParentID,
							Value1   = p.Children.Where(t => t.ParentID > 0).Sum(t => t.ParentID) / 2,
						}
					where p.Value1 > 1
					select p,
					from p in
						from p in db.Parent
						select new Parent
						{
							ParentID = p.ParentID,
							Value1   = p.Children.Where(t => t.ParentID > 0).Sum(t => t.ParentID) / 2,
						}
					where p.Value1 > 1
					select p);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Count3([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
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

		[Test]
		public void Issue1601([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
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

		[Test]
		public void Issue383Test1([DataSources(false, TestProvName.AllClickHouse)] string context)
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

		[Test]
		public void Issue383Test2([DataSources(false, TestProvName.AllClickHouse)] string context)
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
		[YdbCteAsSource]
		public void DropOrderByFromNonLimitedSubquery([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.Where(p => db.Child.Where(c => c.ParentID == p.ParentID)
					.Any(c => db.GrandChild.Select(gc => gc.ChildID).OrderBy(id => id).Contains(c.ChildID)));

			AssertQuery(query);
		}

		#region Issue 4458
		[Table]
		sealed class Issue4458Item
		{
			[Column(CanBeNull = false, Length = 100), PrimaryKey] public string Id { get; set; } = null!;

			public static readonly Issue4458Item[] Data =
			[
				new Issue4458Item() { Id = "1", },
				new Issue4458Item() { Id = "2", },
				new Issue4458Item() { Id = "3", }
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

		[ThrowsRequiresCorrelatedSubquery]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4458")]
		public void Issue4458Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Issue4458Item.Data);
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

		[ThrowsRequiresCorrelatedSubquery]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4458")]
		public void Issue4458Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Issue4458Item.Data);
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
		#endregion

		#region Issue 4347
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
		public void Issue4347Test1([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllDB2, TestProvName.AllMariaDB, TestProvName.AllOracle11)] string context)
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
		public void Issue4347Test2([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllDB2, TestProvName.AllMariaDB, TestProvName.AllOracle11)] string context)
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
		public void Issue4347Test3([DataSources] string context)
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
		#endregion

		[ActiveIssue(Configurations = [TestProvName.AllOracle], Details = "https://forums.oracle.com/ords/apexds/post/error-ora-12704-character-set-mismatch-in-case-statement-6917")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3295")]
		public void Issue3295Test1([DataSources(TestProvName.AllSybase)] string context)
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

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3295")]
		public void Issue3295Test2([DataSources] string context)
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
		public void Issue3334Test([DataSources] string context)
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
		public void Issue3365Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child.Select(x => new
			{
				Assignee = x.GrandChildren.Select(a => a.ParentID).FirstOrDefault()
			});

			var orderedQuery = query.OrderBy(x => x.Assignee);

			orderedQuery.ToArray();
		}

		#region Issue 4184

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4184")]
		public void Issue4184Test([DataSources] string context)
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

		#endregion

		// technically it is not correct as such rownum generation is not guaranteed to follow query ordering
		// that's why many dbs disabled and only sqlserver and oracle work
		[Test]
		public void PreserveOrderInSubqueryWithWindowFunction_WithOrder([DataSources(
			TestProvName.AllSqlServer2008Minus, TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase,
			TestProvName.AllClickHouse, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySql,
			TestProvName.AllPostgreSQL, TestProvName.AllSapHana, TestProvName.AllSQLite, TestProvName.AllDB2
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
		public void PreserveOrderInSubqueryWithWindowFunction_NoOrdering([DataSources(TestProvName.AllAccess, ProviderName.SqlCe, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase)] string context)
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

		#region Issue 4751

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
		public void Issue4751Test([DataSources] string context)
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
		#endregion
	}
}
