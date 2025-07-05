using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests.Test3840
{
	[TestFixture]
	public class Test3840Tests : TestBase
	{
		public class Test
		{
			public virtual DateTime? StartDateTime    { get; set; }
			public virtual TimeSpan? PreNotification  { get; set; }
			public virtual TimeSpan? PreNotification2 { get; set; }
			public virtual TimeSpan  PreNotification3 { get; set; }
			public virtual DateTime? StrField         { get; set; }
		}

		[Test]
		public void Test3840([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string configuration)
		{
			using var db = GetDataContext(
				configuration,
				new FluentMappingBuilder()
					.Entity<Test>()
						.HasTableName("Common_Topology_Locations")
						.Property(e => e.StartDateTime)
						.Property(e => e.PreNotification)
							.HasDataType(DataType.Int64)
						.Property(e => e.PreNotification2)
						.Property(e => e.PreNotification3)
						.Property(e => e.StrField)
					.Build()
					.MappingSchema);
			using var tbl = db.CreateLocalTable(new[]
			{
				new Test
				{
					StartDateTime    = TestData.DateTimeUtc,
					PreNotification  = TimeSpan.FromSeconds(2000),
					PreNotification2 = TimeSpan.FromSeconds(2000),
					PreNotification3 = TimeSpan.FromSeconds(2000),
					StrField         = TestData.Date,
				}
			});

			var qry =
				from t in db.GetTable<Test>()
				select new
				{
					StartDateTime         = t.StartDateTime,
					PreNotification       = t.PreNotification,
					NotificationDateTime  = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification!.Value.Milliseconds, t.StartDateTime),
					NotificationDateTime2 = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification2!.Value.Milliseconds, t.StartDateTime),
					NotificationDateTime3 = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification3.Milliseconds, t.StartDateTime),
					t.StrField!.Value.Day
				};

			_ = qry.ToList();
		}
	}
}
