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
		public void TestIssue3993_Test1([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql)] string configuration)
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
						StartDateTime    = TestData.DateTimeUtc,
						StartDateTime2    = TestData.DateTimeUtc,
						EndDateTime      = TestData.DateTimeUtc.AddHours(4),
						PreNotification  = TimeSpan.FromSeconds(20000),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					}
				});



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

				var res = qry.Where(x => x.NotificationDateTime < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(1, res.Count);
				var res2 = qry.Where(x => x.NotificationDateTime2 < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(1, res2.Count);
				var res3 = qry.Where(x => x.NotificationDateTime4 < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(1, res3.Count);
				var res31 = qry.Where(x => x.NotificationDateTime5 < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(1, res31.Count);
				var res33 = qry.Where(x => x.NotificationDateTime6 < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(0, res33.Count);
				var res22 = qry.Where(x => x.NotificationDateTime7 < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(1, res22.Count);
				var res11 = qry.Where(x => x.NotificationDateTime8 < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(1, res11.Count);

				var qry4 =
				from t in db.GetTable<Test>()
				select new
				{
					NotificationDateTime4 = t.StartDateTime - t.PreNotification3,
				};

				var res4 = qry4.Where(x => x.NotificationDateTime4 < TestData.DateTimeUtc).ToList();
				Assert.AreEqual(1, res3.Count);


				var qry5 =
				from t in db.GetTable<Test>()
				select new
				{
					diff = t.EndDateTime - t.StartDateTime,
				};

				var res6 = qry5.ToList();
				Assert.AreEqual(1, res6.Count);
				var res5 = qry5.Where(x => x.diff < TimeSpan.FromHours(5)).ToList();
				Assert.AreEqual(1, res5.Count);
				var res7 = qry5.Where(x => x.diff!.Value.TotalHours < 5).ToList();
				Assert.AreEqual(1, res7.Count);
				var res8 = qry5.Where(x => x.diff < TimeSpan.FromHours(2)).ToList();
				Assert.AreEqual(0, res8.Count);
				var res9 = qry5.Where(x => x.diff!.Value.TotalHours < 2).ToList();
				Assert.AreEqual(0, res9.Count);
			}
			finally
			{
				db?.Dispose();
			}
		}
		
		[Test]
		public void TestIssue3993_Test2([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql)] string configuration)
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
						StartDateTime    = TestData.DateTimeUtc,
						StartDateTime2    = TestData.DateTimeUtc,
						EndDateTime      = TestData.DateTimeUtc.AddHours(4),
						PreNotification  = TimeSpan.FromSeconds(20000),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					},
					new Test
					{
						StartDateTime    = new DateTime(2023,10,17, 9,40,23),
						StartDateTime2    = TestData.DateTimeUtc,
						EndDateTime      = TestData.DateTimeUtc.AddHours(4),
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
		public void TestIssue3993_Test3([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql)] string configuration)
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
						StartDateTime    = TestData.DateTimeUtc,
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
				var second = qryComplex.Where(x => x.NotificationDateTime!.Value.Second == 53).First();
			}
			finally
			{
				db?.Dispose();
			}
		}
	}
}
