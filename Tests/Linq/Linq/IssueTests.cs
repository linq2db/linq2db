using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class IssueTests : TestBase
	{
		// https://github.com/linq2db/linq2db/issues/38
		//
		[Test, DataContextSource(false)]
		public void Issue38Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from a in Child
					select new { Count = a.GrandChildren.Count() },
					from a in db.Child
					select new { Count = a.GrandChildren1.Count() });

				var sql = ((TestDataConnection)db).LastQuery;

				Assert.That(sql, Is.Not.Contains("INNER JOIN"));

				Debug.WriteLine(sql);
			}
		}

		// https://github.com/linq2db/linq2db/issues/42
		//
		[Test, DataContextSource()]
		public void Issue42Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var t1 = db.Types2.First();

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);

				var t2 = db.Types2.First();

				Assert.That(t2.BoolValue, Is.EqualTo(t1.BoolValue));

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);
			}
		}
#if !NETSTANDARD
		// https://github.com/linq2db/linq2db/issues/60
		//
		[Test, IncludeDataContextSource(
			ProviderName.SqlServer2000,
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2012,
			ProviderName.SqlServer2014,
			TestProvName.SqlAzure,
			ProviderName.SqlCe)]
		public void Issue60Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var sp       = db.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(db);

				var q =
					from t in dbSchema.Tables
					from c in t.Columns
					where c.ColumnType.StartsWith("tinyint") && c.MemberType.StartsWith("sbyte")
					select c;

				var column = q.FirstOrDefault();

				Assert.That(column, Is.Null);
			}
		}
#endif
		// https://github.com/linq2db/linq2db/issues/67
		//
		[Test, DataContextSource()]
		public void Issue67Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID into ch
					select new { p.ParentID, count = ch.Count() } into t
					where t.count > 0
					select t,
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into ch
					select new { p.ParentID, count = ch.Count() } into t
					where t.count > 0
					select t);
			}
		}

		[Test, DataContextSource]
		public void Issue424Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.Distinct().OrderBy(_ => _.ParentID).Take(1),
					db.Parent.Distinct().OrderBy(_ => _.ParentID).Take(1)
					);
			}
		}

		[Test, DataContextSource]
		public void Issue424Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.Distinct().OrderBy(_ => _.ParentID).Skip(1).Take(1),
					db.Parent.Distinct().OrderBy(_ => _.ParentID).Skip(1).Take(1)
					);
			}
		}

		// https://github.com/linq2db/linq2db/issues/461
		//
		[Test, DataContextSource()]
		public void Issue461Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 = from sep in db.Parent
					select new
					{
						Child =
						(from l in db.Child
							select new
							{
								Id = l.ParentID + 1
							}).FirstOrDefault()
					};
				
				var q2 = from sep in Parent
					select new
					{
						Child =
						(from l in Child
							select new
							{
								Id = l.ParentID + 1
							}).FirstOrDefault()
					};

				AreEqual(q1, q2);
			}
		}
	}
}
