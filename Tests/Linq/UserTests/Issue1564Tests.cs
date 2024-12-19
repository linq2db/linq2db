using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1564Tests : TestBase
	{
		[Table]
		sealed class Issue1564Category
		{
			[PrimaryKey] public int     Id           { get; set; }
			[Column]     public bool    IsVisible    { get; set; }
			[Column]     public int     DisplayOrder { get; set; }
			[Column]     public int     ParentId     { get; set; }
			[Column]     public string? Name         { get; set; }
		}

		sealed class AdminCategoryPreview
		{
			public int     Id;
			public bool    IsVisible;
			public int     DisplayOrder;
			public string? FullPath;
		}

		sealed class AdminCategoryPathItemCte
		{
			public int     CategoryId;
			public int     ParentCategoryId;
			public string? Name;
			public int     RootCategoryId;
			public int     Level;
		}

		[Test]
		public void CteTest1564([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1564Category>())
			{
				var pathQuery = GetPathQuery(db);

				var adminCategoriesQuery =
					from c in db.GetTable<Issue1564Category>()
					select new AdminCategoryPreview
					{
						Id           = c.Id,
						IsVisible    = c.IsVisible,
						DisplayOrder = c.DisplayOrder,
						FullPath     = pathQuery
							.Where(c1 => c1.RootCategoryId == c.Id)
							.StringAggregate(" -> ", i => i.Name).OrderByDescending(z => z.Level)
							.ToValue()
					};

				pathQuery.ToList();
				adminCategoriesQuery.ToList();

				Assert.That(adminCategoriesQuery.ToSqlQuery().Sql, Does.Contain("ORDER BY"));
			}

			IQueryable<AdminCategoryPathItemCte> GetPathQuery(IDataContext db)
			{
				var categoryPathCte = db.GetCte<AdminCategoryPathItemCte>(categoryHierarchy =>
				{
					return
						(
							from innerC in db.GetTable<Issue1564Category>()
							select new AdminCategoryPathItemCte
							{
								CategoryId = innerC.Id,
								ParentCategoryId = innerC.ParentId,
								Name = innerC.Name,
								RootCategoryId = innerC.Id,
								Level = 0
							}
						)
						.Concat
						(
							from c in db.GetTable<Issue1564Category>()
							from ch in categoryHierarchy.InnerJoin(ch => ch.ParentCategoryId == c.Id)
							select new AdminCategoryPathItemCte
							{
								CategoryId = c.Id,
								ParentCategoryId = c.ParentId,
								Name = c.Name,
								RootCategoryId = ch.RootCategoryId,
								Level = ch.Level + 1
							}
						);
				});

				return categoryPathCte;
			}
		}
	}
}
