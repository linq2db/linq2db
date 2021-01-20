using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2785Tests : TestBase
	{
		[Table]
		class TableA
		{
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column(CanBeNull = false)]
			public string? NameA { get; set; }
		}

		[Table]
		class TableB
		{
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column(CanBeNull = false)]
			public string? NameB { get; set; }
		}

		[Test]
		public void Issue2785Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TableA>())
				using (db.CreateLocalTable<TableB>())
				{
					var query = from a in db.GetTable<TableA>()
								join b in  db.GetTable<TableB>() on a.Id equals b.Id
								select new { Id=a.Id, Id2=b.Id };

					var res = query.Take(10).ToList();

					var sql = ((DataConnection) db).LastQuery;
				}
			}
		}
	}
}
