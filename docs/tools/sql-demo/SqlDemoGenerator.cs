using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class SqlDemoGenerator : TestBase
	{
		#region Models

		[Table("Products")]
		public class Product
		{
			[PrimaryKey, Identity] public int     Id         { get; set; }
			[Column, NotNull]     public string   Name       { get; set; } = null!;
			[Column]              public decimal   Price      { get; set; }
			[Column]              public int       CategoryId { get; set; }
			[Column]              public DateTime  CreatedAt  { get; set; }

			[Association(ThisKey = nameof(CategoryId), OtherKey = nameof(Category.Id))]
			public Category Category { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(OrderItem.ProductId))]
			public IQueryable<OrderItem> OrderItems { get; set; } = null!;
		}

		[Table("Categories")]
		public class Category
		{
			[PrimaryKey, Identity] public int    Id   { get; set; }
			[Column, NotNull]     public string  Name { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Product.CategoryId))]
			public IQueryable<Product> Products { get; set; } = null!;
		}

		[Table("Orders")]
		public class Order
		{
			[PrimaryKey, Identity] public int      Id         { get; set; }
			[Column, NotNull]     public string    Customer   { get; set; } = null!;
			[Column]              public DateTime   OrderDate  { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(OrderItem.OrderId))]
			public IQueryable<OrderItem> Items { get; set; } = null!;
		}

		[Table("OrderItems")]
		public class OrderItem
		{
			[PrimaryKey, Identity] public int     Id        { get; set; }
			[Column]              public int      OrderId   { get; set; }
			[Column]              public int      ProductId { get; set; }
			[Column]              public int      Quantity  { get; set; }
			[Column]              public decimal  UnitPrice { get; set; }

			[Association(ThisKey = nameof(OrderId), OtherKey = nameof(Order.Id))]
			public Order Order { get; set; } = null!;

			[Association(ThisKey = nameof(ProductId), OtherKey = nameof(Product.Id))]
			public Product Product { get; set; } = null!;
		}

		#endregion

		#region ExpressionMethod helpers

		[ExpressionMethod(nameof(TotalRevenueImpl))]
		public static decimal TotalRevenue(Product product)
		{
			throw new InvalidOperationException("Should not be called directly");
		}

		static Expression<Func<Product, decimal>> TotalRevenueImpl()
		{
			return product => product.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
		}

		#endregion

		static readonly SqlGenerationOptions _sqlOptions = new() { InlineParameters = true };

		static DataConnection CreateSQLiteConnection()
		{
			var options = new DataOptions().UseSQLite(":memory:");
			return new DataConnection(options);
		}

		[Test]
		public void BasicQuery()
		{
			using var db = CreateSQLiteConnection();

			var query =
				from p in db.GetTable<Product>()
				where p.Price > 100 && p.CategoryId == 1
				orderby p.Name
				select new
				{
					p.Name,
					p.Price
				};

			Console.WriteLine("=== Basic Query ===");
			Console.WriteLine(query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void Associations()
		{
			using var db = CreateSQLiteConnection();

			var query =
				from p in db.GetTable<Product>()
				select new
				{
					Product  = p.Name,
					Category = p.Category.Name,
					Orders   = p.OrderItems.Count()
				};

			Console.WriteLine("=== Associations ===");
			Console.WriteLine(query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void ExpressionMethodDemo()
		{
			using var db = CreateSQLiteConnection();

			var query =
				from p in db.GetTable<Product>()
				let total = TotalRevenue(p)
				where total > 1000
				select new
				{
					p.Name,
					Revenue = total
				};

			Console.WriteLine("=== ExpressionMethod ===");
			Console.WriteLine(query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void CteDemo()
		{
			using var db = CreateSQLiteConnection();

			var topProducts = db.GetTable<Product>()
				.Where(p => p.Price > 50)
				.Select(p => new
				{
					p.Id,
					p.Name,
					p.Price,
					p.CategoryId
				})
				.AsCte("TopProducts");

			var query =
				from tp in topProducts
				join c in db.GetTable<Category>() on tp.CategoryId equals c.Id
				select new
				{
					tp.Name,
					tp.Price,
					Category     = c.Name,
					SameCategory = topProducts.Count(x => x.CategoryId == tp.CategoryId)
				};

			Console.WriteLine("=== CTE ===");
			Console.WriteLine(query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void WindowFunctions()
		{
			using var db = CreateSQLiteConnection();

			var query =
				from p in db.GetTable<Product>()
				select new
				{
					p.Name,
					p.Price,
					Category = p.Category.Name,
					RowNum   = Sql.Ext.RowNumber().Over().PartitionBy(p.CategoryId).OrderByDesc(p.Price).ToValue(),
					Rank     = Sql.Ext.Rank().Over().PartitionBy(p.CategoryId).OrderByDesc(p.Price).ToValue(),
					Total    = Sql.Ext.Sum(p.Price).Over().PartitionBy(p.CategoryId).ToValue()
				};

			Console.WriteLine("=== Window Functions ===");
			Console.WriteLine(query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void MergeDemo([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var source = new[]
			{
				new Product { Id = 1, Name = "Laptop",  Price = 999, CategoryId = 1 },
				new Product { Id = 2, Name = "Tablet",  Price = 499, CategoryId = 1 },
			};

			var merge = db.GetTable<Product>()
				.Merge()
				.Using(source)
				.OnTargetKey()
				.UpdateWhenMatched((target, src) => new Product
				{
					Name  = src.Name,
					Price = src.Price,
				})
				.InsertWhenNotMatched(src => new Product
				{
					Id         = src.Id,
					Name       = src.Name,
					Price      = src.Price,
					CategoryId = src.CategoryId,
				});

			Console.WriteLine("=== Merge ===");
			Console.WriteLine(merge.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void TempTableDemo()
		{
			using var db = CreateSQLiteConnection();

			var query =
				from p in db.GetTable<Product>()
				from oi in db.GetTable<OrderItem>().InnerJoin(oi => oi.ProductId == p.Id)
				group new { p, oi } by new { p.Id, p.Name } into g
				select new
				{
					g.Key.Name,
					TotalQty   = g.Sum(x => x.oi.Quantity),
					TotalValue = g.Sum(x => x.oi.Quantity * x.oi.UnitPrice)
				};

			Console.WriteLine("=== TempTable ===");
			Console.WriteLine(query.ToSqlQuery(_sqlOptions).Sql);
		}
	}
}
