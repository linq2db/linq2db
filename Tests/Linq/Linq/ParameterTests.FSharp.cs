using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.FSharp.Models;

namespace Tests.Linq
{
	[TestFixture]
	public partial class ParameterTests : TestBase
	{
		[Test]
		public void SqlStringParameter([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = "John";
				var person1 = db.GetTable<Person>().Where(t => t.FirstName == p).Single();

				p = "Tester";
				var person2 = db.GetTable<Person>().Where(t => t.FirstName == p).Single();

				Assert.Multiple(() =>
				{
					Assert.That(person1.FirstName, Is.EqualTo("John"));
					Assert.That(person2.FirstName, Is.EqualTo("Tester"));
				});
			}
		}

		// Excluded providers inline such parameter
		[Test]
		public void ExposeSqlStringParameter([DataSources(false, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p   = "abc";
				db.GetTable<Person>().Where(t => t.FirstName == p).ToArray();

				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(3)").Or.Contains("(4000)"));
			}
		}
	}
}
