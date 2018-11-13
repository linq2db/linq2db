using System;

using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue1107Tests : TestBase
	{
		[Table(Name = "Issue1107TB")]
		class Issue1107TestsClass
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public DateTime TestDate { get; set; }
		}


		[Test]
		public void Test([DataSources(false)] string configuration)
		{
			using (var db = GetDataContext(configuration))
			{
				using (db.CreateLocalTable<Issue1107TestsClass>())
				{
					((DataConnection)db).BulkCopy(new[] { new Issue1107TestsClass() { TestDate = new DateTime(2018, 1, 1) } });
				}
			}
		}
	}
}
