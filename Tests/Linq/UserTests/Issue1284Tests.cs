using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture, ActiveIssue(1284)]
	public class Issue1284Tests : TestBase
	{
		[Test, DataContextSource]
		public void TestCteExpressionIsNotATable(string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte  = db.Person.Select(person => new { entry = person }).AsCte();
				var list = cte.Where(x => x.entry.ID == 1).ToList();

				Assert.AreEqual(1, list.Count);
			}
		}

		[Test, DataContextSource]
		public void TestCteNoFieldList(string context)
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

		[Test, DataContextSource]
		public void TestCteInvalidMapping(string context)
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

		[Test, DataContextSource]
		public void TestCteReservedWords(string context)
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
