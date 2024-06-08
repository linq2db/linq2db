using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1974Tests : TestBase
	{
		public class Person1974
		{
			[Column]
			public int ID {get; set;}

			[Column]
			public string? Name {get; set;}

			[Association(QueryExpressionMethod = nameof(ArticleExpr), CanBeNull = true)]
			public Article? BoughtQuery {get; set; }

			[Association(ThisKey = nameof(ID), OtherKey = nameof(Article.PersonId), CanBeNull = true)]
			public Article? Bought {get; set; }

			public static Expression<Func<Person1974, IDataContext, IQueryable<Article>>> ArticleExpr()
			{
				return (p, db) => db.GetTable<Article>().Where(a => a.PersonId == p.ID);
			}
		}

		public class Article
		{
			[Column]
			public string? ID {get; set;}

			[Column]
			public int PersonId {get; set;}

			[Column]
			public double Price {get; set;}
		}

		[Test]
		public void SelectAssociations([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new []
			{
				new Person1974{ID = 1, Name = "Person1"}, 
				new Person1974{ID = 2, Name = "Person2"} 
			}))
			using (db.CreateLocalTable(new []
			{
				new Article{ID = "Article", PersonId = 2}, 
			}))
			{
				var items = db.GetTable<Person1974>().LoadWith(p => p.Bought).LoadWith(p => p.BoughtQuery).ToArray();

				Assert.Multiple(() =>
				{
					Assert.That(items[0].Bought, Is.Null);
					Assert.That(items[0].BoughtQuery, Is.Null);

					Assert.That(items[1].Bought!.ID, Is.EqualTo("Article"));
					Assert.That(items[1].BoughtQuery!.ID, Is.EqualTo("Article"));
				});
			}
		}
	}
}
