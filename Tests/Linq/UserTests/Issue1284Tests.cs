using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Linq;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1284Tests : TestBase
	{
		[ActiveIssue(1284)]
		[Test]
		public void TestCteExpressionIsNotATable([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte  = db.Person.Select(person => new { entry = person }).AsCte();
				var list = cte.Where(x => x.entry.ID == 1).ToList();

				Assert.AreEqual(1, list.Count);
			}
		}

		[Test]
		public void TestCteNoFieldList([CteTests.CteContextSource] string context)
		{
			// fails in postgresql
			using (var db = GetDataContext(context))
			{
				var cte = db.Person
					.Select(person => new { entry = person, rn = 1 })
					.Where(x => x.rn == 1)
					.Select(x => x.entry)
					.AsCte("cte");

				var list = cte.ToList();

				Assert.AreEqual(4, list.Count);
			}
		}

		[ActiveIssue(1284)]
		[Test]
		public void TestCteInvalidMapping([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte = db.Person
					.Select(person => new {entry = person, rn = 1})
					.Where(x => x.rn == 1)
					.AsCte();
				;
				var item = cte.First();

				Assert.NotNull(item.entry.LastName);
			}
		}

		[Test]
		public void TestCteReservedWords([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte = db.Person
					.Select(person => new
					{
						x = new
						{
							Operator = person.LastName
						},
					})
					.Select(x => x.x)
					.AsCte();

				var item = cte.FirstOrDefault();

				Assert.NotNull(item);
			}
		}
	}
}
