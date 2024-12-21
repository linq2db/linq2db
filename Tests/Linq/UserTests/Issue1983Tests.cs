using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1983Tests : TestBase
	{
		public class Issue1983Person
		{
			public int     Id            { get; set; }
			public string? Name          { get; set; }
			public int     CountOfCards  { get; set; }
			[ExpressionMethod(nameof(CountOfCardExpr), IsColumn = true)]
			public int     CountOfCards2 { get; set; }

			public static Expression<Func<Issue1983Person, IDataContext, int>> CountOfCardExpr()
			{
				return (p, db) => db.GetTable<Issue1983Card>().Count(card => card.PersonId == p.Id && card.CardType == 2);
			}

			[ExpressionMethod(nameof(CountOfCardExpr3), IsColumn = true)]
			public int    CountOfCards3 { get; set; }

			public static Expression<Func<Issue1983Person, IDataContext, int>> CountOfCardExpr3()
			{
				return (p, db) => p.Cards.Where(card => card.CardType == 2).Count();
			}

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue1983Card.PersonId))]
			public IEnumerable<Issue1983Card> Cards { get; set; } = null!;
		}

		public class Issue1983Card
		{
			public int     Id         { get; set; }
			public int     CardType   { get; set; }
			public string? CardNumber { get; set; }
			public int     PersonId   { get; set; }
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1983Person>())
			using (db.CreateLocalTable<Issue1983Card>())
			{
				var q = from p in db.GetTable<Issue1983Person>()
						select new Issue1983Person()
						{
							Id = p.Id,
							CountOfCards = db.GetTable<Issue1983Card>().Count(card => card.PersonId == p.Id && card.CardType == 2)
						};

				// works
				q.Where(cu => cu.CountOfCards == 0 || cu.CountOfCards != 0).ToList();

				// workaround
				db.GetTable<Issue1983Person>().Where(cu => cu.CountOfCards3 == 0 || cu.CountOfCards3 != 0).ToList();

				// fails
				db.GetTable<Issue1983Person>().Where(cu => cu.CountOfCards2 == 0 || cu.CountOfCards2 != 0).ToList();
			}
		}
	}
}
