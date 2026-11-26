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

		// The builder builds the guard before any provider code runs, so SQLite covers it; .LinqService takes the guarded SQL
		// through the remote path. Access and Oracle 11 still read the enclosing value on these shapes: Oracle 11 does not
		// NULL-extend the constant marker the guard tests, and Access keeps its old markers (see the Access-only test below).
		[Test]
		public void OuterSourceProjectionReadsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// The outer-joined sequence projects a value of an enclosing source. The value does not come from the joined
			// query, so the join does not NULL-extend it: children 61-66 and 77 have no grandchildren and read null.
			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p.Value1)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void NonNullableOuterSourceProjectionReadsDefaultWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// A non-nullable outer value must not serve as the row-exists marker either
			var nullable =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => (int?)p.ParentID)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AssertQuery(nullable);

			var notNullable =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p.ParentID)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AssertQuery(notNullable);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void NonNullableOuterSourceProjectionReadsExplicitDefaultWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// An explicit default is chosen by the row-exists marker alone, so a non-nullable outer value must not be that
			// marker: children 61-66 and 77 have no grandchildren and read -1
			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p.ParentID)
					.DefaultIfEmpty(-1)
				select new { c.ChildID, V = v };

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterAndOwnMembersReadNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => new { P = p.Value1, G = g.GrandChildID })
					.DefaultIfEmpty()
				select new
				{
					c.ChildID,
					v.P,
					v.G,
				};

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionFiltersAsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p.Value1)
					.DefaultIfEmpty()
				where v == null
				select c.ChildID;

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionCountsAsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => (int?)p.ParentID)
					.DefaultIfEmpty()
				group v by c.ParentID into gr
				select new
				{
					gr.Key,
					Missing = gr.Count(x => x == null),
				};

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionGroupsAsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => (int?)p.ParentID)
					.DefaultIfEmpty()
				group c by v into gr
				select new
				{
					gr.Key,
					Count = gr.Count(),
				};

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterEntityProjectionIsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p)
					.DefaultIfEmpty()
				select new
				{
					c.ChildID,
					IsMissing = v == null,
				};

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision. Access and Oracle 11 fail differently here:
		// they cannot convert DefaultIfEmpty().Max() over this projection to SQL, before or after the fix
		[Test]
		public void OuterSourceProjectionAggregatesOverEmptySequence([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var oneRow =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				select new
				{
					c.ChildID,
					Max = db.GrandChild
						.Where(g => g.ChildID == c.ChildID)
						.Select(g => (int?)p.ParentID)
						.DefaultIfEmpty()
						.Max(),
				};

			AssertQuery(oneRow);

			var grouped =
				from c in db.Child
				join g in db.GrandChild on c.ChildID equals g.ChildID into gj
				select new
				{
					c.ChildID,
					Max = gj
						.Select(x => (int?)c.ParentID)
						.DefaultIfEmpty()
						.Max(),
				};

			AssertQuery(grouped);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionAggregatesOverExplicitDefault([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// DefaultIfEmpty(x) in a projection builds a one-row LEFT JOIN: one result per child, -1 for the children without
			// grandchildren
			var nullable =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				select new
				{
					c.ChildID,
					Max = db.GrandChild
						.Where(g => g.ChildID == c.ChildID)
						.Select(g => (int?)p.ParentID)
						.DefaultIfEmpty(-1)
						.Max(),
				};

			AssertQuery(nullable);

			var notNullable =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				select new
				{
					c.ChildID,
					Max = db.GrandChild
						.Where(g => g.ChildID == c.ChildID)
						.Select(g => p.ParentID)
						.DefaultIfEmpty(-1)
						.Max(),
				};

			AssertQuery(notNullable);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionThroughAssociationReadsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in p.Children
				from v in c.GrandChildren
					.Select(g => p.Value1)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			// AssertQuery cannot follow the associations on its expected side, so the expected rows join the model collections
			var expected =
				from p in Parent
				from c in Child
				where c.ParentID == p.ParentID
				from v in GrandChild
					.Where(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID)
					.Select(g => p.Value1)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AreEqual(expected, query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionThroughGroupJoinReadsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from c in db.Child
				join g in db.GrandChild on c.ChildID equals g.ChildID into gj
				from v in gj
					.Select(x => (int?)c.ParentID)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionThroughLeftJoinReadsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Select(g => new { g.ChildID, P = p.Value1 })
					.LeftJoin(g => g.ChildID == c.ChildID)
				select new { c.ChildID, v.P };

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionAfterTakeReadsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p.Value1)
					.Take(1)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionInEagerLoadReadsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				select new
				{
					c.ChildID,
					Values = db.GrandChild
						.Where(g => g.ChildID == c.ChildID)
						.Select(g => p.Value1)
						.DefaultIfEmpty()
						.ToArray(),
				};

			AssertQuery(query);
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionOrdersAsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => (int?)p.ParentID)
					.DefaultIfEmpty()
				orderby v ?? -1, c.ChildID
				select c.ChildID;

			// AssertQuery compares without order, so the expected order comes from the model collections
			var expected =
				from p in Parent
				from c in Child
				where c.ParentID == p.ParentID
				from v in GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => (int?)p.ParentID)
					.DefaultIfEmpty()
				orderby v ?? -1, c.ChildID
				select c.ChildID;

			query
				.ToList()
				.ShouldBe(expected.ToList());
		}

		[Table]
		sealed class OuterValueTarget
		{
			[PrimaryKey] public int  Id    { get; set; }
			[Column    ] public int? Value { get; set; }
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision
		[Test]
		public void OuterSourceProjectionInsertsNullWithoutMatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<OuterValueTarget>();

			var source =
				from p in db.Parent
				from c in db.Child
				where c.ParentID == p.ParentID
				from v in db.GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p.Value1)
					.Take(1)
					.DefaultIfEmpty()
				select new { c.ChildID, V = v };

			source.Insert(table, r => new OuterValueTarget { Id = r.ChildID, Value = r.V });

			var expected =
				from p in Parent
				from c in Child
				where c.ParentID == p.ParentID
				from v in GrandChild
					.Where(g => g.ChildID == c.ChildID)
					.Select(g => p.Value1)
					.Take(1)
					.DefaultIfEmpty()
				orderby c.ChildID
				select new { Id = c.ChildID, Value = v };

			table
				.OrderBy(r => r.Id)
				.Select(r => new { r.Id, r.Value })
				.ToList()
				.ShouldBe(expected.ToList());
		}

		// SQLite only, as OuterSourceProjectionReadsNullWithoutMatch: a builder decision. Access and Oracle 11 fail differently here:
		// they cannot convert DefaultIfEmpty().Max() over this projection to SQL, before or after the fix
		[Test]
		public void OuterSourceProjectionAggregatesThroughAssociation([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in p.Children
				select new
				{
					c.ChildID,
					Max = c.GrandChildren
						.Select(g => (int?)p.ParentID)
						.DefaultIfEmpty()
						.Max(),
				};

			// AssertQuery cannot follow the associations on its expected side, so the expected rows join the model collections
			var expected =
				from p in Parent
				from c in Child
				where c.ParentID == p.ParentID
				select new
				{
					c.ChildID,
					Max = GrandChild
						.Where(g => g.ParentID == c.ParentID && g.ChildID == c.ChildID)
						.Select(g => (int?)p.ParentID)
						.DefaultIfEmpty()
						.Max(),
				};

			AreEqual(expected, query);
		}

		// Access only: it does not NULL-extend the constant marker, so a sequence without a non-nullable value of its own keeps
		// the markers it always had. Only matched parents are read, because the unmatched ones still get the enclosing value there.
		[Test]
		public void OuterSourceProjectionKeepsMatchedRowsOnAccess([IncludeDataSources(true, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			// a matched person with a NULL MiddleName must keep the enclosing value: the nullable MiddleName cannot guard it
			var values =
				from p in db.Parent
				where p.ParentID <= 4
				from v in db.Person
					.Where(x => x.ID == p.ParentID)
					.Select(x => new { x.MiddleName, P = p.Value1 })
					.DefaultIfEmpty()
				select new { p.ParentID, v.P };

			AssertQuery(values);

			// ...and must not read as missing
			var presence =
				from p in db.Parent
				where p.ParentID <= 4
				from v in db.Person
					.Where(x => x.ID == p.ParentID)
					.Select(x => new { x.MiddleName, P = (int?)p.ParentID })
					.DefaultIfEmpty()
				select new
				{
					p.ParentID,
					IsMissing = v == null,
				};

			AssertQuery(presence);
		}

		// SQLite and Access: a non-nullable column of the sequence's own is the marker, which Access does NULL-extend, so Access
		// takes the fixed path here. Parents 5-7 have no person.
		[Test]
		public void OuterSourceProjectionGuardedByOwnNonNullableColumn([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var values =
				from p in db.Parent
				from v in db.Person
					.Where(x => x.ID == p.ParentID)
					.Select(x => new { x.ID, P = p.Value1 })
					.DefaultIfEmpty()
				select new { p.ParentID, v.P };

			AssertQuery(values);
		}

		// SQLite and Access, as OuterSourceProjectionGuardedByOwnNonNullableColumn: the own non-nullable column is the marker
		[Test]
		public void OuterSourceProjectionMarkedByOwnNonNullableColumn([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			// the enclosing non-nullable value comes first: it must not become the marker
			var presence =
				from p in db.Parent
				from v in db.Person
					.Where(x => x.ID == p.ParentID)
					.Select(x => new { P = p.ParentID, x.ID })
					.DefaultIfEmpty()
				select new
				{
					p.ParentID,
					IsMissing = v == null,
				};

			AssertQuery(presence);
		}
	}
}
