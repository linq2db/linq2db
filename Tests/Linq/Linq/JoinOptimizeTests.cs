﻿using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class JoinOptimizeTests : TestBase
	{
		[Test]
		public void InnerJoinToSelf([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);

				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on o1.OrderID equals o2.OrderID
					join o3 in db.Order on new {o1.OrderID, ID2 = o2.OrderID} equals new {o3.OrderID, ID2 = o3.OrderID}
					join od1 in db.OrderDetail on new {o1.OrderID, od.ProductID} equals new {od1.OrderID, od1.ProductID}
					join od2 in db.OrderDetail on new {od1.OrderID, od.ProductID} equals new {od2.OrderID, od2.ProductID}
					join od3 in db.OrderDetail on new {od1.OrderID, od2.ProductID} equals new {od3.OrderID, od3.ProductID}
					orderby od.OrderID, od.ProductID
					select new
					{
						OrderID = od.OrderID,
						ProductID = od.ProductID,
						OrderID1 = od3.OrderID,
						OrderID2 = od2.OrderID,
					};

				var q2 = from od in dd.OrderDetail
					join o1 in dd.Order on od.OrderID equals o1.OrderID
					join o2 in dd.Order on o1.OrderID equals o2.OrderID
					join o3 in dd.Order on new {o1.OrderID, ID2 = o2.OrderID} equals new {o3.OrderID, ID2 = o3.OrderID}
					join od1 in dd.OrderDetail on new {o1.OrderID, od.ProductID} equals new {od1.OrderID, od1.ProductID}
					join od2 in dd.OrderDetail on new {od1.OrderID, od.ProductID} equals new {od2.OrderID, od2.ProductID}
					join od3 in dd.OrderDetail on new {od1.OrderID, od2.ProductID} equals new {od3.OrderID, od3.ProductID}
					orderby od.OrderID, od.ProductID
					select new
					{
						OrderID = od.OrderID,
						ProductID = od.ProductID,
						OrderID1 = od3.OrderID,
						OrderID2 = od2.OrderID,
					};

				Assert.That(q2, Is.EqualTo(q));

				var ts = q.GetTableSource();
				Assert.That(ts.Joins, Has.Count.EqualTo(1));
			}
		}
		[Test]
		public void InnerJoin([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);

				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on new {od.OrderID, od.ProductID} equals new {OrderID = o2.OrderID, ProductID = 1}
					join o3 in db.Order on od.OrderID equals o3.OrderID
					join od2 in db.OrderDetail on new {od.OrderID, od.ProductID} equals new {od2.OrderID, od2.ProductID}
					join od3 in db.OrderDetail on new {od2.OrderID, od2.ProductID} equals new {od3.OrderID, od3.ProductID}
					select new
					{
						OrderID = od.OrderID,
						OrderDate = o3.OrderDate,
						ProductID = od3.ProductID,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
						OrderID3 = o3.OrderID,
					};

				var q2 = from od in dd.OrderDetail
					join o1 in dd.Order on od.OrderID equals o1.OrderID
					join o2 in dd.Order on new {od.OrderID, od.ProductID} equals new {OrderID = o2.OrderID, ProductID = 1}
					join o3 in dd.Order on od.OrderID equals o3.OrderID
					join od2 in dd.OrderDetail on new {od.OrderID, od.ProductID} equals new {od2.OrderID, od2.ProductID}
					join od3 in dd.OrderDetail on new {od2.OrderID, od2.ProductID} equals new {od3.OrderID, od3.ProductID}
					select new
					{
						OrderID = od.OrderID,
						OrderDate = o3.OrderDate,
						ProductID = od3.ProductID,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
						OrderID3 = o3.OrderID,
					};

				Assert.Multiple(() =>
				{
					Assert.That(q2, Is.EqualTo(q));

					Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1));
				});

				var proj1 = q.Select(v => v.OrderID);
				proj1.ToArray();
				var sq1 = proj1.GetSelectQuery();
				Assert.Multiple(() =>
				{
					Assert.That(sq1.GetTableSource().Joins, Has.Count.EqualTo(1));
					Assert.That(sq1.GetWhere().Predicates, Is.Empty);
				});

				var proj2 = q.Select(v => v.OrderDate);
				proj2.ToArray();
				var sq2 = proj2.GetSelectQuery();
				Assert.Multiple(() =>
				{
					Assert.That(sq2.GetTableSource().Joins, Has.Count.EqualTo(1));
					Assert.That(sq2.GetWhere().Predicates, Is.Empty);
				});
			}
		}

		[Test]
		public void InnerJoinFalse([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);

				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join od1 in db.OrderDetail on new { o1.OrderID, od.ProductID } equals new { od1.OrderID, od1.ProductID }
					join od2 in db.OrderDetail on new { od.OrderID, od.ProductID } equals new { od2.OrderID, od2.ProductID }
					orderby o1.OrderID, od.ProductID
					select new
					{
						OrderID = od.OrderID,
						ProductID = od.ProductID,
						OrderID1 = o1.OrderID,
						OrderID2 = od2.OrderID,
					};

				var q2 = from od in dd.OrderDetail
					join o1 in dd.Order on od.OrderID equals o1.OrderID
					join od1 in dd.OrderDetail on new { o1.OrderID, od.ProductID } equals new { od1.OrderID, od1.ProductID }
					join od2 in dd.OrderDetail on new { o1.OrderID, od.ProductID } equals new { od2.OrderID, od2.ProductID }
					orderby o1.OrderID, od.ProductID
					select new
					{
						OrderID = od.OrderID,
						ProductID = od.ProductID,
						OrderID1 = o1.OrderID,
						OrderID2 = od2.OrderID,
					};

				Assert.That(q2, Is.EqualTo(q));

				var ts = q.GetTableSource();
				Assert.That(ts.Joins, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void LeftJoin([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);

				var q = from od in db.OrderDetail
					join o1 in db.Order on new {od.OrderID, od.ProductID} equals new {o1.OrderID, ProductID = 39}
					join e1 in db.Employee on o1.EmployeeID equals e1.EmployeeID
					from o2 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o3 in db.Order.Where(o => o.OrderID == od.OrderID && od.ProductID == 1).DefaultIfEmpty()
					from o4 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o5 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o6 in db.Order.Where(o => o.OrderID == od.OrderID && od.ProductID == 1).DefaultIfEmpty()
					from o7 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					join o8 in db.Order on od.OrderID equals o8.OrderID
					join e2 in db.Employee on o8.EmployeeID equals e2.EmployeeID
					from o9 in db.OrderDetail.Where(d => d.OrderID == od.OrderID && d.ProductID == od.ProductID).DefaultIfEmpty()
					from o10 in db.OrderDetail.Where(d => d.OrderID == od.OrderID && d.ProductID == od.ProductID).DefaultIfEmpty()
					where o5 != null && o5.OrderID > 1000
					orderby od.OrderID
					select new
					{
						OrderID = od.OrderID,
						OrderID1 = o1 == null ? 0 : o1.OrderID,
						OrderID2 = o2 == null ? 0 : o2.OrderID,
						OrderID3 = o3 == null ? 0 : o3.OrderID,
						OrderID4 = o4 == null ? 0 : o4.OrderID,
					};

				var q2 = from od in dd.OrderDetail
					join o1 in dd.Order on new {od.OrderID, od.ProductID} equals new {o1.OrderID, ProductID = 39}
					from o2 in dd.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o3 in dd.Order.Where(o => o.OrderID == od.OrderID && od.ProductID == 1).DefaultIfEmpty()
					from o4 in dd.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o5 in dd.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o6 in dd.Order.Where(o => o.OrderID == od.OrderID && od.ProductID == 1).DefaultIfEmpty()
					from o7 in dd.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					join o8 in dd.Order on od.OrderID equals o8.OrderID
					from o9 in dd.OrderDetail.Where(d => d.OrderID == od.OrderID && d.ProductID == od.ProductID).DefaultIfEmpty()
					from o10 in dd.OrderDetail.Where(d => d.OrderID == od.OrderID && d.ProductID == od.ProductID).DefaultIfEmpty()
					where o5 != null && o5.OrderID > 1000
					orderby o1.OrderID
					select new
					{
						OrderID = od.OrderID,
						OrderID1 = o1 == null ? 0 : o1.OrderID,
						OrderID2 = o2 == null ? 0 : o2.OrderID,
						OrderID3 = o3 == null ? 0 : o3.OrderID,
						OrderID4 = o4 == null ? 0 : o4.OrderID,
					};

				Assert.That(q2, Is.EqualTo(q));

				var ts = q.GetTableSource();
				Assert.Multiple(() =>
				{
					Assert.That(ts.Joins.Count(j => j.JoinType == JoinType.Inner), Is.EqualTo(2));
					Assert.That(ts.Joins.Count(j => j.JoinType == JoinType.Left), Is.EqualTo(3));
				});
			}
		}

		[Test]
		public void InnerJoin1([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on od.OrderID equals o2.OrderID
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1));

				var proj1 = q.Select(v => v.OrderID);
				Assert.That(proj1.GetTableSource().Joins, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void InnerJoinSubquery([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q1 = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on od.OrderID equals o2.OrderID
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				var q2 = from e in q1.Take(10)
					join o1 in db.Order on e.OrderID equals o1.OrderID
					select e;

				q2.ToArray();
				var ts = q2.GetTableSource();
				Assert.That(ts.Joins, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void InnerJoinMixKeys([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on o1.OrderID equals o2.OrderID
					join o3 in db.Order on o2.OrderID equals o3.OrderID
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
						OrderID3 = o3.OrderID,
					};

				q.ToArray();

				Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1));

				var proj1 = q.Select(v => v.OrderID);
				proj1.ToArray();
				Assert.That(proj1.GetTableSource().Joins, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void InnerAndLeftMixed([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					from o2 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o3 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					join o4 in db.Order on o1.OrderID equals o4.OrderID
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
						OrderID3 = o3.OrderID,
						OrderID4 = o4.OrderID,
					};

				q.ToArray();
				Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1));

				var proj1 = q.Select(v => v.OrderID);
				proj1.ToArray();
				Assert.That(proj1.GetTableSource().Joins, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void InnerJoin2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					join o1 in db.Order on new { od.OrderID, od.ProductID} equals new { o1.OrderID, ProductID = 100 }
					join o2 in db.Order on od.OrderID equals o2.OrderID
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				var sql = q.GetSelectQuery();
				Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(sql.GetTableSource().Joins.First().Condition.Predicates, Has.Count.EqualTo(2));
					Assert.That(sql.GetWhere().Predicates, Is.Empty);
				});

				var proj1 = q.Select(v => v.OrderID);
				var sql1 = proj1.GetSelectQuery();
				Assert.Multiple(() =>
				{
					Assert.That(sql1.GetTableSource().Joins, Has.Count.EqualTo(1));
					Assert.That(sql1.GetWhere().Predicates, Is.Empty);
				});
			}
		}

		[Test]
		public void InnerJoin3([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on od.OrderID equals o2.OrderID
					join o3 in db.Order on od.OrderID equals o3.OrderID
					where o1.OrderDate == TestData.DateTime || o2.OrderDate < TestData.DateTime && o3.EmployeeID != null
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
						OrderID3 = o3.OrderID,
					};

				Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void LeftJoin1([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					from o1 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o2 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				var ts = q.GetTableSource();
				Assert.That(ts.Joins.Count(j => j.JoinType == JoinType.Left), Is.EqualTo(1));
			}
		}

		[Test]
		public void LeftJoin2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					from o1 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o2 in db.Order.Where(o => o.OrderID == od.OrderID && od.ProductID == 100).DefaultIfEmpty()
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				var ts = q.GetTableSource();
				Assert.That(ts.Joins.Count(j => j.JoinType == JoinType.Left), Is.EqualTo(2));
			}
		}

		[Test]
		public void LeftJoinProjection([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					from o1 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o2 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1), "Join not optimized");

				var qw = q.Where(v => v.OrderDate != null);
				Assert.That(qw.GetTableSource().Joins, Has.Count.EqualTo(2), "If LEFT join is used in where condition - it can not be optimized");

				var proj1 = q.Select(v => v.OrderID1);
				Assert.That(proj1.GetTableSource().Joins, Has.Count.EqualTo(1));

				var proj2 = qw.Select(v => v.OrderID1);
				Assert.That(proj2.GetTableSource().Joins, Has.Count.EqualTo(1));

				var proj3 = q.Select(v => v.OrderID);
				Assert.That(proj3.GetTableSource().Joins, Is.Empty, "All joins should be optimized");
			}
		}

		[Test]
		public void LeftJoinProjectionSubquery([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q1 = from od in db.OrderDetail
					from o1 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o2 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				var q = from od in q1.Take(10)
					from o1 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					from o2 in db.Order.Where(o => o.OrderID == od.OrderID).DefaultIfEmpty()
					orderby od.OrderID
					select new
					{
						od.OrderID,
						od.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
					};

				q.ToArray();

				Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1), "Join not optimized");

				var ts = q.GetTableSource();
				Assert.That(ts.Joins, Has.Count.EqualTo(1), "Join should be optimized");

#pragma warning disable CS0472 // comparison of int with null
				var qw = q.Where(v => v.OrderID1 != null);
#pragma warning restore CS0472
				qw.ToArray();
				Assert.That(qw.GetTableSource().Joins, Has.Count.EqualTo(2), "If LEFT join is used in where condition - it can not be optimized");

				var proj1 = q.Select(v => v.OrderID1);
				Assert.That(proj1.GetTableSource().Joins, Has.Count.EqualTo(1));

				var proj2 = qw.Select(v => v.OrderID1);
				Assert.That(proj2.GetTableSource().Joins, Has.Count.EqualTo(1));

				var proj3 = q.Select(v => v.OrderID);
				Assert.That(proj3.GetTableSource().Joins, Is.Empty, "All joins should be optimized");
			}
		}

		[Test]
		public void SelftJoinFail([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q1 = from od in db.Order
					join od2 in db.Order on od.EmployeeID equals od2.OrderID
					select od;

				Assert.That(q1.GetTableSource().Joins, Has.Count.EqualTo(1));

				var q2 = from od in db.Order
					join od2 in db.Order on od.OrderID equals od2.EmployeeID
					select od;

				Assert.That(q2.GetTableSource().Joins, Has.Count.EqualTo(1));

				var q3 = from od in db.Order
					join od2 in db.Order on new {ID1 = od.OrderID, ID2 = od.EmployeeID!.Value} equals new {ID1 = od2.EmployeeID!.Value, ID2 = od2.OrderID}
					select od;

				Assert.That(q3.GetTableSource().Joins, Has.Count.EqualTo(1));

			}
		}

		[Test]
		public void SelftJoinOptimized([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q1 = from od in db.Order
					join od2 in db.Order on od.OrderID equals od2.OrderID
					select od;

				Assert.That(q1.GetTableSource().Joins, Is.Empty);

				var q2 = from od in db.Order
					join od2 in db.Order on new {od.OrderID, od.EmployeeID} equals new {od2.OrderID, od2.EmployeeID}
					select od;

				Assert.That(q2.GetTableSource().Joins, Is.Empty);
			}
		}

		[Table(Name = "Person")]
		public class PersonEntity
		{
			[Column]
			[PrimaryKey]
			[Identity]
			public int Id { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Table(Name = "Adress")]
		public class AdressEntity
		{
			[Column]
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int PersonId { get; set; }
		}

		[Test]
		public void JoinWithHint([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query = from p in db.GetTable<PersonEntity>().With("READUNCOMMITTED")
						 join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 on p.Id equals a.Id //PK column
						 select p;

				Assert.That(query.GetTableSource().Joins, Has.Count.EqualTo(1));
			}
		}

		[ActiveIssue(2452, Details = "Enable when new hints design will be ready.")]
		[Test]
		public void SelfJoinWithHint([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query = from p in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 on p.Id equals a.Id //PK column
						 select p;

				Assert.That(query.GetTableSource().Joins, Is.Empty);
			}
		}

		[Test]
		public void SelfJoinWithDifferentHint([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query =
					from p in db.GetTable<AdressEntity>().With("NOLOCK")
					join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
					on p.Id equals a.Id //PK column
					select p;

				Assert.That(query.GetTableSource().Joins, Has.Count.EqualTo(1));
			}
		}
	}
}
