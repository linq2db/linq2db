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
	public class Test
	{
		public virtual DateTime? StartDateTime { get; set; }

		public virtual TimeSpan? PreNotification { get; set; }
	}

	[TestFixture]
	public class Test3840Tests : TestBase
	{
		[Test]
		public void Test3840([IncludeDataSources(TestProvName.AllSqlServer)] string configuration)
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();
			mb.Entity<Test>()
			   .HasTableName("Common_Topology_Locations")
			   .Property(e => e.StartDateTime)
			   .Property(e => e.PreNotification);

			using (var db = GetDataContext(configuration, ms))
			{
				using (db.CreateLocalTable<Test>())
				{
					var qry = from t in db.GetTable<Test>()
							  select new
							  {
								  StartDateTime = t.StartDateTime,
								  PreNotification = t.PreNotification,
								  NotificationDateTime = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification!.Value.Milliseconds, t.StartDateTime)
							  };
					var lst = qry.ToList();
					var sql = ((DataConnection)db).LastQuery;
				}				
			}
		}
	}
}
