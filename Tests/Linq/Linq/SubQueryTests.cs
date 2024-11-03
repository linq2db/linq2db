using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Mapping;
	using Model;

	[TestFixture]
	public class SubQueryTests : TestBase
	{
		[Test]
		public void Test1([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void Test2([DataSources(TestProvName.AllClickHouse)] string context)
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
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
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
		public void Test5([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void Test6([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void Test7([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child select new { Count =    GrandChild.Where(g => g.ChildID == c.ChildID).Count() },
					from c in db.Child select new { Count = db.GrandChild.Where(g => g.ChildID == c.ChildID).Count() });
		}

		[Test]
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
		[ThrowsForProvider(typeof(SqlException), providers: [TestProvName.AllAccess, TestProvName.AllSybase], ErrorMessage = "Skip for subqueries is not supported")]
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
		public void SubSub1([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SubSub2([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
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
		public void SubSub21([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SubSub211([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SubSub212([DataSources(TestProvName.AllClickHouse)] string context)
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
		public void SubSub22([DataSources(
			ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2,
			TestProvName.AllClickHouse,
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
		public void Count1([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
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
		public void Count3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
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
			[Column] public int Agent_Id { get; set; }
			[Column] public int Distributor_Id { get; set; }
			[Column] public int Contract_Id { get; set; }
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
			[Column] public int Agent_Id { get; set; }
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
			[Column] public int Distributor_Id { get; set; }
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
			[Column] public int Distributor_Id { get; set; }
			[Column] public int Commercial_Property_Id { get; set; }
			[Column] public string? Distributor_Type_Code { get; set; }

			public static readonly Distributor_Commercial_Propert[] Data = new[]
			{
				new Distributor_Commercial_Propert() { Distributor_Id = 1, Commercial_Property_Id = 1, Distributor_Type_Code = "RE" }
			};
		}

		[Table]
		sealed class Commercial_Property
		{
			[Column              ] public int     Commercial_Property_Id { get; set; }
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
			[Column] public int Contract_Id { get; set; }
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
			[Column] public string? City_Code { get; set; }
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
		public void DropOrderByFromNonLimitedSubquery([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.Where(p => db.Child.Where(c => c.ParentID == p.ParentID)
					.Any(c => db.GrandChild.Select(gc => gc.ChildID).OrderBy(id => id).Contains(c.ChildID)));

			AssertQuery(query);
		}
		
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
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
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
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
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
	}
}
