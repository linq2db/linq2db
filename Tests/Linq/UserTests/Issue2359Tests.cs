using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2359Tests : TestBase
	{
		[Table]
		public class Issue2358Tag
		{
			[Column]
			public string? Name { get; set; }

			[Column]
			public Guid Id { get; set; }
		}

		public class Issue2359TagArchive
		{
			[Column]
			public Guid TagId { get; set; }

			[Column]
			public decimal TagValue { get; set; }

			[Column]
			public DateTime ModifiedTimeStamp { get; set; }
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			var today = DateTime.Today;
			var now = today.AddHours(5);

			using (var db = GetDataContext(context, ms))
			using (var t = db.CreateLocalTable<Issue2358Tag>())
			using (var t2 = db.CreateLocalTable<Issue2358TagArchive>())
			{
				var id=Guid.NewGuid();
				t.Insert(() => new Issue2358Tag() { Name = "SRM.MDYNAMIC.ServiceData.General.OperatingTimeInAutomaticMode", Id =id });
				t2.Insert(()=> new Issue2358TagArchive() { TagId = id, TagValue = 0, ModifiedTimeStamp = today.AddMinutes(-1) });
				t2.Insert(()=> new Issue2358TagArchive() { TagId = id, TagValue = 10000, ModifiedTimeStamp = now.AddMinutes(-1) });
				var qry = from tag in t
						  where tag.Name.EndsWith(".ServiceData.General.OperatingTimeInAutomaticMode")
						  select new
						  {
							  Name = tag.Name.Substring(tag.Name.IndexOf('.', tag.Name.IndexOf('.', 5) + 1) + 1),
							  Srm = tag.Name.Substring(4, tag.Name.IndexOf('.', 5) - 4),
							  ValueName = tag.Name,
							  Value = t2.Where(x => x.TagId == tag.Id && x.ModifiedTimeStamp < now).OrderBy(x => x.ModifiedTimeStamp).Select(x => x.TagValue).FirstOrDefault()
									- t2.Where(x => x.TagId == tag.Id && x.ModifiedTimeStamp < today).OrderBy(x => x.ModifiedTimeStamp).Select(x => x.TagValue).FirstOrDefault()
						  };
				var result = qry.ToList();

				var sql=((TestDataConnection)db).LastQuery;
			}
		}
	}
}
