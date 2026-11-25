using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class JoinOptimizeTests : TestBase
	{
		[Test]
		public void InnerJoinToSelf([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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
		[Test]
		public void InnerJoin([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(q2, Is.EqualTo(q));

				Assert.That(q.GetTableSource().Joins, Has.Count.EqualTo(1));
			}

			var proj1 = q.Select(v => v.OrderID);
			proj1.ToArray();
			var sq1 = proj1.GetSelectQuery();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sq1.GetTableSource().Joins, Has.Count.EqualTo(1));
				Assert.That(sq1.GetWhere().Predicates, Is.Empty);
			}

			var proj2 = q.Select(v => v.OrderDate);
			proj2.ToArray();
			var sq2 = proj2.GetSelectQuery();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sq2.GetTableSource().Joins, Has.Count.EqualTo(1));
				Assert.That(sq2.GetWhere().Predicates, Is.Empty);
			}
		}

		[Test]
		public void InnerJoinFalse([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void LeftJoin([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

			AssertQuery(q);

			var ts = q.GetTableSource();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ts.Joins.Count(j => j.JoinType == JoinType.Inner), Is.EqualTo(2));
				Assert.That(ts.Joins.Count(j => j.JoinType == JoinType.Left), Is.EqualTo(3));
			}
		}

		[Test]
		public void InnerJoin1([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void InnerJoinSubquery([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void InnerJoinMixKeys([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void InnerAndLeftMixed([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void InnerJoin2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sql.GetTableSource().Joins.First().Condition.Predicates, Has.Count.EqualTo(2));
				Assert.That(sql.GetWhere().Predicates, Is.Empty);
			}

			var proj1 = q.Select(v => v.OrderID);
			var sql1 = proj1.GetSelectQuery();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sql1.GetTableSource().Joins, Has.Count.EqualTo(1));
				Assert.That(sql1.GetWhere().Predicates, Is.Empty);
			}
		}

		[Test]
		public void InnerJoin3([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var q = from od in db.OrderDetail
					join o1 in db.Order on od.OrderID equals o1.OrderID
					join o2 in db.Order on od.OrderID equals o2.OrderID
					join o3 in db.Order on od.OrderID equals o3.OrderID
					where o1.OrderDate == TestData.DateTime || (o2.OrderDate < TestData.DateTime && o3.EmployeeID != null)
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

		[Test]
		public void LeftJoin1([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void LeftJoin2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void LeftJoinProjection([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void LeftJoinProjectionSubquery([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void SelftJoinFail([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
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

		[Test]
		public void SelftJoinOptimized([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var q1 = from od in db.Order
					 join od2 in db.Order on od.OrderID equals od2.OrderID
					 select od;

			Assert.That(q1.GetTableSource().Joins, Is.Empty);

			var q2 = from od in db.Order
					 join od2 in db.Order on new {od.OrderID, od.EmployeeID} equals new {od2.OrderID, od2.EmployeeID}
					 select od;

			Assert.That(q2.GetTableSource().Joins, Is.Empty);
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
			using var db = new NorthwindDB(context);
			var query = from p in db.GetTable<PersonEntity>().With("READUNCOMMITTED")
						join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 on p.Id equals a.Id //PK column
						select p;

			Assert.That(query.GetTableSource().Joins, Has.Count.EqualTo(1));
		}

		[ActiveIssue(2452, ErrorMessage = "Assert.That(query.GetTableSource().Joins, Is.Empty)", Details = "Enable when new hints design will be ready.")]
		[Test]
		public void SelfJoinWithHint([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var query = from p in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
						 on p.Id equals a.Id //PK column
						select p;

			Assert.That(query.GetTableSource().Joins, Is.Empty);
		}

		[Test]
		public void SelfJoinWithDifferentHint([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var query =
					from p in db.GetTable<AdressEntity>().With("NOLOCK")
					join a in db.GetTable<AdressEntity>().With("READUNCOMMITTED")
					on p.Id equals a.Id //PK column
					select p;

			Assert.That(query.GetTableSource().Joins, Has.Count.EqualTo(1));
		}

		#region Isue 4790
		class Issue4790Client
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		class Issue4790ClientWithKey
		{
			[PrimaryKey]
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		class Issue4790Bill : IIssue4790Bill
		{
			public int  Id       { get; set; }
			public int? IdClient { get; set; }

			[Association(ThisKey = nameof(IdClient), OtherKey = nameof(Client.Id), CanBeNull = false)]
			public Issue4790Client Client { get; set; } = null!;

			[Association(ThisKey = nameof(IdClient), OtherKey = nameof(Issue4790ClientWithKey.Id), CanBeNull = false)]
			public Issue4790ClientWithKey KeyedClient { get; set; } = null!;
		}

		interface IIssue4790Bill
		{
			int  Id       { get; set; }
			int? IdClient { get; set; }

			public Issue4790Client        Client      { get; set; }
			public Issue4790ClientWithKey KeyedClient { get; set; }
		}

		class Issue4790Position : IIssue4790Position
		{
			public int Id { get; set; }
			public int? IdBill { get; set; }

			[Association(ThisKey = nameof(IdBill), OtherKey = nameof(Issue4790Bill.Id), CanBeNull = false)]
			public Issue4790Bill Bill { get; set; } = null!;
		}

		interface IIssue4790Position
		{
			int Id { get; set; }
			int? IdBill { get; set; }

			public Issue4790Bill Bill { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void Issue4790Test_Association_NoKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue4790Client>();
			using var t2 = db.CreateLocalTable<Issue4790Bill>();

			var query = from bill in Filter(db.GetTable<Issue4790Bill>(), "Abc") select new { bill.Client.Name };

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(1));

			static IQueryable<TBill> Filter<TBill>(IQueryable<TBill> query, string clientName)
				where TBill : IIssue4790Bill
			{
				return query.Where(b => b.Client.Name == clientName);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void Issue4790Test_Join_NoKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue4790Client>();
			using var t2 = db.CreateLocalTable<Issue4790Bill>();

			var query = from bill in Filter(db, db.GetTable<Issue4790Bill>(), "Abc")
						join client in t1
						on bill.IdClient equals client.Id
						select client.Name;

			query.ToArray();

			var sql = query.GetSelectQuery();
			// we cannot remove explicit join if we don't known cardinality
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(2));

			static IQueryable<TBill> Filter<TBill>(IDataContext db, IQueryable< TBill> query, string clientName)
				where TBill : IIssue4790Bill
			{
				return from bill in query
					   join client in db.GetTable<Issue4790Client>()
					   on bill.IdClient equals client.Id
					   where client.Name == clientName
					   select bill;
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void Issue4790Test_Association_WithKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue4790ClientWithKey>();
			using var t2 = db.CreateLocalTable<Issue4790Bill>();

			var query = from bill in Filter(db.GetTable<Issue4790Bill>(), "Abc") select new { bill.KeyedClient.Name };

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(1));

			static IQueryable<TBill> Filter<TBill>(IQueryable<TBill> query, string clientName)
				where TBill : IIssue4790Bill
			{
				return query.Where(b => b.KeyedClient.Name == clientName);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void Issue4790Test_Join_WithKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue4790ClientWithKey>();
			using var t2 = db.CreateLocalTable<Issue4790Bill>();

			var query = from bill in Filter(db, db.GetTable<Issue4790Bill>(), "Abc")
						join client in t1
						on bill.IdClient equals client.Id
						select client.Name;

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(1));

			static IQueryable<TBill> Filter<TBill>(IDataContext db, IQueryable<TBill> query, string clientName)
				where TBill : IIssue4790Bill
			{
				return from bill in query
					   join client in db.GetTable<Issue4790ClientWithKey>()
					   on bill.IdClient equals client.Id
					   where client.Name == clientName
					   select bill;
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void Issue4790Test_Association_Nested([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue4790Client>();
			using var t2 = db.CreateLocalTable<Issue4790Bill>();
			using var t3 = db.CreateLocalTable<Issue4790Position>();

			var query = from position in Filter(db.GetTable<Issue4790Position>(), "Abc") select new { position.Bill.Client.Name };

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(2));

			static IQueryable<TPosition> Filter<TPosition>(IQueryable<TPosition> query, string clientName)
				where TPosition : IIssue4790Position
			{
				return query.Where(p => p.Bill.Client.Name == clientName);
			}
		}
		#endregion

		#region Left join to grouping with computed key

		[Table]
		sealed class InventoryItem
		{
			[Column(CanBeNull = false, IsPrimaryKey = true)] public string No { get; set; } = null!;
		}

		[Table]
		sealed class StockEntry
		{
			[Column(IsPrimaryKey = true)] public int    Id       { get; set; }
			[Column]                      public string Code     { get; set; } = null!;
			[Column]                      public int    Quantity { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5799")]
		public void LeftJoinToGroupingWithComputedKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable([new InventoryItem { No = "ITM" }]);
			using var t2 = db.CreateLocalTable([new StockEntry { Id = 1, Code = "ITM-A", Quantity = 5 }]);

			// LEFT JOIN a grouping whose key is a computed expression, not a plain column
			var query =
				from item in t1
				from g in t2.GroupBy(s => s.Code.Substring(0, 3)).LeftJoin(g => g.Key == item.No)
				select new { item.No, Qty = g.Sum(s => s.Quantity) };

			var rows = query.ToList();

			rows.Count.ShouldBe(1);
			rows[0].No.ShouldBe("ITM");
			rows[0].Qty.ShouldBe(5);
			db.LastQuery!.ShouldContain("LEFT JOIN");

			// Count() drops the joined columns from the projection, which engages left-join removal.
			// The grouping key is unique, so the join preserves cardinality and can be dropped.
			query.Count().ShouldBe(1);
			db.LastQuery!.ShouldNotContain("LEFT JOIN");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5799")]
		public void LeftJoinToDistinctWithComputedKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable([new InventoryItem { No = "ITM" }]);
			using var t2 = db.CreateLocalTable([new StockEntry { Id = 1, Code = "ITM-A", Quantity = 5 }]);

			// DISTINCT contributes its projected column expressions as a keyset, so a computed
			// projection puts a non-reducible element there just as a GROUP BY item does.
			var distinct = t2.Select(s => s.Code.Substring(0, 3)).Distinct();

			var query =
				from item in t1
				from d in distinct.LeftJoin(d => d == item.No)
				select new { item.No, Key = d };

			var rows = query.ToList();

			rows.Count.ShouldBe(1);
			rows[0].No.ShouldBe("ITM");
			rows[0].Key.ShouldBe("ITM");

			// The keyset spans the whole DISTINCT projection and the join matches only part of it,
			// so the join must be kept - resolving that must not throw.
			query.Count().ShouldBe(1);
			db.LastQuery!.ShouldContain("LEFT JOIN");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5799")]
		public void LeftJoinToSourceWithComputedUniqueKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable([new InventoryItem { No = "ITM" }]);
			using var t2 = db.CreateLocalTable([new StockEntry { Id = 1, Code = "ITM", Quantity = 5 }]);

			// HasUniqueKey writes the raw selector into the source's unique keys, reaching the same
			// keyset walk without any subquery at all.
			var query =
				from item in t1
				from s in t2.HasUniqueKey(s => s.Code.Substring(0, 3)).LeftJoin(s => s.Code == item.No)
				select new { item.No, Qty = (int?)s.Quantity };

			var rows = query.ToList();

			rows.Count.ShouldBe(1);
			rows[0].Qty.ShouldBe(5);

			query.Count().ShouldBe(1);
		}

		#endregion

		#region Outer join projection hoisting

		// A conditional projection of an outer-joined source reads NULL for an outer row without a match. Lifting the
		// projection out of the joined subquery evaluates it over the NULL-extended row instead, where `Value1 IS NULL`
		// holds, so the last parent (no ParentID + 1) reads "none".
		[Test]
		public void OuterJoinedConditionalProjectionReadsNullWithoutMatch([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var plain =
				from p in db.Parent
				from x in db.Parent
					.Where(q => q.ParentID == p.ParentID + 1)
					.Select(q => q.Value1 == null ? "none" : null)
					.DefaultIfEmpty()
				select new { p.ParentID, X = x };

			AssertQuery(plain);

			var subQuery =
				from p in db.Parent
				from x in db.Parent
					.Where(q => q.ParentID == p.ParentID + 1)
					.Select(q => q.Value1 == null ? "none" : null)
					.AsSubQuery()
					.DefaultIfEmpty()
				select new { p.ParentID, X = x };

			AssertQuery(subQuery);
		}

		// A RIGHT or FULL join inside the outer-joined subquery yields rows without its main source, so the NULL
		// marker of the lifted projection cannot come from the main source alone: a parent without children would
		// read NULL instead of its value. The range condition has no `=` to take the marker from, so it comes from
		// the sources of the subquery.
		[Test]
		public void OuterJoinedSubqueryWithRightJoinKeepsMatchedValue([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var sub = db.Child
				.RightJoin(db.Parent, (c, p) => c.ParentID == p.ParentID, (c, p) => new { p.ParentID, Flag = p.Value1 > 4 ? "big" : null });

			var expected =
				from x in Parent
				from c in Child
					.Where(c => c.ParentID == x.ParentID)
					.Select(c => (Child?)c)
					.DefaultIfEmpty()
				select new { x.ParentID, Flag = x.Value1 > 4 ? "big" : null };

			var query =
				from x in db.Parent
				from s in sub
					.Where(s => s.ParentID == x.ParentID)
					.DefaultIfEmpty()
				select new { x.ParentID, s.Flag };

			AreEqual(expected, query);

			var leftJoinQuery =
				from x in db.Parent
				from s in sub.LeftJoin(s => s.ParentID == x.ParentID)
				select new { x.ParentID, s.Flag };

			AreEqual(expected, leftJoinQuery);

			var rangeQuery =
				from x in db.Parent
				from s in sub
					.Where(s => s.ParentID >= x.ParentID && s.ParentID <= x.ParentID)
					.DefaultIfEmpty()
				select new { x.ParentID, s.Flag };

			AreEqual(expected, rangeQuery);
		}

		[Test]
		public void OuterJoinedSubqueryWithFullJoinKeepsMatchedValue([DataSources(TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllMySql, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var sub = db.Child
				.FullJoin(db.Parent, (c, p) => c.ParentID == p.ParentID, (c, p) => new { ParentID = (int?)p.ParentID, Flag = p.Value1 > 4 ? "big" : null });

			var expected =
				from x in Parent
				from c in Child
					.Where(c => c.ParentID == x.ParentID)
					.Select(c => (Child?)c)
					.DefaultIfEmpty()
				select new { x.ParentID, Flag = x.Value1 > 4 ? "big" : null };

			var query =
				from x in db.Parent
				from s in sub
					.Where(s => s.ParentID == x.ParentID)
					.DefaultIfEmpty()
				select new { x.ParentID, s.Flag };

			AreEqual(expected, query);

			var rangeQuery =
				from x in db.Parent
				from s in sub
					.Where(s => s.ParentID >= x.ParentID && s.ParentID <= x.ParentID)
					.DefaultIfEmpty()
				select new { x.ParentID, s.Flag };

			AreEqual(expected, rangeQuery);
		}

		// The subquery side of a RIGHT join keeps every row: a lifted projection reads its value also for a row the ON
		// condition does not match, here parents 2 and 4 with NULL in the compared column.
		[Test]
		public void RightJoinedSubqueryKeepsUnmatchedValue([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var sub = db.Parent
				.Select(p => new { p.ParentID, p.Value1, Flag = p.Value1 == null ? "none" : null });

			var query =
				from x in db.Parent
				from s in sub.RightJoin(s => s.Value1 == x.ParentID)
				select new { s.ParentID, s.Flag };

			var expected =
				from p in Parent
				select new { p.ParentID, Flag = p.Value1 == null ? "none" : null };

			AreEqual(expected, query);
		}

		[Table]
		sealed class NullableOnlyRow
		{
			[Column] public int? A { get; set; }
			[Column] public int? B { get; set; }
		}

		// The joined source has no non-nullable column to mark a matched row. The projection references a sibling
		// source, so it cannot stay in a derived table under a LEFT JOIN, and it still reads NULL for an outer row
		// without a match (ChildID 22).
		[Test]
		public void OuterJoinedSiblingProjectionWithoutNotNullColumn([DataSources] string context)
		{
			NullableOnlyRow[] rows = [new() { A = 11, B = null }, new() { A = 21, B = 1 }];

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(rows);

			var query =
				from c in db.Child
				where c.ChildID == 11 || c.ChildID == 21 || c.ChildID == 22
				from n in t
					.Where(n => n.A == c.ChildID)
					.Select(n => n.B == null ? (int?)c.ParentID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, N = n };

			var expected =
				from c in Child
				where c.ChildID == 11 || c.ChildID == 21 || c.ChildID == 22
				from n in rows
					.Where(n => n.A == c.ChildID)
					.Select(n => n.B == null ? (int?)c.ParentID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, N = n };

			AreEqual(expected, query);
		}

		// No column marks a matched row: the source has no non-nullable one and the range condition has no `=`. The
		// FROM moves into a subquery with a constant marker. Access does not NULL-extend it and lifts the column
		// unguarded; Oracle 11 merges the subquery over the table and loses it. Both read the sibling value, as on master.
		[Test]
		public void OuterJoinedSiblingProjectionWithoutAnyMarker([DataSources(TestProvName.AllAccess, TestProvName.AllOracle11)] string context)
		{
			NullableOnlyRow[] rows = [new() { A = 11, B = null }, new() { A = 21, B = 1 }];

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(rows);

			var query =
				from c in db.Child
				where c.ChildID == 11 || c.ChildID == 21 || c.ChildID == 22
				from n in t
					.Where(n => n.A >= c.ChildID && n.A <= c.ChildID)
					.Select(n => n.B == null ? (int?)c.ParentID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, N = n };

			var expected =
				from c in Child
				where c.ChildID == 11 || c.ChildID == 21 || c.ChildID == 22
				from n in rows
					.Where(n => n.A == c.ChildID)
					.Select(n => n.B == null ? (int?)c.ParentID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, N = n };

			AreEqual(expected, query);
		}

		// With `=` in the ON condition the marker is the compared CTE column, without it a non-nullable CTE column.
		[Test]
		public void OuterJoinedSiblingProjectionOverCte([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte = db.Child
				.Where(c => c.ChildID > 0)
				.AsCte();

			var expected =
				from p in Parent
				where p.ParentID == 1 || p.ParentID == 4 || p.ParentID == 5
				from x in Child
					.Where(cc => cc.ParentID == p.ParentID)
					.Select(cc => cc.ChildID > 40 ? (int?)p.Value1 : p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, X = x };

			var query =
				from p in db.Parent
				where p.ParentID == 1 || p.ParentID == 4 || p.ParentID == 5
				from x in cte
					.Where(cc => cc.ParentID == p.ParentID)
					.Select(cc => cc.ChildID > 40 ? (int?)p.Value1 : p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, X = x };

			AreEqual(expected, query);

			var rangeQuery =
				from p in db.Parent
				where p.ParentID == 1 || p.ParentID == 4 || p.ParentID == 5
				from x in cte
					.Where(cc => cc.ParentID >= p.ParentID && cc.ParentID <= p.ParentID)
					.Select(cc => cc.ChildID > 40 ? (int?)p.Value1 : p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, X = x };

			AreEqual(expected, rangeQuery);

			// ChildID comes from a source the CTE body outer-joins: it is NULL on a row of the CTE
			var joinedCte =
				(
					from pp in db.Parent
					from cc in db.Child.Where(cc => cc.ParentID == pp.ParentID).DefaultIfEmpty()
					select new { ChildID = (int?)cc.ChildID, pp.Value1 }
				)
				.AsCte();

			var joinedExpected =
				from p in Parent
				where p.ParentID == 1 || p.ParentID == 4 || p.ParentID == 5
				from x in
					(
						from pp in Parent
						from cc in Child.Where(cc => cc.ParentID == pp.ParentID).DefaultIfEmpty()
						select new { ChildID = cc == null ? null : (int?)cc.ChildID, pp.Value1 }
					)
					.Where(cc => cc.Value1 == p.ParentID)
					.Select(cc => cc.ChildID > 40 ? (int?)p.Value1 : p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, X = x };

			var joinedQuery =
				from p in db.Parent
				where p.ParentID == 1 || p.ParentID == 4 || p.ParentID == 5
				from x in joinedCte
					.Where(cc => cc.Value1 >= p.ParentID && cc.Value1 <= p.ParentID)
					.Select(cc => cc.ChildID > 40 ? (int?)p.Value1 : p.ParentID)
					.DefaultIfEmpty()
				select new { p.ParentID, X = x };

			AreEqual(joinedExpected, joinedQuery);
		}

		[Test]
		public void OuterJoinedSiblingProjectionOverConcat([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var union = db.Parent
				.Where(p => p.ParentID < 3)
				.Concat(db.Parent.Where(p => p.ParentID > 5));

			var query =
				from c in db.Child
				where c.ParentID == 1 || c.ParentID == 2 || c.ParentID == 3 || c.ParentID == 6
				from v in union
					.Where(q => q.ParentID == c.ParentID)
					.Select(q => q.Value1 == null ? (int?)c.ChildID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			var expected =
				from c in Child
				where c.ParentID == 1 || c.ParentID == 2 || c.ParentID == 3 || c.ParentID == 6
				from v in Parent
					.Where(q => (q.ParentID < 3 || q.ParentID > 5) && q.ParentID == c.ParentID)
					.Select(q => q.Value1 == null ? (int?)c.ChildID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AreEqual(expected, query);

			var rangeQuery =
				from c in db.Child
				where c.ParentID == 1 || c.ParentID == 2 || c.ParentID == 3 || c.ParentID == 6
				from v in union
					.Where(q => q.ParentID >= c.ParentID && q.ParentID <= c.ParentID)
					.Select(q => q.Value1 == null ? (int?)c.ChildID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AreEqual(expected, rangeQuery);
		}

		// Access rejects the FROM-less selects of a VALUES source inside a derived table
		[Test]
		public void OuterJoinedSiblingProjectionOverValues([DataSources(TestProvName.AllAccess)] string context)
		{
			NullableOnlyRow[] rows = [new() { A = 11, B = null }, new() { A = 21, B = 1 }];

			using var db = GetDataContext(context);

			var values = rows.AsQueryable(db);

			var expected =
				from c in Child
				where c.ChildID == 11 || c.ChildID == 21 || c.ChildID == 22
				from n in rows
					.Where(n => n.A == c.ChildID)
					.Select(n => n.B == null ? (int?)c.ParentID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, N = n };

			var query =
				from c in db.Child
				where c.ChildID == 11 || c.ChildID == 21 || c.ChildID == 22
				from n in values
					.Where(n => n.A == c.ChildID)
					.Select(n => n.B == null ? (int?)c.ParentID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, N = n };

			AreEqual(expected, query);

			var rangeQuery =
				from c in db.Child
				where c.ChildID == 11 || c.ChildID == 21 || c.ChildID == 22
				from n in values
					.Where(n => n.A >= c.ChildID && n.A <= c.ChildID)
					.Select(n => n.B == null ? (int?)c.ParentID : null)
					.DefaultIfEmpty()
				select new { c.ChildID, N = n };

			AreEqual(expected, rangeQuery);
		}

		#endregion
	}
}
