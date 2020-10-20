using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2564Tests : TestBase
	{
		class Issue2564Class
		{
			public long Id { get; set; }
			public DateTime TimestampGenerated { get; set; }
			public DateTime? TimestampGone { get; set; }
			public string? MessageClassName { get; set; }
			public string? ExternID1 { get; set; }
			public string? TranslatedMessageGroup { get; set; }
			public string? TranslatedMessage1 { get; set; }
		}

		[Test]
		public void TestContainerParameter([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<Issue2564Class>()
				.HasTableName("Common_Scripts")
				.Property(e => e.Id).IsPrimaryKey();


			using (var db = GetDataContext(context, ms))
			{
				var from = DateTime.UtcNow.AddDays(-1);
				var to = DateTime.UtcNow;
				var qry = (from m in db.GetTable<Issue2564Class>()
						   where m.TimestampGone.HasValue &&
							 m.TimestampGenerated >= @from &&
							 m.TimestampGenerated <= to &&
							 m.MessageClassName == "Error"
						   group m by new { m.ExternID1, m.TranslatedMessageGroup, m.TimestampGenerated.Hour } into tgGroup
						   select new
						   {
							   MessageText = tgGroup.Min(x => x.TranslatedMessage1).Trim(),
							   GroupName = tgGroup.Key.TranslatedMessageGroup,
							   Hour = tgGroup.Key.Hour,
							   Count = tgGroup.Count(),
							   DurationInSeconds = (long)tgGroup.Sum(x => (x.TimestampGone.Value - x.TimestampGenerated).TotalMilliseconds),
						   });
				var data = qry.ToList();
			}
		}
	}
}
