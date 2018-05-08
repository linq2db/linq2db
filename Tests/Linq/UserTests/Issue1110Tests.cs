using System;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	public class Issue1110Tests : TestBase
	{
		[Table(Name = "Issue1110TB")]
		class Issue1110TestsClass
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public DateTime TimeStamp { get; set; }
		}

		[Test, DataContextSource]
		public void Test(string configuration)
		{
			using (var db = GetDataContext(configuration))
			{
				using (db.CreateLocalTable<Issue1110TestsClass>())
				{
					db.Insert(new Issue1110TestsClass() { Id = 10, TimeStamp = DateTime.UtcNow });
				}
			}
		}
	}
}
