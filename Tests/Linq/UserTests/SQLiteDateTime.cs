using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		class A
		{
			[PrimaryKey, Identity] public int       ID       { get; set; }
			[Column,     NotNull ] public string    Value    { get; set; }
			[Column,     NotNull ] public DateTime  DateTime { get; set; }
		}

		class B
		{
			public int    ID;
			public string Name;
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
			using (var db = GetDataContext(context))
			{
				var matchSymbolIds = new List<int>();

				var queryable = GenerateQuery(db, new DateTime(2010, 3, 5)).Where(x => matchSymbolIds.Contains(x.ID));
				return queryable.ToString();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLite)]
		public void TestSql(string context)
		{
			var query1 = GetSql(context);
			var query2 = GetSql(context);
			var query3 = GetSql(context);

			Debug.WriteLine(query1);
			Debug.WriteLine(query2);
			Debug.WriteLine(query3);

			Assert.AreEqual(query1, query2);
		}
	}
}
