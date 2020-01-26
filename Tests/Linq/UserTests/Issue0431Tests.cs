using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;

namespace Tests.UserTests
{
	public class Issue0431Tests : TestBase
	{
		[Table("Test0431")]
		public partial class TestTable
		{
			[Column(DataType =  DataType.Date)]                     public DateTime       Date             { get; set; }
			[Column]                                                public DateTime       DateTime         { get; set; }
			[Column(DataType =  DataType.DateTime)]                 public DateTime       DateTime_        { get; set; }
			[Column(DataType =  DataType.DateTime2)]                public DateTime       DateTime2        { get; set; }
			[Column(DataType =  DataType.DateTime2, Precision = 0)] public DateTime       DateTime2_0      { get; set; }
			[Column(DataType =  DataType.DateTime2, Precision = 1)] public DateTime       DateTime2_1      { get; set; }
			[Column(DataType =  DataType.DateTime2, Precision = 9)] public DateTime       DateTime2_9      { get; set; }
			[Column(DataType =  DataType.DateTimeOffset)]           public DateTime       DateTimeOffset   { get; set; }
			[Column]                                                public DateTimeOffset DateTimeOffset_  { get; set; }
			[Column(Precision = 0)]                                 public DateTimeOffset DateTimeOffset_0 { get; set; }
			[Column(Precision = 1)]                                 public DateTimeOffset DateTimeOffset_1 { get; set; }
			[Column(Precision = 9)]                                 public DateTimeOffset DateTimeOffset_9 { get; set; }

			public static readonly TestTable[] Data = new[]
			{
				new TestTable()
				{
					// for DataType.Date we currently don't trim parameter values of time part
					Date             = new DateTime(2020, 1, 3),
					DateTime         = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234),
					DateTime_        = new DateTime(2020, 1, 3, 4, 5, 6),
					DateTime2        = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234),
					DateTime2_0      = new DateTime(2020, 1, 3, 4, 5, 6, 189).AddTicks(1234),
					DateTime2_1      = new DateTime(2020, 1, 3, 4, 5, 6, 719).AddTicks(1234),
					DateTime2_9      = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234),
					DateTimeOffset   = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234),
					DateTimeOffset_  = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234),
					DateTimeOffset_0 = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 189, TimeSpan.FromMinutes(45)).AddTicks(1234),
					DateTimeOffset_1 = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 719, TimeSpan.FromMinutes(45)).AddTicks(1234),
					DateTimeOffset_9 = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234)
				}
			};
		}

		[Test]
		public void Test([IncludeDataSources(true, TestProvName.AllOracle)] string context, [Values] bool inlineParameters)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				db.Insert(TestTable.Data[0]);

				db.InlineParameters = inlineParameters;

				var pDate           = new DateTime(2020, 1, 3);
				var pDateTime       = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234);
				var pDateTimeOffset = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234);

				var results = table.Where(r => r.Date             == pDate          ).ToArray(); assert();
				results     = table.Where(r => r.DateTime         == pDateTime      ).ToArray(); assert();
				results     = table.Where(r => r.DateTime_        == pDateTime      ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2        == pDateTime      ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2_0      == pDateTime      ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2_1      == pDateTime      ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2_9      == pDateTime      ).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset   == pDateTimeOffset).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_  == pDateTimeOffset).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_0 == pDateTimeOffset).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_1 == pDateTimeOffset).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_9 == pDateTimeOffset).ToArray(); assert();

				void assert()
				{
					Assert.AreEqual(1, results.Length);

					Assert.AreEqual(new DateTime(2020, 1, 3), results[0].Date);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6), results[0].DateTime_);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime2);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 0), results[0].DateTime2_0);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 700), results[0].DateTime2_1);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234), results[0].DateTime2_9);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTimeOffset);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1230), results[0].DateTimeOffset_);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 0, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_0);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 700, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_1);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234), results[0].DateTimeOffset_9);
				}
			}
		}

		[Test]
		public void TestSQL([IncludeDataSources(false, TestProvName.AllOracle)] string context, [Values] bool inlineParameters)
		{
			using (var db = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				Assert.True(db.LastQuery.Contains("\"Date\"           date                        NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTime         timestamp                   NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTime_        date                        NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTime2        timestamp                   NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTime2_0      timestamp(0)                NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTime2_1      timestamp(1)                NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTime2_9      timestamp(9)                NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTimeOffset   timestamp with time zone    NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTimeOffset_  timestamp with time zone    NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTimeOffset_0 timestamp(0) with time zone NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTimeOffset_1 timestamp(1) with time zone NOT NULL"));
				Assert.True(db.LastQuery.Contains("DateTimeOffset_9 timestamp(9) with time zone NOT NULL"));

				db.Insert(TestTable.Data[0]);

				db.InlineParameters = inlineParameters;

				var pDate           = new DateTime(2020, 1, 3);
				var pDateTime       = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234);
				var pDateTimeOffset = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234);

				var results = table.Where(r => r.Date == pDate).ToArray();
				assert("TO_DATE('2020-01-03', 'YYYY-MM-DD')");

				results = table.Where(r => r.DateTime == pDateTime).ToArray();
				assert("TO_TIMESTAMP('2020-01-03 04:05:06.789123', 'YYYY-MM-DD HH24:MI:SS.FF6')");

				results = table.Where(r => r.DateTime_ == pDateTime).ToArray();
				assert("TO_DATE('2020-01-03 04:05:06', 'YYYY-MM-DD HH24:MI:SS')");

				results = table.Where(r => r.DateTime2 == pDateTime).ToArray();
				assert("TO_TIMESTAMP('2020-01-03 04:05:06.789123', 'YYYY-MM-DD HH24:MI:SS.FF6')");

				results = table.Where(r => r.DateTime2_0 == pDateTime).ToArray();
				assert("TO_TIMESTAMP('2020-01-03 04:05:06', 'YYYY-MM-DD HH24:MI:SS')");

				results = table.Where(r => r.DateTime2_1 == pDateTime).ToArray();
				assert("TO_TIMESTAMP('2020-01-03 04:05:06.7', 'YYYY-MM-DD HH24:MI:SS.FF1')");

				results = table.Where(r => r.DateTime2_9 == pDateTime).ToArray();
				assert("TO_TIMESTAMP('2020-01-03 04:05:06.7891234', 'YYYY-MM-DD HH24:MI:SS.FF7')");

				results = table.Where(r => r.DateTimeOffset == pDateTimeOffset).ToArray();
				assert("TO_TIMESTAMP_TZ('2020-01-03 03:20:06.789123 00:00', 'YYYY-MM-DD HH24:MI:SS.FF6 TZH:TZM')");

				results = table.Where(r => r.DateTimeOffset_ == pDateTimeOffset).ToArray();
				assert("TO_TIMESTAMP_TZ('2020-01-03 03:20:06.789123 00:00', 'YYYY-MM-DD HH24:MI:SS.FF6 TZH:TZM')");

				results = table.Where(r => r.DateTimeOffset_0 == pDateTimeOffset).ToArray();
				assert("TO_TIMESTAMP_TZ('2020-01-03 03:20:06 00:00', 'YYYY-MM-DD HH24:MI:SS TZH:TZM')");

				results = table.Where(r => r.DateTimeOffset_1 == pDateTimeOffset).ToArray();
				assert("TO_TIMESTAMP_TZ('2020-01-03 03:20:06.7 00:00', 'YYYY-MM-DD HH24:MI:SS.FF1 TZH:TZM')");

				results = table.Where(r => r.DateTimeOffset_9 == pDateTimeOffset).ToArray();
				assert("TO_TIMESTAMP_TZ('2020-01-03 03:20:06.7891234 00:00', 'YYYY-MM-DD HH24:MI:SS.FF7 TZH:TZM')");

				void assert(string function)
				{
					Assert.AreEqual(1, results.Length);

					Assert.AreEqual(new DateTime(2020, 1, 3), results[0].Date);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6), results[0].DateTime_);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime2);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 0), results[0].DateTime2_0);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 700), results[0].DateTime2_1);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234), results[0].DateTime2_9);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTimeOffset);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1230), results[0].DateTimeOffset_);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 0, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_0);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 700, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_1);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234), results[0].DateTimeOffset_9);

					Assert.True(db.LastQuery.Contains(function));
				}
			}
		}
	}
}
