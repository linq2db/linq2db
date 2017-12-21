using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Linq;
	using LinqToDB.SqlQuery;

	using Model;
	using System.Linq.Expressions;

	[TestFixture]
	public class JoinOptimizeTests : TestBase
	{
		SelectQuery GetSelectQuery<T>(IQueryable<T> query)
		{
			var eq = (IExpressionQuery)query;
			var expression = eq.Expression;
			var info = Query<T>.GetQuery(eq.DataContext, ref expression);
			return (SelectQuery)info.Queries.Single().Statement;
		}

		SqlSearchCondition GetWhere<T>(IQueryable<T> query)
		{
			return GetSelectQuery(query).Where.SearchCondition;
		}

		SqlSearchCondition GetWhere(SelectQuery selectQuery)
		{
			return selectQuery.Where.SearchCondition;
		}

		SqlTableSource GeTableSource(SelectQuery selectQuery)
		{
			return selectQuery.From.Tables.Single();
		}

		SqlTableSource GeTableSource<T>(IQueryable<T> query)
		{
			return GetSelectQuery(query).From.Tables.Single();
		}

		[Test, NorthwindDataContext]
		public void InnerJoinToSelf(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				//Configuration.Linq.OptimizeJoins = false;

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

				Assert.AreEqual(q, q2);

				var ts = GeTableSource(q);
				Assert.AreEqual(1, ts.Joins.Count);
			}
		}
		[Test, NorthwindDataContext]
		public void InnerJoin(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				//Configuration.Linq.OptimizeJoins = false;

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


				var sql = q.ToString();

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

				Assert.AreEqual(q, q2);

				Assert.AreEqual(1, GeTableSource(q).Joins.Count);

				var proj1 = q.Select(v => v.OrderID);
				Console.WriteLine(proj1.ToString());
				var sq1 = GetSelectQuery(proj1);
				Assert.AreEqual(1, GeTableSource(sq1).Joins.Count);
				Assert.AreEqual(0, GetWhere(sq1).Conditions.Count);

				var proj2 = q.Select(v => v.OrderDate);
				Console.WriteLine(proj2.ToString());
				var sq2 = GetSelectQuery(proj2);
				Assert.AreEqual(1, GeTableSource(sq2).Joins.Count);
				Assert.AreEqual(0, GetWhere(sq2).Conditions.Count);
			}
		}


		[Test, NorthwindDataContext]
		public void InnerJoinFalse(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				//Configuration.Linq.OptimizeJoins = false;

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

				var str = q.ToString();

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

				Assert.AreEqual(q, q2);

				var ts = GeTableSource(q);
				Assert.AreEqual(1, ts.Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void LeftJoin(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				//Configuration.Linq.OptimizeJoins = false;

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

				var str = q.ToString();

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

				Assert.AreEqual(q, q2);

				var ts = GeTableSource(q);
				Assert.AreEqual(2, ts.Joins.Count(j => j.JoinType == JoinType.Inner));
				Assert.AreEqual(3, ts.Joins.Count(j => j.JoinType == JoinType.Left));
			}
		}


		[Test, NorthwindDataContext]
		public void InnerJoin1(string context)
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

				Assert.AreEqual(1, GeTableSource(q).Joins.Count);

				var proj1 = q.Select(v => v.OrderID);
				Assert.AreEqual(1, GeTableSource(proj1).Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void InnerJoinSubquery(string context)
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

				var ts = GeTableSource(q2);
				Assert.AreEqual(1, ((SelectQuery)ts.Source).From.Tables.Single().Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void InnerJoinMixKeys(string context)
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

				var str = q.ToString();

				Assert.AreEqual(1, GeTableSource(q).Joins.Count);

				var proj1 = q.Select(v => v.OrderID);
				Assert.AreEqual(1, GeTableSource(proj1).Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void InnerAndLeftMixed(string context)
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

				Console.WriteLine(q.ToString());
				Assert.AreEqual(1, GeTableSource(q).Joins.Count);

				var proj1 = q.Select(v => v.OrderID);
				Console.WriteLine(proj1.ToString());
				Assert.AreEqual(1, GeTableSource(proj1).Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void InnerJoin2(string context)
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

				var sql = GetSelectQuery(q);
				Assert.AreEqual(1, GeTableSource(sql).Joins.Count);
				Assert.AreEqual(2, GeTableSource(sql).Joins.First().Condition.Conditions.Count);
				Assert.AreEqual(0, GetWhere(sql).Conditions.Count);

				var proj1 = q.Select(v => v.OrderID);
				var sql1 = GetSelectQuery(proj1);
				Assert.AreEqual(1, GeTableSource(sql1).Joins.Count);
				Assert.AreEqual(0, GetWhere(sql1).Conditions.Count);
			}
		}


		[Test, NorthwindDataContext]
		public void InnerJoin3(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on od.OrderID equals o2.OrderID
					join o3 in db.Order on od.OrderID equals o3.OrderID
					where o1.OrderDate == DateTime.Now || o2.OrderDate < DateTime.Now && o3.EmployeeID != null
					orderby od.OrderID
					select new
					{
						od.OrderID,
						o1.OrderDate,
						OrderID1 = o1.OrderID,
						OrderID2 = o2.OrderID,
						OrderID3 = o3.OrderID,
					};

				Assert.AreEqual(1, GeTableSource(q).Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void LeftJoin1(string context)
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

				var ts = GeTableSource(q);
				Assert.AreEqual(1, ts.Joins.Count(j => j.JoinType == JoinType.Left));
			}
		}

		[Test, NorthwindDataContext]
		public void LeftJoin2(string context)
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

				var ts = GeTableSource(q);
				Assert.AreEqual(2, ts.Joins.Count(j => j.JoinType == JoinType.Left));
			}
		}

		[Test, NorthwindDataContext]
		public void LeftJoinProjection(string context)
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

				Assert.AreEqual(1, GeTableSource(q).Joins.Count, "Join not optimized");

				var qw = q.Where(v => v.OrderDate != null);
				Assert.AreEqual(2, GeTableSource(qw).Joins.Count, "If LEFT join is used in where condition - it can not be optimized");

				var proj1 = q.Select(v => v.OrderID1);
				Assert.AreEqual(1, GeTableSource(proj1).Joins.Count);

				var proj2 = qw.Select(v => v.OrderID1);
				Assert.AreEqual(1, GeTableSource(proj2).Joins.Count);

				var proj3 = q.Select(v => v.OrderID);
				Assert.AreEqual(0, GeTableSource(proj3).Joins.Count, "All joins should be optimized");
			}
		}

		[Test, NorthwindDataContext]
		public void LeftJoinProjectionSubquery(string context)
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

				Assert.AreEqual(1, GeTableSource(q).Joins.Count, "Join not optimized");

				var ts = GeTableSource(q);
				Assert.AreEqual(1, ((SelectQuery)ts.Source).From.Tables.Single().Joins.Count, "Join should be optimized");

#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
				var qw = q.Where(v => v.OrderID1 != null);
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
				var str = qw.ToString();
				Assert.AreEqual(2, GeTableSource(qw).Joins.Count, "If LEFT join is used in where condition - it can not be optimized");

				var proj1 = q.Select(v => v.OrderID1);
				Assert.AreEqual(1, GeTableSource(proj1).Joins.Count);

				var proj2 = qw.Select(v => v.OrderID1);
				Assert.AreEqual(1, GeTableSource(proj2).Joins.Count);

				var proj3 = q.Select(v => v.OrderID);
				Assert.AreEqual(0, GeTableSource(proj3).Joins.Count, "All joins should be optimized");
			}
		}

		[Test, NorthwindDataContext]
		public void SelftJoinFail(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q1 = from od in db.Order
					join od2 in db.Order on od.EmployeeID equals od2.OrderID
					select od;

				Assert.AreEqual(1, GeTableSource(q1).Joins.Count);

				var q2 = from od in db.Order
					join od2 in db.Order on od.OrderID equals od2.EmployeeID
					select od;

				Assert.AreEqual(1, GeTableSource(q2).Joins.Count);

				var q3 = from od in db.Order
					join od2 in db.Order on new {ID1 = od.OrderID, ID2 = od.EmployeeID.Value} equals new {ID1 = od2.EmployeeID.Value, ID2 = od2.OrderID}
					select od;

				Assert.AreEqual(1, GeTableSource(q3).Joins.Count);

			}
		}

		[Test, NorthwindDataContext]
		public void SelftJoinOptimized(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q1 = from od in db.Order
					join od2 in db.Order on od.OrderID equals od2.OrderID
					select od;

				Assert.AreEqual(0, GeTableSource(q1).Joins.Count);

				var q2 = from od in db.Order
					join od2 in db.Order on new {od.OrderID, od.EmployeeID} equals new {od2.OrderID, od2.EmployeeID}
					select od;

				Assert.AreEqual(0, GeTableSource(q2).Joins.Count);
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
			public string Name { get; set; }
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

		[Test, NorthwindDataContext]
		public void JoinWithHint(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query = from p in db.GetTable<PersonEntity>().With("READUNCOMMITTED")
						 join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 on p.Id equals a.Id //PK column
						 select p;

				Assert.AreEqual(1, GeTableSource(query).Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void SelfJoinWithHint(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query = from p in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 on p.Id equals a.Id //PK column
						 select p;

				Assert.AreEqual(0, GeTableSource(query).Joins.Count);
			}
		}

		[Test, NorthwindDataContext]
		public void SelfJoinWithDifferentHint(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query = from p in db.GetTable<AdressEntity>().With("NOLOCK")
						 join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 on p.Id equals a.Id //PK column
						 select p;

				Assert.AreEqual(1, GeTableSource(query).Joins.Count);
			}
		}

	}
}
