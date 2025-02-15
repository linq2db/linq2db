using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class SQLiteDateTime : TestBase
	{
		[Table]
		sealed class A
		{
			[PrimaryKey, Identity] public int       ID       { get; set; }
			[Column,     NotNull ] public string    Value    { get; set; } = null!;
			[Column,     NotNull ] public DateTime  DateTime { get; set; }
		}

		sealed class B
		{
			public int     ID;
			public string? Name;
		}

		static IQueryable<B> GenerateQuery(ITestDataContext db, DateTime? asOfDate = null)
		{
			var q =
				from identifier in db.GetTable<A>()
				where identifier.DateTime <= asOfDate
				select identifier;

			return
				from a in db.GetTable<A>()
				select new B
				{
					ID   = a.ID,
					Name = q.Select(identifier => identifier.Value).FirstOrDefault()
				};
		}

		string GetSql(string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<A>();

			var matchSymbolIds = new List<int>();

			var queryable = GenerateQuery(db, new DateTime(2010, 3, 5)).Where(x => matchSymbolIds.Contains(x.ID));
			return queryable.ToSqlQuery().Sql;
		}

		[Test]
		public void TestSql([IncludeDataSources(TestProvName.AllSQLiteClassic)] string context)
		{
			var query1 = GetSql(context);
			var query2 = GetSql(context);
			var query3 = GetSql(context);

			Assert.That(query2, Is.EqualTo(query1));
		}
	}
}
