using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Linq;
	using LinqToDB.SqlQuery;

	using Model;

	[TestFixture]
	public class JoinOptimizeTests : TestBase
	{
		SelectQuery.TableSource GeTableSource<T>(IQueryable<T> query)
		{
			var eq = (IExpressionQuery)query;
			var info = Query<T>.GetQuery(eq.DataContextInfo, eq.Expression);
			return info.Queries.Single().SelectQuery.From.Tables.Single();
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
						OrderID1 = o1.OrderID, 
						OrderID2 = o2.OrderID, 
						OrderID3 = o3.OrderID, 
					};

				Assert.AreEqual(q, q2);

				var ts = GeTableSource(q);
				Assert.AreEqual(1, ts.Joins.Count);
				Assert.AreEqual(2, ts.Joins.First().Condition.Conditions.Count);
				Assert.AreEqual(true, ts.Joins.First().Condition.Conditions.All(c => !c.IsOr));
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
				Assert.AreEqual(3, ts.Joins.Count);
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
				Assert.AreEqual(2, ts.Joins.Count(j => j.JoinType == SelectQuery.JoinType.Inner));
				Assert.AreEqual(3, ts.Joins.Count(j => j.JoinType == SelectQuery.JoinType.Left));
			}
		}

	}
}