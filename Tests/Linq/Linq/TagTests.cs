using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TagTests : TestBase
	{
		[Test]
		public void Test_IfExists([DataSources(false)] string context)
		{
			var tag = "My Test";
			var expected = "-- " + tag + "\r\n";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = ((DataConnection)db).LastQuery!;

				var selectIndex = commandSql!.IndexOf("SELECT");
				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(selectIndex - expected.Length));
			}
		}

		[Test]
		public void Test_Multiline([DataSources(false)] string context)
		{
			var tag = "My custom\r\nwonderful multiline\nquery tag";
			var expected = "-- My custom\r\n-- wonderful multiline\n-- query tag\r\n";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = ((DataConnection)db).LastQuery!;

				var selectIndex = commandSql!.IndexOf("SELECT");
				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(selectIndex - expected.Length));
			}
		}

		[Test]
		public void Test_Null([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.Throws<ArgumentNullException>(() =>
				{
					var query =
					from x in db.Person.TagQuery(null!)
					select x;
				});
			}
		}
	}
}
