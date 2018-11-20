using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Linq;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1284Tests : TestBase
	{
		[Test]
		public void TestCteExpressionIsNotATable([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person.Select(person => new { entry = person });
				var cte   = query.AsCte();

				var result   = cte.Where(x => x.entry.ID == 1).ToList();
				var expected = query.Where(x => x.entry.ID == 1).ToList();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void TestCteNoFieldList([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Select(person => new { entry = person, rn = 1 })
					.Where(x => x.rn == 1)
					.Select(x => x.entry);

				var cte = query
					.AsCte("cte");

				var expected = query;

				AreEqual(expected, cte);
			}
		}

		[Test]
		public void TestCteInvalidMapping([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Select(person => new { entry = person, rn = 1 })
					.Where(x => x.rn == 1);

				var cte = query
					.AsCte();

				var item = cte.First();

				var expected = query
					.First();

				Assert.AreEqual(expected, item);
			}
		}

		[ActiveIssue]
		[Test]
		public void TestCteInvalidMappingUnion([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Select(person => new { entry = person, rn = 1 })
					.Concat(db.Person
					.Select(person => new { entry = person, rn = 2 }))
					.Where(x => x.rn == 1);

				var cte = query
					.AsCte();

				var item = cte.First();

				var expected = query
					.First();

				Assert.AreEqual(expected, item);
			}
		}

		[Test]
		public void TestCteReservedWords([CteTests.CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Select(person => new
					{
						x = new
						{
							Obj = new
							{
								Operator = person.LastName
							}
						},
					})
					.Select(x => x.x);

				var cte = query
					.AsCte();

				var item = cte.FirstOrDefault();
				var expected = query.FirstOrDefault();

				Assert.AreEqual(expected, item);
			}
		}
	}
}
