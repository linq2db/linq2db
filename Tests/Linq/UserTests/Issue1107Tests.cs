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


		[Test, DataContextSource]
		public void Test(string configuration)
		{
			using (var db = GetDataContext(configuration))
			{
				try
				{
					db.CreateTable<Issue1107TestsClass>();
				}
				catch
				{
					db.DropTable<Issue1107TestsClass>(throwExceptionIfNotExists: false);

					db.CreateTable<Issue1107TestsClass>();
				}

				try
				{
					((DataConnection)db).BulkCopy(new[] { new Issue1107TestsClass() { TestDate = new DateTime(2018, 1, 1) } });
				}
				finally
				{
					db.DropTable<Issue1107TestsClass>();
				}
			}
		}
	}
}
