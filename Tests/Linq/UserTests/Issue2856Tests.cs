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
			[PrimaryKey]
			public Guid Id { get; set; }

			public Guid? RPSourceID { get; set; }

			public Guid? RPDestinationID { get; set; }
		}

		[Test]
		public void TestIssue2856([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var s = GetDataContext(context))
			using (s.CreateLocalTable<GlobalTaskDTO>())
			{
				var allRpIds = new[] { TestData.Guid1, TestData.Guid2 };

				Assert.DoesNotThrow(() =>
					_ = (
							from gt1 in s.GetTable<GlobalTaskDTO>()
							where allRpIds.Contains(gt1.RPSourceID!.Value)
							select gt1.RPSourceID!.Value
						)
						.Union(
							from gt2 in s.GetTable<GlobalTaskDTO>()
							select gt2.RPDestinationID!.Value
						)
						.ToList());
			}
		}
	}
}
