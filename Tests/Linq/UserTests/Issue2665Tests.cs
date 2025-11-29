using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2665Tests : TestBase
	{
		[Table]
		sealed class ProductTable
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = false)]
			public string? Name { get; set; }

			[Association(
				ThisKey  = nameof(Id),
				OtherKey = nameof(ProductAttributeMapping.ProductId))]
			public ProductAttributeMapping? AtributeMapping { get; set; }
		}

		[Table]
		sealed class ProductAttributeTable
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = false)]
			public string? Name { get; set; }

			[Association(
				ThisKey  = nameof(Id),
				OtherKey = nameof(ProductAttributeMapping.ProductAttributeId))]
			public ProductAttributeMapping? AtributeMapping { get; set; }

		}

		[Table]
		sealed class ProductAttributeMapping
		{
			[PrimaryKey]
			public int ProductId { get; set; }

			[PrimaryKey]
			public int ProductAttributeId { get; set; }
		}

		[YdbTableNotFound]
		[Test]
		public void IssueTest([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<ProductTable>())
				using (db.CreateLocalTable<ProductAttributeTable>())
				using (db.CreateLocalTable<ProductAttributeMapping>())
				{
					var query = from p in db.GetTable<ProductTable>()
								join pam in  db.GetTable<ProductAttributeMapping>() on p.Id equals pam.ProductId
								group p by p into groupedProduct
								where groupedProduct.Count() == 1
								select groupedProduct.Key;

					var query2 = from pam in db.GetTable<ProductAttributeMapping>()
								 join pa in db.GetTable<ProductAttributeTable>() on pam.ProductAttributeId equals pa.Id
								 where query.Any(p => p.Id >= pam.ProductId)
								 select pa.Id;

					var res = query2.ToList();

					Assert.That(res, Is.Empty);
				}
			}
		}
	}
}
