using System;
using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

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


		[Test, DataContextSource(false)]
		public void Test(string configuration)
		{
			using (var db = GetDataContext(configuration))
			{
				using (new LocalTable<Issue1107TestsClass>(db))
				{
					((DataConnection)db).BulkCopy(new[] { new Issue1107TestsClass() { TestDate = new DateTime(2018, 1, 1) } });
				}
			}
		}
	}
}
