using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2356Tests : TestBase
	{
		[Table]
		public class Issue2356Table
		{
			[Column]
			public DateTime DateTime { get; set; }
		}

		static DateTime SetKind(DateTime dt) 
			=> DateTime.SpecifyKind(dt, DateTimeKind.Utc);

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<DateTime, DateTime>(dt => SetKind(dt));
			ms.SetConvertExpression<DateTime, DataParameter>(dt => new DataParameter() { Value = SetKind(dt), DataType = DataType.DateTime });
			
			using (var db = GetDataContext(context, ms))
			using (var t = db.CreateLocalTable<Issue2356Table>())
			{
				t.Insert(() => new Issue2356Table()
				{
					DateTime = DateTime.UtcNow
				});

				var record = t.Single();

				Assert.AreEqual(DateTimeKind.Utc, record.DateTime.Kind);
			}
		}
	}
}
