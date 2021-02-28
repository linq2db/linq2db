using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2856Tests : TestBase
	{
		public class GlobalTaskDTO
		{
			public Guid Id { get; set; }

			public Guid? RPSourceID { get; set; }

			public Guid? RPDestinationID { get; set; }
		}

		[Test]
		public void TestIssue2856([DataSources] string context)
		{
			var fluentMappingBuilder = new MappingSchema().GetFluentMappingBuilder();

			var carBuilder = fluentMappingBuilder.Entity<GlobalTaskDTO>();
			carBuilder.Property(x => x.Id).IsPrimaryKey();

			using (var s = GetDataContext(context, fluentMappingBuilder.MappingSchema))
			using (var carTable = s.CreateLocalTable<GlobalTaskDTO>())
			{
				var allRpIds = new []{Guid.NewGuid(),Guid.NewGuid() };

				var toRemoveRpIds2 = (
				from gt in s.GetTable<GlobalTaskDTO>()
				where allRpIds.Contains(gt.RPSourceID.Value)
				select gt.RPSourceID.Value
				).Union(
					from gt in s.GetTable<GlobalTaskDTO>()
					select gt.RPDestinationID.Value
				).ToList();
			}
		}
	}
}
