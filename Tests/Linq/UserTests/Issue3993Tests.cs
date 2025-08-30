using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

using DataType = LinqToDB.DataType;

namespace Tests.UserTests.Test3993
{
	[TestFixture]
	public class Test3993Tests : TestBase
	{
		public class Test
		{
			public virtual DateTime? StartDateTime    { get; set; }
			public virtual DateTime StartDateTime2    { get; set; }
			public virtual DateTime? EndDateTime      { get; set; }
			public virtual TimeSpan? PreNotification  { get; set; }
			public virtual TimeSpan? PreNotification2 { get; set; }
			public virtual TimeSpan  PreNotification3 { get; set; }
			public virtual DateTime? StrField         { get; set; }
			
			public virtual string? Status { get; set; }
		}

		[Test]
		public void TestIssue3993_Test1([IncludeDataSources(TestProvName.AllSqlServer2019, TestProvName.AllSqlServer2016Plus, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllFirebird3Plus, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllSapHana)] string configuration)
		{
			MappingSchema ms;
			Model.ITestDataContext? db = null;
			try
			{
				if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle") || configuration.Contains("Informix"))
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Interval)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Interval)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
				}
				else
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Int64)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
					ms.AddScalarType(typeof(TimeSpan?), DataType.Int64);
				}

				LinqToDB.Linq.Expressions.AddTimeSpanMappings();

				db = GetDataContext(configuration, ms);

				using var tbl = db.CreateLocalTable(new[]
				{
					new Test
					{
						StartDateTime    = TestData.DateTime4Utc,
						StartDateTime2    = TestData.DateTime4Utc,
						EndDateTime      = TestData.DateTime4Utc.AddHours(4),
						PreNotification  = TimeSpan.FromSeconds(20000),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					}
				});

				var qryAA =
				(from t in db.GetTable<Test>()
				select new
				{
					NotificationDateTime5 = t.StartDateTime - t.PreNotification,
				}).ToList();

				var d = db.GetTable<Test>().ToList();

				var d2 = db.GetTable<Test>().Where(x=>x.StartDateTime2.Year == 2023).ToList();
				var d3 = db.GetTable<Test>().Where(x=>x.StartDateTime2 + TimeSpan.FromMinutes(5) > DateTime.UtcNow).ToList();
				var d4 = db.GetTable<Test>().Where(x=>x.StartDateTime2 + TimeSpan.FromDays(365 * 100) > DateTime.UtcNow).ToList();

				var qry2 =
				(from t in db.GetTable<Test>()
				 select new
				 {
					 t1 = t.PreNotification!.Value.TotalMilliseconds,
					 t2 = t.PreNotification!.Value.TotalSeconds
				 }).Where(x => x.t2 < x.t1).ToList();

				var qry =
				from t in db.GetTable<Test>()
				select new
				{
					StartDateTime         = t.StartDateTime,
					PreNotification       = t.PreNotification,
					NotificationDateTime  = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification!.Value.TotalMilliseconds, t.StartDateTime),
					NotificationDateTime2 = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification2!.Value.TotalMilliseconds, t.StartDateTime),
					NotificationDateTime3 = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification3.TotalMilliseconds, t.StartDateTime),
					NotificationDateTime4 = t.StartDateTime - t.PreNotification3,
					NotificationDateTime5 = t.StartDateTime - t.PreNotification,
					NotificationDateTime6 = t.StartDateTime + t.PreNotification,
					NotificationDateTime7 = t.StartDateTime2 - t.PreNotification,
					NotificationDateTime8 = t.StartDateTime2 - t.PreNotification3,
					t.StrField!.Value.Day
				};

				var res = qry.Where(x => x.NotificationDateTime < TestData.DateTime4Utc).ToList();
				Assert.That(res, Has.Count.EqualTo(1));
				var res2 = qry.Where(x => x.NotificationDateTime2 < TestData.DateTime4Utc).ToList();
				Assert.That(res2, Has.Count.EqualTo(1));
				var res3 = qry.Where(x => x.NotificationDateTime4 < TestData.DateTime4Utc).ToList();
				Assert.That(res3, Has.Count.EqualTo(1));
				var res31 = qry.Where(x => x.NotificationDateTime5 < TestData.DateTime4Utc).ToList();
				Assert.That(res31, Has.Count.EqualTo(1));
				var res33 = qry.Where(x => x.NotificationDateTime6 < TestData.DateTime4Utc).ToList();
				Assert.That(res33, Has.Count.EqualTo(0));
				var res22 = qry.Where(x => x.NotificationDateTime7 < TestData.DateTime4Utc).ToList();
				Assert.That(res22, Has.Count.EqualTo(1));
				var res11 = qry.Where(x => x.NotificationDateTime8 < TestData.DateTime4Utc).ToList();
				Assert.That(res11, Has.Count.EqualTo(1));

				var qry4 =
				from t in db.GetTable<Test>()
				select new
				{
					NotificationDateTime4 = t.StartDateTime - t.PreNotification3,
				};

				var res4 = qry4.Where(x => x.NotificationDateTime4 < TestData.DateTimeUtc).ToList();
				Assert.That(res3, Has.Count.EqualTo(1));

				var qry5 =
				from t in db.GetTable<Test>()
				select new
				{
					diff = t.EndDateTime - t.StartDateTime,
				};

				var res6 = qry5.ToList();
				Assert.That(res6, Has.Count.EqualTo(1));
				var res21 = qry5.Select(x => x.diff).ToList();
				Assert.That(res21[0]!.Value, Is.LessThanOrEqualTo(TimeSpan.FromHours(4).Add(TimeSpan.FromSeconds(1))));
				Assert.That(res21[0]!.Value, Is.GreaterThanOrEqualTo(TimeSpan.FromHours(4).Add(TimeSpan.FromSeconds(-1))));
				var res5 = qry5.Where(x => x.diff < TimeSpan.FromHours(5)).ToList();
				Assert.That(res5, Has.Count.EqualTo(1));
				var res7 = qry5.Where(x => x.diff!.Value.TotalHours < 5).ToList();
				Assert.That(res7, Has.Count.EqualTo(1));
				var res8 = qry5.Where(x => x.diff < TimeSpan.FromHours(2)).ToList();
				Assert.That(res8, Has.Count.EqualTo(0));
				var res9 = qry5.Where(x => x.diff!.Value.TotalHours < 2).ToList();
				Assert.That(res9, Has.Count.EqualTo(0));
			}
			finally
			{
				db?.Dispose();
			}
		}
		
		[Test]
		public void TestIssue3993_Test2([IncludeDataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllFirebird3Plus, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllSapHana, TestProvName.AllSybase)] string configuration)
		{
			MappingSchema ms;
			Model.ITestDataContext? db = null;
			try
			{
				if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle"))
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Interval)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Interval)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
				}
				else
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Int64)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
				}

				LinqToDB.Linq.Expressions.AddTimeSpanMappings();

				db = GetDataContext(configuration, ms);

				using var tbl = db.CreateLocalTable(new[]
				{
					new Test
					{
						StartDateTime    = TestData.DateTime4Utc,
						StartDateTime2    = TestData.DateTime4Utc,
						EndDateTime      = TestData.DateTime4Utc.AddHours(4),
						PreNotification  = TimeSpan.FromSeconds(20000),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					},
					new Test
					{
						StartDateTime    = new DateTime(2023,10,17, 9,40,23),
						StartDateTime2    = TestData.DateTime4Utc,
						EndDateTime      = TestData.DateTime4Utc.AddHours(4),
						PreNotification  = TimeSpan.FromDays(7),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					}
				});

				var qryComplex = from t in db.GetTable<Test>()
								 select new
								 {
									 Task = t,
									 NotificationDateTime = t.StartDateTime - t.PreNotification

								 };

				var qryComplexWhere = qryComplex.Where(x => (x.Task.Status != "New" && x.Task.Status != "Completed" && x.NotificationDateTime < DateTime.UtcNow) && (x.Task.StartDateTime!.Value.Date < DateTime.UtcNow.Date)).ToList();
			}
			finally
			{
				db?.Dispose();
			}
		}

		[Test]
		public void TestIssue3993_Test3([IncludeDataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllFirebird3Plus, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllSapHana, TestProvName.AllSybase)] string configuration)
		{
			MappingSchema ms;
			Model.ITestDataContext? db = null;
			try
			{
				if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle"))
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Interval)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Interval)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
				}
				else
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Int64)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
				}
				
				LinqToDB.Linq.Expressions.AddTimeSpanMappings();

				db = GetDataContext(configuration, ms);

				using var tbl = db.CreateLocalTable(new[]
				{
					new Test
					{
						StartDateTime    = TestData.DateTime4Utc,
						PreNotification = TimeSpan.FromSeconds(4 * 60 * 60 + 3 * 60 + 2)
					}
				});

				var qryComplex = from t in db.GetTable<Test>()
								 select new
								 {
									 Task = t,
									 NotificationDateTime = t.StartDateTime - t.PreNotification
								 };

				var lst = qryComplex.First();

				var val = qryComplex.Select(x=>
				new {
					StartDateTime = x.Task.StartDateTime,
					PreNotification = x.Task.PreNotification,
					x.NotificationDateTime
				}).First();

				var hour = qryComplex.Where(x => x.NotificationDateTime!.Value.Hour == 13).First();
				var minute = qryComplex.Where(x => x.NotificationDateTime!.Value.Minute == 51).First();
				var second = qryComplex.Where(x => x.NotificationDateTime!.Value.Second >= 52 && x.NotificationDateTime!.Value.Second <= 54).First();
			}
			finally
			{
				db?.Dispose();
			}
		}

		public class LanguageDTO
		{
			public string? LanguageID { get; set; }

			public TimeSpan TimeSpan { get; set; }

			public TimeSpan? TimeSpanNull { get; set; }
		}

		[Test]
		public void TestIssue3993_BulkCopy([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle") || configuration.Contains("Informix"))
			{
				ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
			}
			else
			{
				ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
				ms.AddScalarType(typeof(TimeSpan?), DataType.Int64);
			}

			LinqToDB.Linq.Expressions.AddTimeSpanMappings();

			using var db = (DataConnection) GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID = "de",
						TimeSpan = new TimeSpan(2000, 4, 3)
					},

				});

			db.BulkCopy(new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific }, new[]
				{
					new LanguageDTO
					{
						LanguageID = "en",
						TimeSpan = new TimeSpan(2000, 4, 3),
						TimeSpanNull = new TimeSpan(2000, 4, 3)
					},
				});
		}
	}
}
