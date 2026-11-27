using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Interceptors;
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

		class BillClient
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		class BillClientWithKey
		{
			[PrimaryKey]
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		class ClientBill : IClientBill
		{
			public int  Id       { get; set; }
			public int? IdClient { get; set; }

			[Association(ThisKey = nameof(IdClient), OtherKey = nameof(Client.Id), CanBeNull = false)]
			public BillClient Client { get; set; } = null!;

			[Association(ThisKey = nameof(IdClient), OtherKey = nameof(BillClientWithKey.Id), CanBeNull = false)]
			public BillClientWithKey KeyedClient { get; set; } = null!;
		}

		interface IClientBill
		{
			int  Id       { get; set; }
			int? IdClient { get; set; }

			public BillClient        Client      { get; set; }
			public BillClientWithKey KeyedClient { get; set; }
		}

		class BillPosition : IBillPosition
		{
			public int Id { get; set; }
			public int? IdBill { get; set; }

			[Association(ThisKey = nameof(IdBill), OtherKey = nameof(ClientBill.Id), CanBeNull = false)]
			public ClientBill Bill { get; set; } = null!;
		}

		interface IBillPosition
		{
			int Id { get; set; }
			int? IdBill { get; set; }

			public ClientBill Bill { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void FilterByInterfaceAssociationNoKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<BillClient>();
			using var t2 = db.CreateLocalTable<ClientBill>();

			var query = from bill in Filter(db.GetTable<ClientBill>(), "Abc") select new { bill.Client.Name };

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(1));

			static IQueryable<TBill> Filter<TBill>(IQueryable<TBill> query, string clientName)
				where TBill : IClientBill
			{
				return query.Where(b => b.Client.Name == clientName);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void FilterByInterfaceJoinNoKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<BillClient>();
			using var t2 = db.CreateLocalTable<ClientBill>();

			var query = from bill in Filter(db, db.GetTable<ClientBill>(), "Abc")
						join client in t1
						on bill.IdClient equals client.Id
						select client.Name;

			query.ToArray();

			var sql = query.GetSelectQuery();
			// we cannot remove explicit join if we don't known cardinality
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(2));

			static IQueryable<TBill> Filter<TBill>(IDataContext db, IQueryable< TBill> query, string clientName)
				where TBill : IClientBill
			{
				return from bill in query
					   join client in db.GetTable<BillClient>()
					   on bill.IdClient equals client.Id
					   where client.Name == clientName
					   select bill;
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void FilterByInterfaceAssociationWithKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<BillClientWithKey>();
			using var t2 = db.CreateLocalTable<ClientBill>();

			var query = from bill in Filter(db.GetTable<ClientBill>(), "Abc") select new { bill.KeyedClient.Name };

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(1));

			static IQueryable<TBill> Filter<TBill>(IQueryable<TBill> query, string clientName)
				where TBill : IClientBill
			{
				return query.Where(b => b.KeyedClient.Name == clientName);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void FilterByInterfaceJoinWithKey([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<BillClientWithKey>();
			using var t2 = db.CreateLocalTable<ClientBill>();

			var query = from bill in Filter(db, db.GetTable<ClientBill>(), "Abc")
						join client in t1
						on bill.IdClient equals client.Id
						select client.Name;

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(1));

			static IQueryable<TBill> Filter<TBill>(IDataContext db, IQueryable<TBill> query, string clientName)
				where TBill : IClientBill
			{
				return from bill in query
					   join client in db.GetTable<BillClientWithKey>()
					   on bill.IdClient equals client.Id
					   where client.Name == clientName
					   select bill;
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4790")]
		public void FilterByInterfaceAssociationNested([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<BillClient>();
			using var t2 = db.CreateLocalTable<ClientBill>();
			using var t3 = db.CreateLocalTable<BillPosition>();

			var query = from position in Filter(db.GetTable<BillPosition>(), "Abc") select new { position.Bill.Client.Name };

			query.ToArray();

			var sql = query.GetSelectQuery();
			Assert.That(sql.GetTableSource().Joins, Has.Count.EqualTo(2));

			static IQueryable<TPosition> Filter<TPosition>(IQueryable<TPosition> query, string clientName)
				where TPosition : IBillPosition
			{
				return query.Where(p => p.Bill.Client.Name == clientName);
			}
		}

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

		// No ErrorTypeName: the two SqlClient packages raise their own SqlException and share only the message.
		[ActiveIssue(3305, Configuration = TestProvName.AllSqlServer,
			ErrorMessage = "Invalid object name 'InventoryResourceDTO'.",
			Details = "Issue number taken from this test, whose own repro this is. The table the query names is never resolved on SQL Server.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3305")]
		public void MultiLevelLeftJoinsOverCteWithUniqueKeys([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var s = new DataConnection(context);
			using var t1 = s.CreateTempTable<ChannelDTO>();
			using var t2 = s.CreateTempTable<InventoryResourceDTO>();
			using var t3 = s.CreateTempTable<MaterialDTO>();
			using var t4 = s.CreateTempTable<WmsLoadCarrierDTO>();
			using var t5 = s.CreateTempTable<RefResourceStorageShelfDTO>();
			using var t6 = s.CreateTempTable<AisleDTO>();
			using var t7 = s.CreateTempTable<RefOutfeedTransportOrderResourceDTO>();
			using var t8 = s.CreateTempTable<InfeedAdvicePositionDTO>();
			using var t9 = s.CreateTempTable<StorageShelfDTO>();

			var channelQry = s.GetTable<ChannelDTO>();
			var inventoryResourceQry = s.GetTable<InventoryResourceDTO>().Where(x => x.Status < 99);
			var materialQry = s.GetTable<MaterialDTO>().HasUniqueKey(m => m.Id);
			var resourceQry = s.GetTable<WmsLoadCarrierDTO>().HasUniqueKey(c => c.Id);
			var refResourceStorageShelfDtos = s.GetTable<RefResourceStorageShelfDTO>().HasUniqueKey(b => b.StorageShelfID).HasUniqueKey(b => b.ResourceID);
			var storageShelfDtos = s.GetTable<StorageShelfDTO>().HasUniqueKey(f => new { f.ChannelID, f.DepthCoordinate });

			var inventoryResourceQryUsed = inventoryResourceQry
			   .Select(ir => new
			   {
				   RN = Sql.Ext.RowNumber().Over().PartitionBy(ir.ResourceID).OrderBy(ir.ResourceID).ToValue(),
				   Count = Sql.Ext.Count().Over().PartitionBy(ir.ResourceID).ToValue(),
				   CountLocked = Sql.Ext.Count(ir.ProductStatus > 0).Over().PartitionBy(ir.ResourceID).ToValue(),
				   IR = ir
			   }).AsCte()
			   .HasUniqueKey(ir => new { ir.IR.ResourceID, ir.RN });

			var query = from c in channelQry
						from a in s.GetTable<AisleDTO>().InnerJoin(a => c.AisleID == a.Id)
						from m in materialQry.LeftJoin(m => c.MaterialID == m.Id)

							#region Depth 1

						from a1 in storageShelfDtos.LeftJoin(a1 => c.Id == a1.ChannelID && 1 == a1.DepthCoordinate)
						from b1 in refResourceStorageShelfDtos.LeftJoin(b1 => a1.Id == b1.StorageShelfID)
						from c1 in resourceQry.LeftJoin(c1 => c1.Id == b1.ResourceID)
						from i1 in inventoryResourceQryUsed.LeftJoin(i1 => i1.IR.ResourceID == b1.ResourceID && i1.RN == 1)
						from m1 in materialQry.LeftJoin(m1 => m1.Id == i1.IR.MaterialID)

							#endregion

							#region Depth 2

						from a2 in storageShelfDtos.LeftJoin(a2 => c.Id == a2.ChannelID && 2 == a2.DepthCoordinate)
						from b2 in refResourceStorageShelfDtos.LeftJoin(b2 => a2.Id == b2.StorageShelfID)
						from c2 in resourceQry.LeftJoin(c2 => c2.Id == b2.ResourceID)
						from i2 in inventoryResourceQryUsed.LeftJoin(i2 => i2.IR.ResourceID == b2.ResourceID && i2.RN == 1)
						from m2 in materialQry.LeftJoin(m2 => m2.Id == i2.IR.MaterialID)

							#endregion

						select new ChannelInfoCombinedDTO
						{
							Id = c.Id,
							ChannelInfo = new ChannelInfoDTO
							{
								Id = c.Id,
								Channel = c,
								Aisle = new AisleDTO() { StrippedDownDTO = true, Id = a.Id, AisleNumber = a.AisleNumber, PlantID = a.PlantID, Name = a.Name },
								Material = new MaterialDTO { StrippedDownDTO = true, Id = m.Id, MaterialNumber = m.MaterialNumber, MaterialDescription_1 = m.MaterialDescription_1, MaterialDescription_2 = m.MaterialDescription_2, MaterialDescription_3 = m.MaterialDescription_3, CategoryABC = m.CategoryABC, CategoryCustoms = m.CategoryCustoms, CategoryDimensions = m.CategoryDimensions, CategoryQuality = m.CategoryQuality, CategoryTemperature = m.CategoryTemperature },
							},
							Shelf1 = (a1 != null ? new ShelfInfoDTO()
							{
								Id = a1.Id,
								LoadCarrier = new WmsLoadCarrierDTO { StrippedDownDTO = true, Id = c1.Id, Status = c1.Status, ResourceLabel = c1.ResourceLabel, CustomField1 = c1.CustomField1, CustomField2 = c1.CustomField2, CustomField3 = c1.CustomField3, CustomLong1 = c1.CustomLong1, CustomLong2 = c1.CustomLong2, CustomLong3 = c1.CustomLong3 },
								Material = new MaterialDTO { StrippedDownDTO = true, Id = m1.Id, MaterialNumber = m1.MaterialNumber, MaterialDescription_1 = m1.MaterialDescription_1, MaterialDescription_2 = m1.MaterialDescription_2, MaterialDescription_3 = m1.MaterialDescription_3, CategoryABC = m1.CategoryABC, CategoryCustoms = m1.CategoryCustoms, CategoryDimensions = m1.CategoryDimensions, CategoryQuality = m1.CategoryQuality, CategoryTemperature = m1.CategoryTemperature },
								InventoryResource = new InventoryResourceDTO { StrippedDownDTO = true, Id = i1.IR.Id, BatchNumber = i1.IR.BatchNumber, BundleUnit = i1.IR.BundleUnit, ProductStatus = i1.IR.ProductStatus, CustomField1 = i1.IR.CustomField1, CustomField2 = i1.IR.CustomField2, CustomField3 = i1.IR.CustomField3 },
								StorageShelf = new StorageShelfDTO { StrippedDownDTO = true, Id = a1.Id, Status = a1.Status, CategoryABC = a1.CategoryABC, HeightClass = a1.HeightClass },
								InventoryCount = (int)i1.Count,
								LockedMaterial = i1.CountLocked,
								ReservedForOutfeed = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c1.Id),
								ManuallyInfeededMaterial = inventoryResourceQryUsed.Any(x => x.IR.ResourceID == c1.Id && (x.IR.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.IR.InfeedAdviceID && y.InfeedAdviceType ==1)))
							} : null),
							Shelf2 = (a2 != null ? new ShelfInfoDTO()
							{
								Id = a2.Id,
								LoadCarrier = new WmsLoadCarrierDTO { StrippedDownDTO = true, Id = c2.Id, Status = c2.Status, ResourceLabel = c2.ResourceLabel, CustomField1 = c2.CustomField1, CustomField2 = c2.CustomField2, CustomField3 = c2.CustomField3, CustomLong1 = c2.CustomLong1, CustomLong2 = c2.CustomLong2, CustomLong3 = c2.CustomLong3 },
								Material = new MaterialDTO { StrippedDownDTO = true, Id = m2.Id, MaterialNumber = m2.MaterialNumber, MaterialDescription_1 = m2.MaterialDescription_1, MaterialDescription_2 = m2.MaterialDescription_2, MaterialDescription_3 = m2.MaterialDescription_3, CategoryABC = m2.CategoryABC, CategoryCustoms = m2.CategoryCustoms, CategoryDimensions = m2.CategoryDimensions, CategoryQuality = m2.CategoryQuality, CategoryTemperature = m2.CategoryTemperature },
								InventoryResource = new InventoryResourceDTO { StrippedDownDTO = true, Id = i2.IR.Id, BatchNumber = i2.IR.BatchNumber, BundleUnit = i2.IR.BundleUnit, ProductStatus = i2.IR.ProductStatus, CustomField1 = i2.IR.CustomField1, CustomField2 = i2.IR.CustomField2, CustomField3 = i2.IR.CustomField3 },
								StorageShelf = new StorageShelfDTO { StrippedDownDTO = true, Id = a2.Id, Status = a2.Status, CategoryABC = a2.CategoryABC, HeightClass = a2.HeightClass },
								InventoryCount = (int)i2.Count,
								LockedMaterial = i2.CountLocked,
								ReservedForOutfeed = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c2.Id),
								ManuallyInfeededMaterial = inventoryResourceQryUsed.Any(x => x.IR.ResourceID == c2.Id && (x.IR.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.IR.InfeedAdviceID && y.InfeedAdviceType ==1)))
							} : null),
						};

			var maxDepth = 1;
			var qry = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1 });

			if (maxDepth > 1)
				qry = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2 });

			var lst = qry.ToList();

			var sql = s.LastQuery;
		}

		public class AisleDTO 
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int AisleNumber { get; set; }
			public virtual int PlantID { get; set; }
			public virtual string? Name { get; set; }
		}

		public class ChannelDTO
		{
			public virtual int Id { get; set; }
			public virtual int AisleID { get; set; }
			public virtual int MaterialID { get; set; }
		}

		public class ShelfInfoDTO
		{
			public virtual int Id { get; set; }
			public virtual WmsLoadCarrierDTO? LoadCarrier { get; set; }
			public virtual InventoryResourceDTO? InventoryResource { get; set; }
			public virtual MaterialDTO? Material { get; set; }
			public virtual StorageShelfDTO? StorageShelf { get; set; }
			public virtual int InventoryCount { get; set; }
			public virtual int LockedMaterial { get; set; }
			public virtual bool ReservedForOutfeed { get; set; }
			public virtual bool ManuallyInfeededMaterial { get; set; }
		}

		public class ChannelInfoCombinedDTO
		{
			public virtual int Id { get; set; }
			public virtual ChannelInfoDTO? ChannelInfo { get; set; }
			public virtual ShelfInfoDTO? Shelf1 { get; set; }
			public virtual ShelfInfoDTO? Shelf2 { get; set; }
		}

		public class ChannelInfoDTO
		{
			public virtual int Id { get; set; }
			public virtual ChannelDTO? Channel { get; set; }
			public virtual AisleDTO? Aisle { get; set; }
			public virtual MaterialDTO? Material { get; set; }
		}

		public class InfeedAdvicePositionDTO
		{
			public virtual int Id { get; set; }
			public virtual int InfeedAdviceType { get; set; }
			public virtual AisleDTO? Aisle { get; set; }
			public virtual MaterialDTO? Material { get; set; }
		}

		public class InventoryResourceDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int Status { get; set; }
			public virtual int ResourceID { get; set; }
			public virtual int ProductStatus { get; set; }
			public virtual int BatchNumber { get; set; }
			public virtual int BundleUnit { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual int MaterialID { get; set; }
			public virtual int? InfeedAdviceID { get; set; }
		}

		public class MaterialDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int MaterialNumber { get; set; }
			public virtual string? MaterialDescription_1 { get; set; }
			public virtual string? MaterialDescription_2 { get; set; }
			public virtual string? MaterialDescription_3 { get; set; }
			public virtual string? CategoryABC { get; set; }
			public virtual string? CategoryCustoms { get; set; }
			public virtual string? CategoryDimensions { get; set; }
			public virtual string? CategoryQuality { get; set; }
			public virtual string? CategoryTemperature { get; set; }
		}

		public class WmsLoadCarrierDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int Status { get; set; }
			public virtual int ResourceLabel { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual int CustomLong1 { get; set; }
			public virtual int CustomLong2 { get; set; }
			public virtual int CustomLong3 { get; set; }
		}

		public class RefResourceStorageShelfDTO
		{
			public virtual int Id { get; set; }
			public virtual int StorageShelfID { get; set; }
			public virtual int ResourceID { get; set; }
		}

		public class RefOutfeedTransportOrderResourceDTO
		{
			public virtual int Id { get; set; }
			public virtual int ResourceID { get; set; }
		}

		public class StorageShelfDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int ChannelID { get; set; }
			public virtual int Status { get; set; }
			public virtual int CategoryABC { get; set; }
			public virtual int HeightClass { get; set; }
			public virtual int DepthCoordinate { get; set; }
		}

		[ThrowsForProvider("DuckDB.NET.Data.DuckDBException", TestProvName.AllDuckDB, ErrorMessage = "Not implemented Error: Cannot perform non-inner join on subquery!")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3432")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void OuterApplyOptimizationWithOrPredicateAndAnySubquery([DataSources(TestProvName.AllNativeAccess, TestProvName.AllClickHouse)] string context)
		{
			const string Admin = "Admin";

			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<PartyTask>();
			using var t2 = db.CreateLocalTable<Party>();
			using var t3 = db.CreateLocalTable<PartyAccess>();

			var query =
				from task in db.GetTable<PartyTask>()
				from party in db.GetTable<Party>()
					.Where(p => task.AdminPartyId == p.Id || db.GetTable<PartyAccess>().Any(pa => pa.PartyId == p.Id && pa.Role == Admin))
					.DefaultIfEmpty()
				select new { task.Description, party.Name };

			query.ToArray();
		}

		[Table]
		public class PartyTask
		{
			[PrimaryKey] public int     AdminPartyId { get; set; }
			[Column] public string? Description  { get; set; }
		}

		[Table]
		public class Party
		{
			[PrimaryKey] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }
		}

		[Table]
		public class PartyAccess
		{
			[PrimaryKey] public int     PartyId { get; set; }
			[Column] public string? Role    { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4337")]
		public void ChannelShelfInfoProjectionSubsets([IncludeDataSources(ProviderName.SQLiteMS, ProviderName.SqlServer2019)] string context)
		{
			using var s = new DataConnection(context);

			using var channelQry                          = s.CreateLocalTable<WmsChannel>();
			using var inventoryResourceTbl                = s.CreateLocalTable<WmsInventory>();
			using var materialQry                         = s.CreateLocalTable<WmsMaterial>();
			using var resourceQry                         = s.CreateLocalTable<WmsLoadCarrier>();
			using var refResourceStorageShelfDTO          = s.CreateLocalTable<WmsResourceShelf>();
			using var aisleDTO                            = s.CreateLocalTable<WmsAisle>();
			using var refOutfeedTransportOrderResourceDTO = s.CreateLocalTable<WmsOutfeedReservation>();
			using var infeedAdvicePositionDTO             = s.CreateLocalTable<WmsInfeedPosition>();
			using var storageShelfDTO                     = s.CreateLocalTable<WmsStorageShelf>();

			var inventoryResourceQry = inventoryResourceTbl.Where(x => x.Status < 99);

			var query = from c in channelQry
						join a in aisleDTO on c.AisleID equals a.Id

						join m in materialQry on c.MaterialID equals m.Id into mn
						from m in mn.DefaultIfEmpty()

							/*
							join ss in s.GetTable<WmsStorageShelf>() on c.Id equals ss.ChannelID
							join rss in s.GetTable<WmsResourceShelf>() on ss.Id equals rss.StorageShelfID into rssn
							*/

							#region Depth 1

						join a1 in storageShelfDTO on new { Id = c.Id, Depth = 1 } equals new { Id = a1.ChannelID, Depth = a1.DepthCoordinate } into a1n
						from a1 in a1n.DefaultIfEmpty()
						join b1 in refResourceStorageShelfDTO on a1.Id equals b1.StorageShelfID into b1n
						from b1 in b1n.DefaultIfEmpty()
						join c1 in resourceQry on b1.ResourceID equals c1.Id into c1n
						from c1 in c1n.DefaultIfEmpty()

						join i1 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b1.ResourceID).Id equals i1.Id into i1n
						from i1 in i1n.DefaultIfEmpty()

						join m1 in materialQry on i1.MaterialID equals m1.Id into m1n
						from m1 in m1n.DefaultIfEmpty()

						let ir1ic = inventoryResourceQry.Count(x => x.ResourceID == c1.Id)
						let ir1lm = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && x.ProductStatus > 0)

						let ror1 = refOutfeedTransportOrderResourceDTO.Any(x => x.ResourceID == c1.Id)
						let ir1mim = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && (x.InfeedAdviceID == null || infeedAdvicePositionDTO.Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 2

						join a2 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 2 } equals new { Id = a2.ChannelID, Depth = a2.DepthCoordinate } into a2n
						from a2 in a2n.DefaultIfEmpty()
						join b2 in refResourceStorageShelfDTO on a2.Id equals b2.StorageShelfID into b2n
						from b2 in b2n.DefaultIfEmpty()
						join c2 in resourceQry on b2.ResourceID equals c2.Id into c2n
						from c2 in c2n.DefaultIfEmpty()

						join i2 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b2.ResourceID).Id equals i2.Id into i2n
						from i2 in i2n.DefaultIfEmpty()

						join m2 in materialQry on i2.MaterialID equals m2.Id into m2n
						from m2 in m2n.DefaultIfEmpty()

						let ir2ic = inventoryResourceQry.Count(x => x.ResourceID == c2.Id)
						let ir2lm = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && x.ProductStatus > 0)

						let ror2 = refOutfeedTransportOrderResourceDTO.Any(x => x.ResourceID == c2.Id)
						let ir2mim = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && (x.InfeedAdviceID == null || infeedAdvicePositionDTO.Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 3

						join a3 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 3 } equals new { Id = a3.ChannelID, Depth = a3.DepthCoordinate } into a3n
						from a3 in a3n.DefaultIfEmpty()
						join b3 in s.GetTable<WmsResourceShelf>() on a3.Id equals b3.StorageShelfID into b3n
						from b3 in b3n.DefaultIfEmpty()
						join c3 in s.GetTable<WmsLoadCarrier>() on b3.ResourceID equals c3.Id into c3n
						from c3 in c3n.DefaultIfEmpty()

						join i3 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b3.ResourceID).Id equals i3.Id into i3n
						from i3 in i3n.DefaultIfEmpty()

						join m3 in s.GetTable<WmsMaterial>() on i3.MaterialID equals m3.Id into m3n
						from m3 in m3n.DefaultIfEmpty()

						let ir3ic = inventoryResourceQry.Count(x => x.ResourceID == c3.Id)
						let ir3lm = inventoryResourceQry.Any(x => x.ResourceID == c3.Id && x.ProductStatus > 0)
						let ror3 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c3.Id)
						let ir3mim = inventoryResourceQry.Any(x => x.ResourceID == c3.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 4

						join a4 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 4 } equals new { Id = a4.ChannelID, Depth = a4.DepthCoordinate } into a4n
						from a4 in a4n.DefaultIfEmpty()
						join b4 in s.GetTable<WmsResourceShelf>() on a4.Id equals b4.StorageShelfID into b4n
						from b4 in b4n.DefaultIfEmpty()
						join c4 in s.GetTable<WmsLoadCarrier>() on b4.ResourceID equals c4.Id into c4n
						from c4 in c4n.DefaultIfEmpty()

						join i4 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b4.ResourceID).Id equals i4.Id into i4n
						from i4 in i4n.DefaultIfEmpty()

						join m4 in s.GetTable<WmsMaterial>() on i4.MaterialID equals m4.Id into m4n
						from m4 in m4n.DefaultIfEmpty()

						let ir4ic = inventoryResourceQry.Count(x => x.ResourceID == c4.Id)
						let ir4lm = inventoryResourceQry.Any(x => x.ResourceID == c4.Id && x.ProductStatus > 0)
						let ror4 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c4.Id)
						let ir4mim = inventoryResourceQry.Any(x => x.ResourceID == c4.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 5

						join a5 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 5 } equals new { Id = a5.ChannelID, Depth = a5.DepthCoordinate } into a5n
						from a5 in a5n.DefaultIfEmpty()
						join b5 in s.GetTable<WmsResourceShelf>() on a5.Id equals b5.StorageShelfID into b5n
						from b5 in b5n.DefaultIfEmpty()
						join c5 in s.GetTable<WmsLoadCarrier>() on b5.ResourceID equals c5.Id into c5n
						from c5 in c5n.DefaultIfEmpty()

						join i5 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b5.ResourceID).Id equals i5.Id into i5n
						from i5 in i5n.DefaultIfEmpty()

						join m5 in s.GetTable<WmsMaterial>() on i5.MaterialID equals m5.Id into m5n
						from m5 in m5n.DefaultIfEmpty()

						let ir5ic = inventoryResourceQry.Count(x => x.ResourceID == c5.Id)
						let ir5lm = inventoryResourceQry.Any(x => x.ResourceID == c5.Id && x.ProductStatus > 0)
						let ror5 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c5.Id)
						let ir5mim = inventoryResourceQry.Any(x => x.ResourceID == c5.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 6

						join a6 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 6 } equals new { Id = a6.ChannelID, Depth = a6.DepthCoordinate } into a6n
						from a6 in a6n.DefaultIfEmpty()
						join b6 in s.GetTable<WmsResourceShelf>() on a6.Id equals b6.StorageShelfID into b6n
						from b6 in b6n.DefaultIfEmpty()
						join c6 in s.GetTable<WmsLoadCarrier>() on b6.ResourceID equals c6.Id into c6n
						from c6 in c6n.DefaultIfEmpty()

						join i6 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b6.ResourceID).Id equals i6.Id into i6n
						from i6 in i6n.DefaultIfEmpty()

						join m6 in s.GetTable<WmsMaterial>() on i6.MaterialID equals m6.Id into m6n
						from m6 in m6n.DefaultIfEmpty()

						let ir6ic = inventoryResourceQry.Count(x => x.ResourceID == c6.Id)
						let ir6lm = inventoryResourceQry.Any(x => x.ResourceID == c6.Id && x.ProductStatus > 0)
						let ror6 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c6.Id)
						let ir6mim = inventoryResourceQry.Any(x => x.ResourceID == c6.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 7
						
						join a7 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 7 } equals new { Id = a7.ChannelID, Depth = a7.DepthCoordinate } into a7n
						from a7 in a7n.DefaultIfEmpty()
						join b7 in s.GetTable<WmsResourceShelf>() on a7.Id equals b7.StorageShelfID into b7n
						from b7 in b7n.DefaultIfEmpty()
						join c7 in s.GetTable<WmsLoadCarrier>() on b7.ResourceID equals c7.Id into c7n
						from c7 in c7n.DefaultIfEmpty()

						join i7 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b7.ResourceID).Id equals i7.Id into i7n
						from i7 in i7n.DefaultIfEmpty()

						join m7 in s.GetTable<WmsMaterial>() on i7.MaterialID equals m7.Id into m7n
						from m7 in m7n.DefaultIfEmpty()

						let ir7ic = inventoryResourceQry.Count(x => x.ResourceID == c7.Id)
						let ir7lm = inventoryResourceQry.Any(x => x.ResourceID == c7.Id && x.ProductStatus > 0)
						let ror7 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c7.Id)
						let ir7mim = inventoryResourceQry.Any(x => x.ResourceID == c7.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 8

						join a8 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 8 } equals new { Id = a8.ChannelID, Depth = a8.DepthCoordinate } into a8n
						from a8 in a8n.DefaultIfEmpty()
						join b8 in s.GetTable<WmsResourceShelf>() on a8.Id equals b8.StorageShelfID into b8n
						from b8 in b8n.DefaultIfEmpty()
						join c8 in s.GetTable<WmsLoadCarrier>() on b8.ResourceID equals c8.Id into c8n
						from c8 in c8n.DefaultIfEmpty()

						join i8 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b8.ResourceID).Id equals i8.Id into i8n
						from i8 in i8n.DefaultIfEmpty()

						join m8 in s.GetTable<WmsMaterial>() on i8.MaterialID equals m8.Id into m8n
						from m8 in m8n.DefaultIfEmpty()

						let ir8ic = inventoryResourceQry.Count(x => x.ResourceID == c8.Id)
						let ir8lm = inventoryResourceQry.Any(x => x.ResourceID == c8.Id && x.ProductStatus > 0)
						let ror8 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c8.Id)
						let ir8mim = inventoryResourceQry.Any(x => x.ResourceID == c8.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						// commented as it causes stack overflow

						//#region Depth 9

						//join a9 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 9 } equals new { Id = a9.ChannelID, Depth = a9.DepthCoordinate } into a9n
						//from a9 in a9n.DefaultIfEmpty()
						//join b9 in s.GetTable<WmsResourceShelf>() on a9.Id equals b9.StorageShelfID into b9n
						//from b9 in b9n.DefaultIfEmpty()
						//join c9 in s.GetTable<WmsLoadCarrier>() on b9.ResourceID equals c9.Id into c9n
						//from c9 in c9n.DefaultIfEmpty()

						//join i9 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b9.ResourceID).Id equals i9.Id into i9n
						//from i9 in i9n.DefaultIfEmpty()

						//join m9 in s.GetTable<WmsMaterial>() on i9.MaterialID equals m9.Id into m9n
						//from m9 in m9n.DefaultIfEmpty()

						//let ir9ic = inventoryResourceQry.Count(x => x.ResourceID == c9.Id)
						//let ir9lm = inventoryResourceQry.Any(x => x.ResourceID == c9.Id && x.ProductStatus > 0)
						//let ror9 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c9.Id)
						//let ir9mim = inventoryResourceQry.Any(x => x.ResourceID == c9.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						//#endregion

						//#region Depth 10

						//join a10 in s.GetTable<WmsStorageShelf>() on new { Id = c.Id, Depth = 10 } equals new { Id = a10.ChannelID, Depth = a10.DepthCoordinate } into a10n
						//from a10 in a10n.DefaultIfEmpty()
						//join b10 in s.GetTable<WmsResourceShelf>() on a10.Id equals b10.StorageShelfID into b10n
						//from b10 in b10n.DefaultIfEmpty()
						//join c10 in s.GetTable<WmsLoadCarrier>() on b10.ResourceID equals c10.Id into c10n
						//from c10 in c10n.DefaultIfEmpty()

						//join i10 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b10.ResourceID).Id equals i10.Id into i10n
						//from i10 in i10n.DefaultIfEmpty()

						//join m10 in s.GetTable<WmsMaterial>() on i10.MaterialID equals m10.Id into m10n
						//from m10 in m10n.DefaultIfEmpty()

						//let ir10ic = inventoryResourceQry.Count(x => x.ResourceID == c10.Id)
						//let ir10lm = inventoryResourceQry.Any(x => x.ResourceID == c10.Id && x.ProductStatus > 0)
						//let ror10 = s.GetTable<WmsOutfeedReservation>().Any(x => x.ResourceID == c10.Id)
						//let ir10mim = inventoryResourceQry.Any(x => x.ResourceID == c10.Id && (x.InfeedAdviceID == null || s.GetTable<WmsInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))
						
						//#endregion
						select new WmsChannelInfoCombined
						{
							Id = c.Id,
							ChannelInfo = new WmsChannelInfo
							{
								Id = c.Id,
								Channel = c,
								Aisle = new WmsAisle() { StrippedDownDTO = true, Id = a.Id, AisleNumber = a.AisleNumber, PlantID = a.PlantID, Name = a.Name },
								Material = new WmsMaterial { StrippedDownDTO = true, Id = m.Id, MaterialNumber = m.MaterialNumber, MaterialDescription_1 = m.MaterialDescription_1, MaterialDescription_2 = m.MaterialDescription_2, MaterialDescription_3 = m.MaterialDescription_3, CategoryABC = m.CategoryABC, CategoryCustoms = m.CategoryCustoms, CategoryDimensions = m.CategoryDimensions, CategoryQuality = m.CategoryQuality, CategoryTemperature = m.CategoryTemperature },
							},
							Shelf1 = ((a1 != null && a1.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a1.Id,
								LoadCarrierId = c1.Id,
								LoadCarrierResourceLabel = c1.ResourceLabel,
								LoadCarrierStatus = c1.Status,
								LoadCarrierCustomLong2 = c1.CustomLong2,
								LoadCarrierHeightClass = c1.HeightClass,
								LoadCarrierTypeId = c1.TypeID,

								MaterialId = m1.Id,
								MaterialNumber = m1.MaterialNumber,
								MaterialDescription_1 = m1.MaterialDescription_1,
								MaterialCategoryABC = m1.CategoryABC,

								StorageShelfId = a1.Id,
								StorageShelfStatus = a1.Status,
								StorageShelfName = a1.Name,
								StorageShelfHeightClass = a1.HeightClass,
								StorageShelfCategoryABC = a1.CategoryABC,
								StorageShelfAisleNumber = a1.AisleNumber,

								InventoryResourceId = i1.Id,
								InventoryResourceBatchNumber = i1.BatchNumber,
								InventoryResourceProductStatus = i1.ProductStatus,
								InventoryResourceExpiryDate = i1.ExpiryDate,
								InventoryResourceCustomDate1 = i1.CustomDate1,

								InventoryCount = ir1ic,
								ReservedForOutfeed = ror1,
								ManuallyInfeededMaterial = ir1mim
							}: null),
							Shelf2 = ((a2 != null && a2.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a2.Id,
								LoadCarrierId = c2.Id,
								LoadCarrierResourceLabel = c2.ResourceLabel,
								LoadCarrierStatus = c2.Status,
								LoadCarrierCustomLong2 = c2.CustomLong2,
								LoadCarrierHeightClass = c2.HeightClass,
								LoadCarrierTypeId = c2.TypeID,

								MaterialId = m2.Id,
								MaterialNumber = m2.MaterialNumber,
								MaterialDescription_1 = m2.MaterialDescription_1,
								MaterialCategoryABC = m2.CategoryABC,

								StorageShelfId = a2.Id,
								StorageShelfStatus = a2.Status,
								StorageShelfName = a2.Name,
								StorageShelfHeightClass = a2.HeightClass,
								StorageShelfCategoryABC = a2.CategoryABC,
								StorageShelfAisleNumber = a2.AisleNumber,

								InventoryResourceId = i2.Id,
								InventoryResourceBatchNumber = i2.BatchNumber,
								InventoryResourceProductStatus = i2.ProductStatus,
								InventoryResourceExpiryDate = i2.ExpiryDate,
								InventoryResourceCustomDate1 = i2.CustomDate1,

								InventoryCount = ir2ic,
								ReservedForOutfeed = ror2,
								ManuallyInfeededMaterial = ir2mim
							} : null),
							Shelf3 = ((a3 != null && a3.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a3.Id,
								LoadCarrierId = c3.Id,
								LoadCarrierResourceLabel = c3.ResourceLabel,
								LoadCarrierStatus = c3.Status,
								LoadCarrierCustomLong2 = c3.CustomLong2,
								LoadCarrierHeightClass = c3.HeightClass,
								LoadCarrierTypeId = c3.TypeID,

								MaterialId = m3.Id,
								MaterialNumber = m3.MaterialNumber,
								MaterialDescription_1 = m3.MaterialDescription_1,
								MaterialCategoryABC = m3.CategoryABC,

								StorageShelfId = a3.Id,
								StorageShelfStatus = a3.Status,
								StorageShelfName = a3.Name,
								StorageShelfHeightClass = a3.HeightClass,
								StorageShelfCategoryABC = a3.CategoryABC,
								StorageShelfAisleNumber = a3.AisleNumber,

								InventoryResourceId = i3.Id,
								InventoryResourceBatchNumber = i3.BatchNumber,
								InventoryResourceProductStatus = i3.ProductStatus,
								InventoryResourceExpiryDate = i3.ExpiryDate,
								InventoryResourceCustomDate1 = i3.CustomDate1,

								InventoryCount = ir3ic,
								ReservedForOutfeed = ror3,
								ManuallyInfeededMaterial = ir3mim
							} : null),
							Shelf4 = ((a4 != null && a4.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a4.Id,
								LoadCarrierId = c4.Id,
								LoadCarrierResourceLabel = c4.ResourceLabel,
								LoadCarrierStatus = c4.Status,
								LoadCarrierCustomLong2 = c4.CustomLong2,
								LoadCarrierHeightClass = c4.HeightClass,
								LoadCarrierTypeId = c4.TypeID,

								MaterialId = m4.Id,
								MaterialNumber = m4.MaterialNumber,
								MaterialDescription_1 = m4.MaterialDescription_1,
								MaterialCategoryABC = m4.CategoryABC,

								StorageShelfId = a4.Id,
								StorageShelfStatus = a4.Status,
								StorageShelfName = a4.Name,
								StorageShelfHeightClass = a4.HeightClass,
								StorageShelfCategoryABC = a4.CategoryABC,
								StorageShelfAisleNumber = a4.AisleNumber,

								InventoryResourceId = i4.Id,
								InventoryResourceBatchNumber = i4.BatchNumber,
								InventoryResourceProductStatus = i4.ProductStatus,
								InventoryResourceExpiryDate = i4.ExpiryDate,
								InventoryResourceCustomDate1 = i4.CustomDate1,

								InventoryCount = ir4ic,
								ReservedForOutfeed = ror4,
								ManuallyInfeededMaterial = ir4mim
							} : null),
							Shelf5 = ((a5 != null && a5.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a5.Id,
								LoadCarrierId = c5.Id,
								LoadCarrierResourceLabel = c5.ResourceLabel,
								LoadCarrierStatus = c5.Status,
								LoadCarrierCustomLong2 = c5.CustomLong2,
								LoadCarrierHeightClass = c5.HeightClass,
								LoadCarrierTypeId = c5.TypeID,

								MaterialId = m5.Id,
								MaterialNumber = m5.MaterialNumber,
								MaterialDescription_1 = m5.MaterialDescription_1,
								MaterialCategoryABC = m5.CategoryABC,

								StorageShelfId = a5.Id,
								StorageShelfStatus = a5.Status,
								StorageShelfName = a5.Name,
								StorageShelfHeightClass = a5.HeightClass,
								StorageShelfCategoryABC = a5.CategoryABC,
								StorageShelfAisleNumber = a5.AisleNumber,

								InventoryResourceId = i5.Id,
								InventoryResourceBatchNumber = i5.BatchNumber,
								InventoryResourceProductStatus = i5.ProductStatus,
								InventoryResourceExpiryDate = i5.ExpiryDate,
								InventoryResourceCustomDate1 = i5.CustomDate1,

								InventoryCount = ir5ic,
								ReservedForOutfeed = ror5,
								ManuallyInfeededMaterial = ir5mim
							} : null),
							Shelf6 = ((a6 != null && a6.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a6.Id,
								LoadCarrierId = c6.Id,
								LoadCarrierResourceLabel = c6.ResourceLabel,
								LoadCarrierStatus = c6.Status,
								LoadCarrierCustomLong2 = c6.CustomLong2,
								LoadCarrierHeightClass = c6.HeightClass,
								LoadCarrierTypeId = c6.TypeID,

								MaterialId = m6.Id,
								MaterialNumber = m6.MaterialNumber,
								MaterialDescription_1 = m6.MaterialDescription_1,
								MaterialCategoryABC = m6.CategoryABC,

								StorageShelfId = a6.Id,
								StorageShelfStatus = a6.Status,
								StorageShelfName = a6.Name,
								StorageShelfHeightClass = a6.HeightClass,
								StorageShelfCategoryABC = a6.CategoryABC,
								StorageShelfAisleNumber = a6.AisleNumber,

								InventoryResourceId = i6.Id,
								InventoryResourceBatchNumber = i6.BatchNumber,
								InventoryResourceProductStatus = i6.ProductStatus,
								InventoryResourceExpiryDate = i6.ExpiryDate,
								InventoryResourceCustomDate1 = i6.CustomDate1,

								InventoryCount = ir6ic,
								ReservedForOutfeed = ror6,
								ManuallyInfeededMaterial = ir6mim
							} : null),
							Shelf7 = ((a7 != null && a7.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a7.Id,
								LoadCarrierId = c7.Id,
								LoadCarrierResourceLabel = c7.ResourceLabel,
								LoadCarrierStatus = c7.Status,
								LoadCarrierCustomLong2 = c7.CustomLong2,
								LoadCarrierHeightClass = c7.HeightClass,
								LoadCarrierTypeId = c7.TypeID,

								MaterialId = m7.Id,
								MaterialNumber = m7.MaterialNumber,
								MaterialDescription_1 = m7.MaterialDescription_1,
								MaterialCategoryABC = m7.CategoryABC,

								StorageShelfId = a7.Id,
								StorageShelfStatus = a7.Status,
								StorageShelfName = a7.Name,
								StorageShelfHeightClass = a7.HeightClass,
								StorageShelfCategoryABC = a7.CategoryABC,
								StorageShelfAisleNumber = a7.AisleNumber,

								InventoryResourceId = i7.Id,
								InventoryResourceBatchNumber = i7.BatchNumber,
								InventoryResourceProductStatus = i7.ProductStatus,
								InventoryResourceExpiryDate = i7.ExpiryDate,
								InventoryResourceCustomDate1 = i7.CustomDate1,

								InventoryCount = ir7ic,
								ReservedForOutfeed = ror7,
								ManuallyInfeededMaterial = ir7mim
							} : null),
							Shelf8 = ((a8 != null && a8.Id != Guid.Empty) ? new WmsShelfInfo()
							{
								Id = a8.Id,
								LoadCarrierId = c8.Id,
								LoadCarrierResourceLabel = c8.ResourceLabel,
								LoadCarrierStatus = c8.Status,
								LoadCarrierCustomLong2 = c8.CustomLong2,
								LoadCarrierHeightClass = c8.HeightClass,
								LoadCarrierTypeId = c8.TypeID,

								MaterialId = m8.Id,
								MaterialNumber = m8.MaterialNumber,
								MaterialDescription_1 = m8.MaterialDescription_1,
								MaterialCategoryABC = m8.CategoryABC,

								StorageShelfId = a8.Id,
								StorageShelfStatus = a8.Status,
								StorageShelfName = a8.Name,
								StorageShelfHeightClass = a8.HeightClass,
								StorageShelfCategoryABC = a8.CategoryABC,
								StorageShelfAisleNumber = a8.AisleNumber,

								InventoryResourceId = i8.Id,
								InventoryResourceBatchNumber = i8.BatchNumber,
								InventoryResourceProductStatus = i8.ProductStatus,
								InventoryResourceExpiryDate = i8.ExpiryDate,
								InventoryResourceCustomDate1 = i8.CustomDate1,

								InventoryCount = ir8ic,
								ReservedForOutfeed = ror8,
								ManuallyInfeededMaterial = ir8mim
							} : null),
							//Shelf9 = ((a9 != null && a9.Id != Guid.Empty) ? new WmsShelfInfo()
							//{
							//	Id = a9.Id,
							//	LoadCarrierId = c9.Id,
							//	LoadCarrierResourceLabel = c9.ResourceLabel,
							//	LoadCarrierStatus = c9.Status,
							//	LoadCarrierCustomLong2 = c9.CustomLong2,
							//	LoadCarrierHeightClass = c9.HeightClass,
							//	LoadCarrierTypeId = c9.TypeID,

							//	MaterialId = m9.Id,
							//	MaterialNumber = m9.MaterialNumber,
							//	MaterialDescription_1 = m9.MaterialDescription_1,
							//	MaterialCategoryABC = m9.CategoryABC,

							//	StorageShelfId = a9.Id,
							//	StorageShelfStatus = a9.Status,
							//	StorageShelfName = a9.Name,
							//	StorageShelfHeightClass = a9.HeightClass,
							//	StorageShelfCategoryABC = a9.CategoryABC,
							//	StorageShelfAisleNumber = a9.AisleNumber,

							//	InventoryResourceId = i9.Id,
							//	InventoryResourceBatchNumber = i9.BatchNumber,
							//	InventoryResourceProductStatus = i9.ProductStatus,
							//	InventoryResourceExpiryDate = i9.ExpiryDate,
							//	InventoryResourceCustomDate1 = i9.CustomDate1,

							//	InventoryCount = ir9ic,
							//	ReservedForOutfeed = ror9,
							//	ManuallyInfeededMaterial = ir9mim
							//} : null),
							//Shelf10 = ((a10 != null && a10.Id != Guid.Empty) ? new WmsShelfInfo()
							//{
							//	Id = a10.Id,
							//	LoadCarrierId = c10.Id,
							//	LoadCarrierResourceLabel = c10.ResourceLabel,
							//	LoadCarrierStatus = c10.Status,
							//	LoadCarrierCustomLong2 = c10.CustomLong2,
							//	LoadCarrierHeightClass = c10.HeightClass,
							//	LoadCarrierTypeId = c10.TypeID,

							//	MaterialId = m10.Id,
							//	MaterialNumber = m10.MaterialNumber,
							//	MaterialDescription_1 = m10.MaterialDescription_1,
							//	MaterialCategoryABC = m10.CategoryABC,

							//	StorageShelfId = a10.Id,
							//	StorageShelfStatus = a10.Status,
							//	StorageShelfName = a10.Name,
							//	StorageShelfHeightClass = a10.HeightClass,
							//	StorageShelfCategoryABC = a10.CategoryABC,
							//	StorageShelfAisleNumber = a10.AisleNumber,

							//	InventoryResourceId = i10.Id,
							//	InventoryResourceBatchNumber = i10.BatchNumber,
							//	InventoryResourceProductStatus = i10.ProductStatus,
							//	InventoryResourceExpiryDate = i10.ExpiryDate,
							//	InventoryResourceCustomDate1 = i10.CustomDate1,

							//	InventoryCount = ir10ic,
							//	ReservedForOutfeed = ror10,
							//	ManuallyInfeededMaterial = ir10mim
							//} : null)
						};

			var res = query.Select(x => new WmsChannelInfoCombined() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2 }).ToList();
			var res2 = query.Select(x => new WmsChannelInfoCombined() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2, Shelf3 = x.Shelf3, Shelf4 = x.Shelf4, Shelf5 = x.Shelf5, Shelf6 = x.Shelf6, Shelf7 = x.Shelf7, Shelf8 = x.Shelf8/*, Shelf9 = x.Shelf9, Shelf10 = x.Shelf10*/ });

		}

		public class WmsAisle
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int AisleNumber { get; set; }
			public virtual int PlantID { get; set; }
			public virtual string? Name { get; set; }
		}

		public class WmsChannel
		{
			public virtual int Id { get; set; }
			public virtual int AisleID { get; set; }
			public virtual Guid MaterialID { get; set; }
		}

		public class WmsShelfInfo
		{
			public virtual Guid Id { get; set; }
			//Loadcarrier
			public virtual Guid LoadCarrierId { get; set; }
			public string? LoadCarrierResourceLabel { get; set; }
			public int LoadCarrierStatus { get; set; }
			public long? LoadCarrierCustomLong2 { get; set; }
			public int? LoadCarrierHeightClass { get; set; }
			public virtual Guid? LoadCarrierTypeId { get; set; }

			//material
			public virtual Guid MaterialId { get; set; }
			public string? MaterialNumber { get; set; }
			public string? MaterialDescription_1 { get; set; }
			public virtual string? MaterialCategoryABC { get; set; }

			//storageshelf
			public virtual Guid StorageShelfId { get; set; }
			public int StorageShelfStatus { get; set; }
			public string? StorageShelfName { get; set; }
			public int StorageShelfHeightClass { get; set; }
			public string? StorageShelfCategoryABC { get; set; }
			public int StorageShelfAisleNumber { get; set; }

			//inventoryResource
			public virtual Guid InventoryResourceId { get; set; }
			public string? InventoryResourceBatchNumber { get; set; }
			public int InventoryResourceProductStatus { get; set; }
			public DateTime? InventoryResourceExpiryDate { get; set; }
			public DateTime? InventoryResourceCustomDate1 { get; set; }

			public virtual int InventoryCount { get; set; }
			public virtual bool ReservedForOutfeed { get; set; }
			public virtual bool ManuallyInfeededMaterial { get; set; }
		}

		public class WmsChannelInfoCombined
		{
			public virtual int Id { get; set; }
			public virtual WmsChannelInfo? ChannelInfo { get; set; }
			public virtual WmsShelfInfo? Shelf1 { get; set; }
			public virtual WmsShelfInfo? Shelf2 { get; set; }
			public virtual WmsShelfInfo? Shelf3 { get; set; }
			public virtual WmsShelfInfo? Shelf4 { get; set; }
			public virtual WmsShelfInfo? Shelf5 { get; set; }
			public virtual WmsShelfInfo? Shelf6 { get; set; }
			public virtual WmsShelfInfo? Shelf7 { get; set; }
			public virtual WmsShelfInfo? Shelf8 { get; set; }
			public virtual WmsShelfInfo? Shelf9 { get; set; }
			public virtual WmsShelfInfo? Shelf10 { get; set; }
		}

		public class WmsChannelInfo
		{
			public virtual int Id { get; set; }
			public virtual WmsChannel? Channel { get; set; }
			public virtual WmsAisle? Aisle { get; set; }
			public virtual WmsMaterial? Material { get; set; }
		}

		public class WmsInfeedPosition
		{
			public virtual int Id { get; set; }
			public virtual int InfeedAdviceType { get; set; }
			public virtual WmsAisle? Aisle { get; set; }
			public virtual WmsMaterial? Material { get; set; }
		}

		public class WmsInventory
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual int Status { get; set; }
			public virtual Guid ResourceID { get; set; }
			public virtual int ProductStatus { get; set; }
			public virtual string? BatchNumber { get; set; }
			public virtual int BundleUnit { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual Guid MaterialID { get; set; }
			public virtual int? InfeedAdviceID { get; set; }
			public virtual DateTime ExpiryDate { get; set; }
			public virtual DateTime CustomDate1 { get; set; }
		}

		public class WmsMaterial
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual string? MaterialNumber { get; set; }
			public virtual string? MaterialDescription_1 { get; set; }
			public virtual string? MaterialDescription_2 { get; set; }
			public virtual string? MaterialDescription_3 { get; set; }
			public virtual string? CategoryABC { get; set; }
			public virtual string? CategoryCustoms { get; set; }
			public virtual string? CategoryDimensions { get; set; }
			public virtual string? CategoryQuality { get; set; }
			public virtual string? CategoryTemperature { get; set; }
		}

		public class WmsLoadCarrier
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual int Status { get; set; }
			public virtual string? ResourceLabel { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual int CustomLong1 { get; set; }
			public virtual int CustomLong2 { get; set; }
			public virtual int CustomLong3 { get; set; }
			public int HeightClass { get; set; }
			public Guid TypeID { get; set; }
		}

		public class WmsResourceShelf
		{
			public virtual int Id { get; set; }
			public virtual Guid StorageShelfID { get; set; }
			public virtual Guid ResourceID { get; set; }
		}

		public class WmsOutfeedReservation
		{
			public virtual int Id { get; set; }
			public virtual Guid ResourceID { get; set; }
		}

		public class WmsStorageShelf
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual int ChannelID { get; set; }
			public virtual string? Name { get; set; }
			public virtual int Status { get; set; }
			public virtual int AisleNumber { get; set; }
			public virtual string? CategoryABC { get; set; }
			public virtual int HeightClass { get; set; }
			public virtual int DepthCoordinate { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4394")]
		public void ChannelShelfInfoQueryGenerationTime([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using (var s = GetDataContext(context))
			using (s.CreateLocalTable<TimedAisle>())
			using (s.CreateLocalTable<TimedChannel>())
			using (s.CreateLocalTable<TimedInventory>())
			using (s.CreateLocalTable<TimedMaterial>())
			using (s.CreateLocalTable<TimedOutfeedReservation>())
			using (s.CreateLocalTable<TimedLoadCarrier>())
			using (s.CreateLocalTable<TimedStorageShelf>())
			using (s.CreateLocalTable<TimedResourceShelf>())
			using (s.CreateLocalTable<TimedInfeedPosition>())
			{
				var channelQry = s.GetTable<TimedChannel>();
				var inventoryResourceQry = s.GetTable<TimedInventory>().Where(x => x.Status < TimedInventoryStatus.Finished);
				var materialQry = s.GetTable<TimedMaterial>();
				var resourceQry = s.GetTable<TimedLoadCarrier>();

				#region query
				var query = from c in channelQry
							join a in s.GetTable<TimedAisle>() on c.AisleID equals a.Id

							join m in s.GetTable<TimedMaterial>() on c.MaterialID equals m.Id into mn
							from m in mn.DefaultIfEmpty()

							#region Depth 1

							join a1 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 1 } equals new { Id = a1.ChannelID, Depth = a1.DepthCoordinate } into a1n
							from a1 in a1n.DefaultIfEmpty()
							join b1 in s.GetTable<TimedResourceShelf>() on a1.Id equals b1.StorageShelfID into b1n
							from b1 in b1n.DefaultIfEmpty()
							join c1 in s.GetTable<TimedLoadCarrier>() on b1.ResourceID equals c1.Id into c1n
							from c1 in c1n.DefaultIfEmpty()

							join i1 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b1.ResourceID).Id equals i1.Id into i1n
							from i1 in i1n.DefaultIfEmpty()

							join m1 in s.GetTable<TimedMaterial>() on i1.MaterialID equals m1.Id into m1n
							from m1 in m1n.DefaultIfEmpty()

							let ir1ic = inventoryResourceQry.Count(x => x.ResourceID == c1.Id)
							let ir1lm = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && x.ProductStatus > 0)

							let ror1 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c1.Id)
							let ir1mim = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

							#region Depth 2

							join a2 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 2 } equals new { Id = a2.ChannelID, Depth = a2.DepthCoordinate } into a2n
							from a2 in a2n.DefaultIfEmpty()
							join b2 in s.GetTable<TimedResourceShelf>() on a2.Id equals b2.StorageShelfID into b2n
							from b2 in b2n.DefaultIfEmpty()
							join c2 in s.GetTable<TimedLoadCarrier>() on b2.ResourceID equals c2.Id into c2n
							from c2 in c2n.DefaultIfEmpty()

							join i2 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b2.ResourceID).Id equals i2.Id into i2n
							from i2 in i2n.DefaultIfEmpty()

							join m2 in s.GetTable<TimedMaterial>() on i2.MaterialID equals m2.Id into m2n
							from m2 in m2n.DefaultIfEmpty()

							let ir2ic = inventoryResourceQry.Count(x => x.ResourceID == c2.Id)
							let ir2lm = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && x.ProductStatus > 0)

							let ror2 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c2.Id)
							let ir2mim = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

							#region Depth 3

							join a3 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 3 } equals new { Id = a3.ChannelID, Depth = a3.DepthCoordinate } into a3n
							from a3 in a3n.DefaultIfEmpty()
							join b3 in s.GetTable<TimedResourceShelf>() on a3.Id equals b3.StorageShelfID into b3n
							from b3 in b3n.DefaultIfEmpty()
							join c3 in s.GetTable<TimedLoadCarrier>() on b3.ResourceID equals c3.Id into c3n
							from c3 in c3n.DefaultIfEmpty()

							join i3 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b3.ResourceID).Id equals i3.Id into i3n
							from i3 in i3n.DefaultIfEmpty()

							join m3 in s.GetTable<TimedMaterial>() on i3.MaterialID equals m3.Id into m3n
							from m3 in m3n.DefaultIfEmpty()

							let ir3ic = inventoryResourceQry.Count(x => x.ResourceID == c3.Id)
							let ir3lm = inventoryResourceQry.Any(x => x.ResourceID == c3.Id && x.ProductStatus > 0)
							let ror3 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c3.Id)
							let ir3mim = inventoryResourceQry.Any(x => x.ResourceID == c3.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

							#region Depth 4

							join a4 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 4 } equals new { Id = a4.ChannelID, Depth = a4.DepthCoordinate } into a4n
							from a4 in a4n.DefaultIfEmpty()
							join b4 in s.GetTable<TimedResourceShelf>() on a4.Id equals b4.StorageShelfID into b4n
							from b4 in b4n.DefaultIfEmpty()
							join c4 in s.GetTable<TimedLoadCarrier>() on b4.ResourceID equals c4.Id into c4n
							from c4 in c4n.DefaultIfEmpty()

							join i4 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b4.ResourceID).Id equals i4.Id into i4n
							from i4 in i4n.DefaultIfEmpty()

							join m4 in s.GetTable<TimedMaterial>() on i4.MaterialID equals m4.Id into m4n
							from m4 in m4n.DefaultIfEmpty()

							let ir4ic = inventoryResourceQry.Count(x => x.ResourceID == c4.Id)
							let ir4lm = inventoryResourceQry.Any(x => x.ResourceID == c4.Id && x.ProductStatus > 0)
							let ror4 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c4.Id)
							let ir4mim = inventoryResourceQry.Any(x => x.ResourceID == c4.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

							#region Depth 5

							join a5 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 5 } equals new { Id = a5.ChannelID, Depth = a5.DepthCoordinate } into a5n
							from a5 in a5n.DefaultIfEmpty()
							join b5 in s.GetTable<TimedResourceShelf>() on a5.Id equals b5.StorageShelfID into b5n
							from b5 in b5n.DefaultIfEmpty()
							join c5 in s.GetTable<TimedLoadCarrier>() on b5.ResourceID equals c5.Id into c5n
							from c5 in c5n.DefaultIfEmpty()

							join i5 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b5.ResourceID).Id equals i5.Id into i5n
							from i5 in i5n.DefaultIfEmpty()

							join m5 in s.GetTable<TimedMaterial>() on i5.MaterialID equals m5.Id into m5n
							from m5 in m5n.DefaultIfEmpty()

							let ir5ic = inventoryResourceQry.Count(x => x.ResourceID == c5.Id)
							let ir5lm = inventoryResourceQry.Any(x => x.ResourceID == c5.Id && x.ProductStatus > 0)
							let ror5 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c5.Id)
							let ir5mim = inventoryResourceQry.Any(x => x.ResourceID == c5.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

							#region Depth 6

							join a6 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 6 } equals new { Id = a6.ChannelID, Depth = a6.DepthCoordinate } into a6n
							from a6 in a6n.DefaultIfEmpty()
							join b6 in s.GetTable<TimedResourceShelf>() on a6.Id equals b6.StorageShelfID into b6n
							from b6 in b6n.DefaultIfEmpty()
							join c6 in s.GetTable<TimedLoadCarrier>() on b6.ResourceID equals c6.Id into c6n
							from c6 in c6n.DefaultIfEmpty()

							join i6 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b6.ResourceID).Id equals i6.Id into i6n
							from i6 in i6n.DefaultIfEmpty()

							join m6 in s.GetTable<TimedMaterial>() on i6.MaterialID equals m6.Id into m6n
							from m6 in m6n.DefaultIfEmpty()

							let ir6ic = inventoryResourceQry.Count(x => x.ResourceID == c6.Id)
							let ir6lm = inventoryResourceQry.Any(x => x.ResourceID == c6.Id && x.ProductStatus > 0)
							let ror6 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c6.Id)
							let ir6mim = inventoryResourceQry.Any(x => x.ResourceID == c6.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

							#region Depth 7
							
							join a7 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 7 } equals new { Id = a7.ChannelID, Depth = a7.DepthCoordinate } into a7n
							from a7 in a7n.DefaultIfEmpty()
							join b7 in s.GetTable<TimedResourceShelf>() on a7.Id equals b7.StorageShelfID into b7n
							from b7 in b7n.DefaultIfEmpty()
							join c7 in s.GetTable<TimedLoadCarrier>() on b7.ResourceID equals c7.Id into c7n
							from c7 in c7n.DefaultIfEmpty()

							join i7 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b7.ResourceID).Id equals i7.Id into i7n
							from i7 in i7n.DefaultIfEmpty()

							join m7 in s.GetTable<TimedMaterial>() on i7.MaterialID equals m7.Id into m7n
							from m7 in m7n.DefaultIfEmpty()

							let ir7ic = inventoryResourceQry.Count(x => x.ResourceID == c7.Id)
							let ir7lm = inventoryResourceQry.Any(x => x.ResourceID == c7.Id && x.ProductStatus > 0)
							let ror7 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c7.Id)
							let ir7mim = inventoryResourceQry.Any(x => x.ResourceID == c7.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

							#region Depth 8

							join a8 in s.GetTable<TimedStorageShelf>() on new { Id = c.Id, Depth = 8 } equals new { Id = a8.ChannelID, Depth = a8.DepthCoordinate } into a8n
							from a8 in a8n.DefaultIfEmpty()
							join b8 in s.GetTable<TimedResourceShelf>() on a8.Id equals b8.StorageShelfID into b8n
							from b8 in b8n.DefaultIfEmpty()
							join c8 in s.GetTable<TimedLoadCarrier>() on b8.ResourceID equals c8.Id into c8n
							from c8 in c8n.DefaultIfEmpty()

							join i8 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b8.ResourceID).Id equals i8.Id into i8n
							from i8 in i8n.DefaultIfEmpty()

							join m8 in s.GetTable<TimedMaterial>() on i8.MaterialID equals m8.Id into m8n
							from m8 in m8n.DefaultIfEmpty()

							let ir8ic = inventoryResourceQry.Count(x => x.ResourceID == c8.Id)
							let ir8lm = inventoryResourceQry.Any(x => x.ResourceID == c8.Id && x.ProductStatus > 0)
							let ror8 = s.GetTable<TimedOutfeedReservation>().Any(x => x.ResourceID == c8.Id)
							let ir8mim = inventoryResourceQry.Any(x => x.ResourceID == c8.Id && (x.InfeedAdviceID == null || s.GetTable<TimedInfeedPosition>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == TimedInfeedAdviceType.ManualGenerated)))

							#endregion

						   
							#endregion

							select new TimedChannelInfoCombined
							{
								Id = c.Id,
								ChannelInfo = new TimedChannelInfo
								{
									Id = c.Id,
									Channel = c,
									Aisle = new TimedAisle() { StrippedDownDTO = true, Id = a.Id, AisleNumber = a.AisleNumber, PlantID = a.PlantID, Name = a.Name },
									Material = new TimedMaterial { StrippedDownDTO = true, Id = m.Id, MaterialNumber = m.MaterialNumber, MaterialDescription_1 = m.MaterialDescription_1, MaterialDescription_2 = m.MaterialDescription_2, MaterialDescription_3 = m.MaterialDescription_3, CategoryABC = m.CategoryABC, CategoryCustoms = m.CategoryCustoms, CategoryDimensions = m.CategoryDimensions, CategoryQuality = m.CategoryQuality, CategoryTemperature = m.CategoryTemperature },
								},
								Shelf1 = ((a1 != null && a1.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a1.Id,
									LoadCarrierId = c1.Id,
									LoadCarrierResourceLabel = c1.ResourceLabel,
									LoadCarrierStatus = c1.Status,
									LoadCarrierCustomLong2 = c1.CustomLong2,
									LoadCarrierHeightClass = c1.HeightClass,
									LoadCarrierTypeId = c1.TypeID,

									MaterialId = m1.Id,
									MaterialNumber = m1.MaterialNumber,
									MaterialDescription_1 = m1.MaterialDescription_1,
									MaterialCategoryABC = m1.CategoryABC,

									StorageShelfId = a1.Id,
									StorageShelfStatus = a1.Status,
									StorageShelfName = a1.Name,
									StorageShelfHeightClass = a1.HeightClass,
									StorageShelfCategoryABC = a1.CategoryABC,
									StorageShelfAisleNumber = a1.AisleNumber,

									InventoryResourceId = i1.Id,
									InventoryResourceBatchNumber = i1.BatchNumber,
									InventoryResourceProductStatus = i1.ProductStatus,
									InventoryResourceExpiryDate = i1.ExpiryDate,
									InventoryResourceCustomDate1 = i1.CustomDate1,

									InventoryCount = ir1ic,
									ReservedForOutfeed = ror1,
									ManuallyInfeededMaterial = ir1mim
								}: null),
								Shelf2 = ((a2 != null && a2.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a2.Id,
									LoadCarrierId = c2.Id,
									LoadCarrierResourceLabel = c2.ResourceLabel,
									LoadCarrierStatus = c2.Status,
									LoadCarrierCustomLong2 = c2.CustomLong2,
									LoadCarrierHeightClass = c2.HeightClass,
									LoadCarrierTypeId = c2.TypeID,

									MaterialId = m2.Id,
									MaterialNumber = m2.MaterialNumber,
									MaterialDescription_1 = m2.MaterialDescription_1,
									MaterialCategoryABC = m2.CategoryABC,

									StorageShelfId = a2.Id,
									StorageShelfStatus = a2.Status,
									StorageShelfName = a2.Name,
									StorageShelfHeightClass = a2.HeightClass,
									StorageShelfCategoryABC = a2.CategoryABC,
									StorageShelfAisleNumber = a2.AisleNumber,

									InventoryResourceId = i2.Id,
									InventoryResourceBatchNumber = i2.BatchNumber,
									InventoryResourceProductStatus = i2.ProductStatus,
									InventoryResourceExpiryDate = i2.ExpiryDate,
									InventoryResourceCustomDate1 = i2.CustomDate1,

									InventoryCount = ir2ic,
									ReservedForOutfeed = ror2,
									ManuallyInfeededMaterial = ir2mim
								} : null),
								Shelf3 = ((a3 != null && a3.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a3.Id,
									LoadCarrierId = c3.Id,
									LoadCarrierResourceLabel = c3.ResourceLabel,
									LoadCarrierStatus = c3.Status,
									LoadCarrierCustomLong2 = c3.CustomLong2,
									LoadCarrierHeightClass = c3.HeightClass,
									LoadCarrierTypeId = c3.TypeID,

									MaterialId = m3.Id,
									MaterialNumber = m3.MaterialNumber,
									MaterialDescription_1 = m3.MaterialDescription_1,
									MaterialCategoryABC = m3.CategoryABC,

									StorageShelfId = a3.Id,
									StorageShelfStatus = a3.Status,
									StorageShelfName = a3.Name,
									StorageShelfHeightClass = a3.HeightClass,
									StorageShelfCategoryABC = a3.CategoryABC,
									StorageShelfAisleNumber = a3.AisleNumber,

									InventoryResourceId = i3.Id,
									InventoryResourceBatchNumber = i3.BatchNumber,
									InventoryResourceProductStatus = i3.ProductStatus,
									InventoryResourceExpiryDate = i3.ExpiryDate,
									InventoryResourceCustomDate1 = i3.CustomDate1,

									InventoryCount = ir3ic,
									ReservedForOutfeed = ror3,
									ManuallyInfeededMaterial = ir3mim
								} : null),
								Shelf4 = ((a4 != null && a4.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a4.Id,
									LoadCarrierId = c4.Id,
									LoadCarrierResourceLabel = c4.ResourceLabel,
									LoadCarrierStatus = c4.Status,
									LoadCarrierCustomLong2 = c4.CustomLong2,
									LoadCarrierHeightClass = c4.HeightClass,
									LoadCarrierTypeId = c4.TypeID,

									MaterialId = m4.Id,
									MaterialNumber = m4.MaterialNumber,
									MaterialDescription_1 = m4.MaterialDescription_1,
									MaterialCategoryABC = m4.CategoryABC,

									StorageShelfId = a4.Id,
									StorageShelfStatus = a4.Status,
									StorageShelfName = a4.Name,
									StorageShelfHeightClass = a4.HeightClass,
									StorageShelfCategoryABC = a4.CategoryABC,
									StorageShelfAisleNumber = a4.AisleNumber,

									InventoryResourceId = i4.Id,
									InventoryResourceBatchNumber = i4.BatchNumber,
									InventoryResourceProductStatus = i4.ProductStatus,
									InventoryResourceExpiryDate = i4.ExpiryDate,
									InventoryResourceCustomDate1 = i4.CustomDate1,

									InventoryCount = ir4ic,
									ReservedForOutfeed = ror4,
									ManuallyInfeededMaterial = ir4mim
								} : null),
								Shelf5 = ((a5 != null && a5.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a5.Id,
									LoadCarrierId = c5.Id,
									LoadCarrierResourceLabel = c5.ResourceLabel,
									LoadCarrierStatus = c5.Status,
									LoadCarrierCustomLong2 = c5.CustomLong2,
									LoadCarrierHeightClass = c5.HeightClass,
									LoadCarrierTypeId = c5.TypeID,

									MaterialId = m5.Id,
									MaterialNumber = m5.MaterialNumber,
									MaterialDescription_1 = m5.MaterialDescription_1,
									MaterialCategoryABC = m5.CategoryABC,

									StorageShelfId = a5.Id,
									StorageShelfStatus = a5.Status,
									StorageShelfName = a5.Name,
									StorageShelfHeightClass = a5.HeightClass,
									StorageShelfCategoryABC = a5.CategoryABC,
									StorageShelfAisleNumber = a5.AisleNumber,

									InventoryResourceId = i5.Id,
									InventoryResourceBatchNumber = i5.BatchNumber,
									InventoryResourceProductStatus = i5.ProductStatus,
									InventoryResourceExpiryDate = i5.ExpiryDate,
									InventoryResourceCustomDate1 = i5.CustomDate1,

									InventoryCount = ir5ic,
									ReservedForOutfeed = ror5,
									ManuallyInfeededMaterial = ir5mim
								} : null),
								Shelf6 = ((a6 != null && a6.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a6.Id,
									LoadCarrierId = c6.Id,
									LoadCarrierResourceLabel = c6.ResourceLabel,
									LoadCarrierStatus = c6.Status,
									LoadCarrierCustomLong2 = c6.CustomLong2,
									LoadCarrierHeightClass = c6.HeightClass,
									LoadCarrierTypeId = c6.TypeID,

									MaterialId = m6.Id,
									MaterialNumber = m6.MaterialNumber,
									MaterialDescription_1 = m6.MaterialDescription_1,
									MaterialCategoryABC = m6.CategoryABC,

									StorageShelfId = a6.Id,
									StorageShelfStatus = a6.Status,
									StorageShelfName = a6.Name,
									StorageShelfHeightClass = a6.HeightClass,
									StorageShelfCategoryABC = a6.CategoryABC,
									StorageShelfAisleNumber = a6.AisleNumber,

									InventoryResourceId = i6.Id,
									InventoryResourceBatchNumber = i6.BatchNumber,
									InventoryResourceProductStatus = i6.ProductStatus,
									InventoryResourceExpiryDate = i6.ExpiryDate,
									InventoryResourceCustomDate1 = i6.CustomDate1,

									InventoryCount = ir6ic,
									ReservedForOutfeed = ror6,
									ManuallyInfeededMaterial = ir6mim
								} : null),
								Shelf7 = ((a7 != null && a7.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a7.Id,
									LoadCarrierId = c7.Id,
									LoadCarrierResourceLabel = c7.ResourceLabel,
									LoadCarrierStatus = c7.Status,
									LoadCarrierCustomLong2 = c7.CustomLong2,
									LoadCarrierHeightClass = c7.HeightClass,
									LoadCarrierTypeId = c7.TypeID,

									MaterialId = m7.Id,
									MaterialNumber = m7.MaterialNumber,
									MaterialDescription_1 = m7.MaterialDescription_1,
									MaterialCategoryABC = m7.CategoryABC,

									StorageShelfId = a7.Id,
									StorageShelfStatus = a7.Status,
									StorageShelfName = a7.Name,
									StorageShelfHeightClass = a7.HeightClass,
									StorageShelfCategoryABC = a7.CategoryABC,
									StorageShelfAisleNumber = a7.AisleNumber,

									InventoryResourceId = i7.Id,
									InventoryResourceBatchNumber = i7.BatchNumber,
									InventoryResourceProductStatus = i7.ProductStatus,
									InventoryResourceExpiryDate = i7.ExpiryDate,
									InventoryResourceCustomDate1 = i7.CustomDate1,

									InventoryCount = ir7ic,
									ReservedForOutfeed = ror7,
									ManuallyInfeededMaterial = ir7mim
								} : null),
								Shelf8 = ((a8 != null && a8.Id != Guid.Empty) ? new TimedShelfInfo()
								{
									Id = a8.Id,
									LoadCarrierId = c8.Id,
									LoadCarrierResourceLabel = c8.ResourceLabel,
									LoadCarrierStatus = c8.Status,
									LoadCarrierCustomLong2 = c8.CustomLong2,
									LoadCarrierHeightClass = c8.HeightClass,
									LoadCarrierTypeId = c8.TypeID,

									MaterialId = m8.Id,
									MaterialNumber = m8.MaterialNumber,
									MaterialDescription_1 = m8.MaterialDescription_1,
									MaterialCategoryABC = m8.CategoryABC,

									StorageShelfId = a8.Id,
									StorageShelfStatus = a8.Status,
									StorageShelfName = a8.Name,
									StorageShelfHeightClass = a8.HeightClass,
									StorageShelfCategoryABC = a8.CategoryABC,
									StorageShelfAisleNumber = a8.AisleNumber,

									InventoryResourceId = i8.Id,
									InventoryResourceBatchNumber = i8.BatchNumber,
									InventoryResourceProductStatus = i8.ProductStatus,
									InventoryResourceExpiryDate = i8.ExpiryDate,
									InventoryResourceCustomDate1 = i8.CustomDate1,

									InventoryCount = ir8ic,
									ReservedForOutfeed = ror8,
									ManuallyInfeededMaterial = ir8mim
								} : null),
							};

				var i = new QueryStartedInterceptor();
				s.AddInterceptor(i);
				var sw = Stopwatch.StartNew();
				var sw2 = Stopwatch.StartNew();
				i.Action = () => sw.Stop();
				var res = query.Select(x => new TimedChannelInfoCombined() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1 }).ToList();
				var gt1 = sw2.ElapsedMilliseconds;
				var t1 = sw.ElapsedMilliseconds;

				sw = Stopwatch.StartNew();
				sw2 = Stopwatch.StartNew();
				i.Action = () => sw.Stop();
				var res8 = query.Select(x => new TimedChannelInfoCombined() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2, Shelf3 = x.Shelf3, Shelf4 = x.Shelf4, Shelf5 = x.Shelf5, Shelf6 = x.Shelf6, Shelf7 = x.Shelf7, Shelf8 = x.Shelf8 }).ToList();
				var gt8 = sw2.ElapsedMilliseconds;
				var t8 = sw.ElapsedMilliseconds;
			}
		}

		[Table]
		public class TimedAisle
		{
			[Column] public int Id { get; set; }
			[Column] public int AisleNumber { get; set; }
			[Column] public int PlantID { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public bool StrippedDownDTO { get; set; }
		}

		[Table]
		public class TimedChannel
		{
			[Column] public int Id { get; set; }

			[Column] public int AisleID { get; set; }

			[Column] public int MaterialID { get; set; }
		}

		public enum TimedInventoryStatus
		{
			Finished
		}

		[Table]
		public class TimedInventory
		{
			[Column] public int Id { get; set; }
			[Column] public int? InfeedAdviceID { get; set; }
			[Column] public int Quantity { get; set; }
			[Column] public int ResourceID { get; set; }
			[Column] public int MaterialID { get; set; }
			[Column] public int ProductStatus { get; set; }
			[Column] public string? BatchNumber { get; set; }
			[Column] public DateTime? ExpiryDate { get; set; }
			[Column] public DateTime? CustomDate1 { get; set; }
			[Column] public TimedInventoryStatus Status { get; set; }
		}

		[Table]
		public class TimedMaterial
		{
			[Column] public int Id { get; set; }
			[Column] public int MaterialNumber { get; set; }
			[Column] public bool StrippedDownDTO { get; set; }
			[Column] public string? MaterialDescription_1 { get; set; }
			[Column] public string? MaterialDescription_2 { get; set; }
			[Column] public string? MaterialDescription_3 { get; set; }
			[Column] public string? CategoryABC { get; set; }
			[Column] public string? CategoryCustoms { get; set; }
			[Column] public string? CategoryDimensions { get; set; }
			[Column] public string? CategoryQuality { get; set; }
			[Column] public string? CategoryTemperature { get; set; }
		}

		[Table]
		public class TimedOutfeedReservation
		{
			[Column] public int Id { get; set; }

			[Column] public int ResourceID { get; set; }
		}

		[Table]
		public class TimedLoadCarrier
		{
			[Column] public int Id { get; set; }
			[Column] public string? ResourceLabel { get; set; }
			[Column] public int Status { get; set; }
			[Column] public long CustomLong2 { get; set; }
			[Column] public int HeightClass { get; set; }
			[Column] public int TypeID { get; set; }
		}

		[Table]
		public class TimedStorageShelf
		{
			[Column] public Guid Id { get; set; }
			[Column] public int ChannelID { get; set; }
			[Column] public int DepthCoordinate { get; set; }
			[Column] public int Status { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public int HeightClass { get; set; }
			[Column] public string? CategoryABC { get; set; }
			[Column] public int AisleNumber { get; set; }
		}

		[Table]
		public class TimedResourceShelf
		{
			[Column] public int Id { get; set; }

			[Column] public Guid StorageShelfID { get; set; }

			[Column] public int ResourceID { get; set; }
		}

		public enum TimedInfeedAdviceType
		{
			ManualGenerated
		}

		[Table]
		public class TimedInfeedPosition
		{
			[Column] public int Id { get; set; }

			[Column] public int Nr { get; set; }

			[Column] public TimedInfeedAdviceType InfeedAdviceType { get; set; }
		}

		public class TimedChannelInfo
		{
			public int Id { get; set; }
			public TimedChannel? Channel { get; set; }
			public TimedAisle? Aisle { get; set; }
			public TimedMaterial? Material { get; set; }
		}

		public class TimedShelfInfo
		{
			public Guid Id { get; set; }
			public int LoadCarrierId { get; set; }
			public string? LoadCarrierResourceLabel { get; set; }
			public int LoadCarrierStatus { get; set; }
			public long LoadCarrierCustomLong2 { get; set; }
			public int LoadCarrierHeightClass { get; set; }
			public int LoadCarrierTypeId { get; set; }
			public int MaterialId { get; set; }
			public int MaterialNumber { get; set; }
			public string? MaterialDescription_1 { get; set; }
			public string? MaterialCategoryABC { get; set; }
			public Guid StorageShelfId { get; set; }
			public int StorageShelfStatus { get; set; }
			public string? StorageShelfName { get; set; }
			public int StorageShelfHeightClass { get; set; }
			public string? StorageShelfCategoryABC { get; set; }
			public int StorageShelfAisleNumber { get; set; }
			public int InventoryResourceId { get; set; }
			public string? InventoryResourceBatchNumber { get; set; }
			public int InventoryResourceProductStatus { get; set; }
			public DateTime? InventoryResourceExpiryDate { get; set; }
			public DateTime? InventoryResourceCustomDate1 { get; set; }
			public int InventoryCount { get; set; }
			public bool ReservedForOutfeed { get; set; }
			public bool ManuallyInfeededMaterial { get; set; }
		}

		public class TimedChannelInfoCombined
		{
			public int Id { get; set; }
			public TimedChannelInfo? ChannelInfo { get; set; }
			public TimedShelfInfo? Shelf1 { get; set; }
			public TimedShelfInfo? Shelf2 { get; set; }
			public TimedShelfInfo? Shelf3 { get; set; }
			public TimedShelfInfo? Shelf4 { get; set; }
			public TimedShelfInfo? Shelf5 { get; set; }
			public TimedShelfInfo? Shelf6 { get; set; }
			public TimedShelfInfo? Shelf7 { get; set; }
			public TimedShelfInfo? Shelf8 { get; set; }
		}

		class QueryStartedInterceptor : LinqToDB.Interceptors.CommandInterceptor
		{
			public Action? Action { get; set; }

			public override Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
			{
				Action?.Invoke();
				return base.ExecuteReader(eventData, command, commandBehavior, result);
			}
		}
	}
}
