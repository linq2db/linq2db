using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.UserTests.Test3840
{
	[TestFixture]
	public class Test3840Tests : TestBase
	{
		public class Test
		{
			public virtual DateTime? StartDateTime { get; set; }

			public virtual TimeSpan? PreNotification { get; set; }
		}

		[Test]
		public void Test3840([IncludeDataSources(true, TestProvName.AllSqlServer)] string configuration)
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();
			mb.Entity<Test>()
				.HasTableName("Common_Topology_Locations")
				.Property(e => e.StartDateTime)
				.Property(e => e.PreNotification).HasDataType(LinqToDB.DataType.Int64);
			mb.Build();

			using var db = GetDataContext(configuration, ms);
			using var _ = db.CreateLocalTable<Test>();

			db.Insert(new Test() { StartDateTime = DateTime.UtcNow, PreNotification = TimeSpan.FromSeconds(2000) });

			var qry = from t in db.GetTable<Test>()
					  select new
					  {
						  StartDateTime = t.StartDateTime,
						  PreNotification = t.PreNotification,
						  NotificationDateTime = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification!.Value.Milliseconds, t.StartDateTime)
					  };
			
			var lst = qry.ToList();
		}
	}
}
