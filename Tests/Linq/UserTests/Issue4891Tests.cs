using System;
using System.Linq.Expressions;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue4891Tests : TestBase
	{
		public class BaseEntity
		{
			public virtual Guid Id { get; set; }
		}

		public class Product : BaseEntity
		{
			public string? ProductName { get; set; }
		}

		public class Category : BaseEntity
		{
			public string? CategoryName { get; set; }

			public Guid ProductId { get; set; }
		}

		public class ProductCategoryJoin : IJoinResult<Product, Category>
		{
			public Product Source1 { get; set; } = default!;

			public Category Source2 { get; set; } = default!;
		}

		public interface IJoinResult<T1, T2>
		{
			T1 Source1 { get; set; }

			T2 Source2 { get; set; }
		}

		public static class Example
		{
			public static IQueryable<TResult> InnerJoin<T1, T2, TResult>(IQueryable<T1> query1, IQueryable<T2> query2, Expression<Func<T1, T2, bool>> predicate)
				where T1 : BaseEntity
				where T2 : BaseEntity
				where TResult : IJoinResult<T1, T2>, new()
			{
				return query1
					.InnerJoin(query2, predicate, (product, category) =>
						new TResult
						{
							Source1 = product,
							Source2 = category,
						});
			}
		}

		[Test]
		public void Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Product>())
			using (db.CreateLocalTable<Category>())
			{
				var productId = TestData.Guid1;

				var query = Example.InnerJoin<Product, Category, ProductCategoryJoin>(
					db.GetTable<Product>(),
					db.GetTable<Category>(),
					(product, category) => product.Id == category.ProductId)
					.Where(x => x.Source1.Id == productId);

				var result = query.ToArray();
				Assert.That(result, Has.Length.Zero);
			}
		}
	}

}
