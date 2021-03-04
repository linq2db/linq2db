using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class TagTests : TestBase
	{
		[Test]
		public void Test_IfExists([DataSources] string context)
		{
			var tag = "My Test";
			var expected = "-- " + tag + "\r\n";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagWith(tag)
					select x;				

				var commandSql = query.ToString();

				var selectIndex = commandSql.IndexOf("SELECT");
				Assert.That(commandSql.IndexOf(expected), Is.EqualTo(selectIndex - expected.Length));				
			}
		}

		[Test]
		public void Test_Multiline([DataSources] string context)
		{
			var tag = "My custom\r\nwonderful multiline\nquery tag";
			var expected = "-- My custom\r\n-- wonderful multiline\n-- query tag\r\n";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagWith(tag)
					select x;

				var commandSql = query.ToString();

				var selectIndex = commandSql.IndexOf("SELECT");
				Assert.That(commandSql.IndexOf(expected), Is.EqualTo(selectIndex - expected.Length));
			}
		}
	}
}
