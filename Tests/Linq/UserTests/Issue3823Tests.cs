using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3823Tests : TestBase
	{
		public class BasicDTO
		{
			public virtual int Id { get; set; }
		}

		public class TopologyDTO : BasicDTO
		{
			public virtual string Name { get; set; }

			public virtual Guid ParentId { get; set; }
		}

		public class PlantDTO : TopologyDTO
		{
			public PlantDTO()
			{ }
		}

		public class LocationDTO : TopologyDTO
		{
			public LocationDTO()
			{ }
		}


		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite, TestProvName.SqlServer2019MS)] string configuration)
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();
			mb.Entity<LocationDTO>()
			   .HasTableName("Common_Topology_Locations")
			   .Property(e => e.ParentId).HasColumnName("ClientId")
			   .Property(e => e.Id).IsPrimaryKey();

			TempTable<LocationDTO> table = null;
			using (var db = GetDataContext(configuration, ms))
			{
				table = db.CreateLocalTable<LocationDTO>();
			}

			ms = new MappingSchema();
			mb = ms.GetFluentMappingBuilder();
			mb.Entity<LocationDTO>()
			   .HasTableName("Common_Topology_Locations")
			   .Property(e => e.ParentId).HasColumnName("ClientId")
			   .Property(e => e.Id).IsPrimaryKey();
			mb.Entity<PlantDTO>()
			   .HasTableName("Common_Topology_Plants")
			   .Property(e => e.Id).IsPrimaryKey()
			   .Property(e => e.ParentId).HasColumnName("LocationId");

			using (var db = GetDataContext(configuration, ms))
			{
				table = db.GetTable<LocationDTO>().ToList();
			}

			table.Dispose();
		}
	}
}
