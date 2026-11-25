using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	// Reference copy of the generator behind the landing page demo (docs/source/index.md).
	// Not compiled here: copy it into Tests/Tests.Playground to run it, see README.md next to it.
	// Each test holds the C# of one demo tab and prints the SQL linq2db generates for it.
	[TestFixture]
	public class SqlDemoGenerator
	{
		#region Models

		[Table("Products")]
		public class Product
		{
			[PrimaryKey, Identity] public int     Id         { get; set; }
			[Column, NotNull]      public string  Name       { get; set; } = null!;
			[Column]               public decimal Price      { get; set; }
			[Column]               public int     CategoryId { get; set; }

			[Association(ThisKey = nameof(CategoryId), OtherKey = nameof(Category.Id))]
			public Category Category { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(OrderItem.ProductId))]
			public IQueryable<OrderItem> OrderItems { get; set; } = null!;
		}

		[Table("Categories")]
		public class Category
		{
			[PrimaryKey, Identity] public int    Id   { get; set; }
			[Column, NotNull]      public string Name { get; set; } = null!;
		}

		[Table("OrderItems")]
		public class OrderItem
		{
			[PrimaryKey, Identity] public int     Id        { get; set; }
			[Column]               public int     ProductId { get; set; }
			[Column]               public int     Quantity  { get; set; }
			[Column]               public decimal UnitPrice { get; set; }
		}

		#endregion

		[ExpressionMethod(nameof(Impl))]
		static decimal TotalRevenue(Product p)
			=> throw new InvalidOperationException();

		static Expression<Func<Product, decimal>> Impl() => p => p.OrderItems
			.Sum(oi => oi.Quantity * oi.UnitPrice);

		static readonly SqlGenerationOptions _sqlOptions = new() { InlineParameters = true };

		// SQLite in-memory: no server needed, and CreateTempTable can run for real.
		static DataConnection CreateSQLiteConnection(Action<TraceInfo>? onTrace = null)
		{
			var options = new DataOptions().UseSQLite("Data Source=:memory:", SQLiteProvider.Microsoft);

			if (onTrace != null)
				options = options.UseTracing(System.Diagnostics.TraceLevel.Info, onTrace);

			return new DataConnection(options);
		}

		// SQL Server dialect for SQL generation only: ToSqlQuery never opens the connection.
		static DataConnection CreateSqlServerConnection()
		{
			var options = new DataOptions().UseSqlServer("Server=offline", SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient);
			return new DataConnection(options);
		}

		static void Print(string tab, string sql)
		{
			TestContext.Out.WriteLine($"=== {tab} ===");
			TestContext.Out.WriteLine(sql);
		}

		[Test]
		public void Query()
		{
			using var db = CreateSQLiteConnection();

			var query =
				from p in db.GetTable<Product>()
				where p.Price > 100
				   && p.CategoryId == 1
				orderby p.Name
				select new
				{
					p.Name,
					p.Price
				};

			Print("Query", query.ToSqlQuery(_sqlOptions).Sql);
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

			Print("Associations", query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void ExpressionMethod()
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

			Print("ExpressionMethod", query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void Cte()
		{
			using var db = CreateSQLiteConnection();

			var topProducts =
				db.GetTable<Product>()
				.Where(p => p.Price > 50)
				.Select(p => new
				{
					p.Id, p.Name,
					p.Price, p.CategoryId
				})
				.AsCte("TopProducts");

			var query =
				from tp in topProducts
				join c in db.GetTable<Category>()
					on tp.CategoryId equals c.Id
				select new
				{
					tp.Name, tp.Price,
					Category = c.Name,
					SameCategory = topProducts
						.Count(x => x.CategoryId
							== tp.CategoryId)
				};

			Print("CTE", query.ToSqlQuery(_sqlOptions).Sql);
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
					RowNum = Sql.Ext.RowNumber()
						.Over()
						.PartitionBy(p.CategoryId)
						.OrderByDesc(p.Price)
						.ToValue(),
					Total  = Sql.Ext.Sum(p.Price)
						.Over()
						.PartitionBy(p.CategoryId)
						.ToValue()
				};

			Print("Window Functions", query.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void Merge()
		{
			using var db = CreateSqlServerConnection();

			var source = new[]
			{
				new Product { Id = 1, Name = "Laptop", Price = 999, CategoryId = 1 },
				new Product { Id = 2, Name = "Tablet", Price = 499, CategoryId = 1 },
			};

			// The tab finishes this chain with .Merge(), which executes it; ToSqlQuery renders
			// the same statement without a server.
			var merge = db.GetTable<Product>()
				.Merge()
				.Using(source)
				.OnTargetKey()
				.UpdateWhenMatched(
					(target, src) => new Product
					{
						Name  = src.Name,
						Price = src.Price,
					})
				.InsertWhenNotMatched(
					src => new Product
					{
						Id         = src.Id,
						Name       = src.Name,
						Price      = src.Price,
						CategoryId = src.CategoryId,
					});

			Print("Merge", merge.ToSqlQuery(_sqlOptions).Sql);
		}

		[Test]
		public void TempTable()
		{
			var executed = new StringBuilder();

			using var db = CreateSQLiteConnection(ti =>
			{
				if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
					executed.AppendLine(ti.SqlText);
			});

			// Populate temp table from data
			var ids = Enumerable
				.Range(1, 500)
				.Select(i => new { Id = i });

			string querySql;

			using (var tmp =
				db.CreateTempTable(
					"FilterIds", ids))
			{
				// Join temp table in LINQ query
				var query =
					from p in db.GetTable<Product>()
					join t in tmp
						on p.Id equals t.Id
					orderby p.Name
					select new
					{
						p.Name,
						p.Price
					};

				querySql = query.ToSqlQuery(_sqlOptions).Sql;
			}

			// CREATE, the bulk copy INSERT and the DROP issued by Dispose, in execution order
			Print("Temp Table (executed)", executed.ToString());
			Print("Temp Table (query)", querySql);
		}
	}
}
