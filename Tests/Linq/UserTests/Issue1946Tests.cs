using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1946Tests : TestBase
	{
		public enum UserVersion
		{
			FirstRelease = 0,
			SecondRelease = 1
		}

		[Table("Users")]
		public class User
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = true)]
			public UserVersion? Version { get; set; }
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(new[]{new User{Id = 1, Version = UserVersion.FirstRelease} }))
			{
				var maxVersion = UserVersion.SecondRelease;

				var query = from u in table
					where u.Version!.Value < maxVersion
					select u.Id;

				var ids1 = query.ToArray();
				Assert.That(ids1, Has.Length.EqualTo(1));
			}
		}
	}
}
