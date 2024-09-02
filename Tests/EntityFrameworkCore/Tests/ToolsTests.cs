using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

using FluentAssertions;

using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;
using LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.Northwind;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer
{
	[TestFixture]
	public class ToolsTests : NorthwindContextTestBase
	{
		[Test]
		public void TestToList([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			using var db = ctx.CreateLinqToDBConnection();
			var items = db.GetTable<Order>()
				.LoadWith(d => d.OrderDetails)
				.ThenLoad(d => d.Product).ToList();
		}

		[Test]
		public void TestShadowProperty([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Products.Select(p => new
			{
				Quantity = EF.Property<string>(p, "QuantityPerUnit")
			});

			var expected = query.ToArray();
			var result = query.ToLinqToDB().ToArray();
		}

		IQueryable<Product> ProductQuery(NorthwindContextBase ctx)
		{
			return ctx.Products.Where(p => p.OrderDetails.Count > 0);
		}

		[Test]
		public void TestCallback([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ProductQuery(ctx)
				.Where(pd => pd.ProductName.StartsWith("a"));

			query.Where(p => p.ProductName == "a").Delete();
		}

		[Test]
		public void TestContextRetrieving([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ProductQuery(ctx)
				.ToLinqToDB()
				.Where(pd => pd.ProductName.StartsWith("a"));
		}

		[Test]
		public void TestDelete([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ProductQuery(ctx)
				.Where(pd => pd.ProductName.StartsWith("a"));
		}

		[Test]
		public void TestNestingFunctions([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query =
				from pd in ProductQuery(ctx)
				from pd2 in ProductQuery(ctx)
				where pd.ProductId == pd2.ProductId
				orderby pd.ProductId
				select new { pd, pd2 };

			var items1 = query.ToArray();
			var items2 = query.ToLinqToDB().ToArray();
		}

		[Test]
		public void TestCreateFromOptions([EFDataSources] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder<NorthwindContextBase>();

			var options = base.ProviderSetup(provider, connectionString, optionsBuilder).Options;
			using var db = options.CreateLinqToDBConnection();
		}

		// TODO: reenable after fix
		//[Test]
		//public void TestFunctions([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		//{
		//	using var ctx = CreateContext(provider);

		//	var query = from p in ctx.Orders
		//					//where EF.Functions.Like(p., "a%") || true
		//					//orderby p.ProductId
		//				select new
		//				{
		//					p.OrderId,
		//					// Date = Model.TestFunctions.GetDate(),
		//					// Len = Model.TestFunctions.Len(p.Name),
		//					DiffYear1 = SqlServerDbFunctionsExtensions.DateDiffYear(EF.Functions, p.ShippedDate, p.OrderDate),
		//					DiffYear2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffYear(EF.Functions, p.ShippedDate, p.OrderDate.Value),
		//					DiffMonth1 = SqlServerDbFunctionsExtensions.DateDiffMonth(EF.Functions, p.ShippedDate, p.OrderDate),
		//					DiffMonth2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffMonth(EF.Functions, p.ShippedDate, p.OrderDate.Value),
		//					DiffDay1 = SqlServerDbFunctionsExtensions.DateDiffDay(EF.Functions, p.ShippedDate, p.OrderDate),
		//					DiffDay2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffDay(EF.Functions, p.ShippedDate, p.OrderDate.Value),
		//					DiffHour1 = SqlServerDbFunctionsExtensions.DateDiffHour(EF.Functions, p.ShippedDate, p.OrderDate),
		//					DiffHour2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffHour(EF.Functions, p.ShippedDate, p.OrderDate.Value),
		//					DiffMinute1 = SqlServerDbFunctionsExtensions.DateDiffMinute(EF.Functions, p.ShippedDate, p.OrderDate),
		//					DiffMinute2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffMinute(EF.Functions, p.ShippedDate, p.OrderDate.Value),
		//					DiffSecond1 = SqlServerDbFunctionsExtensions.DateDiffSecond(EF.Functions, p.ShippedDate, p.OrderDate),
		//					DiffSecond2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffSecond(EF.Functions, p.ShippedDate, p.OrderDate.Value),
		//					DiffMillisecond1 = SqlServerDbFunctionsExtensions.DateDiffMillisecond(EF.Functions, p.ShippedDate, p.ShippedDate!.Value.AddMilliseconds(100)),
		//					DiffMillisecond2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffMillisecond(EF.Functions, p.ShippedDate, p.ShippedDate.Value.AddMilliseconds(100)),
		//				};

		//	//				var items1 = query.ToArray();
		//	var items2 = query.ToLinqToDB().ToArray();
		//}

		[Test]
		public async Task TestTransaction([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);
			await using var transaction = await ctx.Database.BeginTransactionAsync();
			using var db = ctx.CreateLinqToDBConnection();

#pragma warning disable CA1866 // Use char overload
			var test1 = await ctx.Products.Where(p => p.ProductName.StartsWith("U")).MaxAsync(p => p.QuantityPerUnit);
			var test2 = await ctx.Products.Where(p => p.ProductName.StartsWith("U")).MaxAsyncLinqToDB(p => p.QuantityPerUnit);
#pragma warning restore CA1866 // Use char overload

			Assert.That(test2, Is.EqualTo(test1));

			ctx.Products.Where(p => p.ProductName == "a")
				.ToLinqToDB(db)
				.Delete();

			await transaction.RollbackAsync();
		}

		[Test]
		public void TestView([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);
			using var db = ctx.CreateLinqToDBConnection();

#pragma warning disable CA1866 // Use char overload
			var query = ProductQuery(ctx)
				.ToLinqToDB(db)
				.Where(pd => pd.ProductName.StartsWith("a"));
#pragma warning restore CA1866 // Use char overload

			var items = query.ToArray();
		}


		[Test]
		public void TestTransformation([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query =
				from p in ctx.Products
				from c in ctx.Categories.InnerJoin(c => c.CategoryId == p.CategoryId)
				select new
				{
					Product = p,
					Ctegory = c
				};

			var items = query.ToLinqToDB().ToArray();
		}

		[Test]
		public void TestTransformationTable([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query =
				from p in ctx.Products
				from c in ctx.Categories.ToLinqToDBTable().InnerJoin(c => c.CategoryId == p.CategoryId)
				select new
				{
					Product = p,
					Ctegory = c
				};

			var items = query.ToLinqToDB().ToArray();
		}

		[Test]
		public void TestDemo2([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query =
				from p in ctx.Products
				from op in ctx.Products.LeftJoin(op => op.ProductId != p.ProductId && op.ProductName == p.ProductName)
				where Sql.ToNullable(op.ProductId) == null
				select p;

			query = query.ToLinqToDB();

			var str = query.ToString();

			var items = query.ToArray();
		}

		[Test]
		public void TestKey([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var ms = LinqToDBForEFTools.GetMappingSchema(ctx.Model, ctx, null);

			var customerPk = ms.GetAttribute<ColumnAttribute>(typeof(Customer),
				MemberHelper.MemberOf<Customer>(c => c.CustomerId));

			Assert.That(customerPk, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(customerPk!.IsPrimaryKey, Is.EqualTo(true));
				Assert.That(customerPk.PrimaryKeyOrder, Is.EqualTo(0));
			});
		}

		[Test]
		public void TestAssociations([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var ms = LinqToDBForEFTools.GetMappingSchema(ctx.Model, ctx, null);

			var associationOrder = ms.GetAttribute<AssociationAttribute>(typeof(Customer),
				MemberHelper.MemberOf<Customer>(c => c.Orders));

			Assert.That(associationOrder, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(associationOrder!.ThisKey, Is.EqualTo("CustomerId"));
				Assert.That(associationOrder.OtherKey, Is.EqualTo("CustomerId"));
			});
		}

		[Test]
		public void TestGlobalQueryFilters([EFDataSources] string provider, [Values] bool enableFilter)
		{
#if NET8_0
			if (provider.IsAnyOf(TestProvName.AllMySql))
			{
				try
				{
					using var _ = new DisableBaseline("Pomelo bug workaround");
					Test();
				}
				catch (UnreachableException)
				{
				}
			}
#endif
			Test();

			void Test()
			{
				using var ctx = CreateContext(provider, enableFilter);

				var withoutFilterQuery =
				from p in ctx.Products.IgnoreQueryFilters()
				join d in ctx.OrderDetails on p.ProductId equals d.ProductId
				select new { p, d };

				var efResult      = withoutFilterQuery.ToArray();
				var linq2dbResult = withoutFilterQuery.ToLinqToDB().ToArray();

				Assert.That(linq2dbResult, Has.Length.EqualTo(efResult.Length));

				var withFilterQuery =
					from p in ctx.Products
					join d in ctx.OrderDetails on p.ProductId equals d.ProductId
					select new { p, d };

				var efResult2  = withFilterQuery.ToArray();
				var linq2dbResult2 = withFilterQuery.ToLinqToDB().ToArray();

				Assert.That(linq2dbResult2, Has.Length.EqualTo(efResult2.Length));
			}
		}

		[Test]
		public async Task TestAsyncMethods([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

#pragma warning disable CA1847 // Use char literal for a single character lookup
			var query = ctx.Products.AsQueryable().Where(p => p.ProductName.Contains("a"));
#pragma warning restore CA1847 // Use char literal for a single character lookup

			var expectedArray = await query.ToArrayAsync();
			var expectedDictionary = await query.ToDictionaryAsync(p => p.ProductId);
			var expectedAny = await query.AnyAsync();

			var byList = await EntityFrameworkQueryableExtensions.ToListAsync(query.ToLinqToDB());
			var byArray = await EntityFrameworkQueryableExtensions.ToArrayAsync(query.ToLinqToDB());
			var byDictionary = await EntityFrameworkQueryableExtensions.ToDictionaryAsync(query.ToLinqToDB(), p => p.ProductId);
			var any = await EntityFrameworkQueryableExtensions.AnyAsync(query.ToLinqToDB());
		}

		[Test]
		public async Task TestInclude([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Orders
				.Include(o => o.Employee!)
				.ThenInclude(e => e.EmployeeTerritories)
				.ThenInclude(et => et.Territory)
				.Include(o => o.OrderDetails)
				.ThenInclude(d => d.Product);

			var expected = await query.ToArrayAsync();
			var result = await query.ToLinqToDB().ToArrayAsync();
		}

		[Test]
		public async Task TestEager([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Orders.Select(o => new
			{
				Employee = o.Employee,
				EmployeeTerritories = o.Employee!.EmployeeTerritories.Select(et => new
				{
					EmployeeTerritory = et,
					Territory = et.Territory
				}),

				OrderDetails = o.OrderDetails.Select(od => new
				{
					OrderDetail = od,
					od.Product
				})
			});

			var expected = await query.ToArrayAsync();

			var result = await query.ToLinqToDB().ToArrayAsync();
		}

		[Test]
		public async Task TestIncludeString([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Orders
				.Include("Employee.EmployeeTerritories")
				.Include(o => o.OrderDetails)
				.ThenInclude(d => d.Product);

			var expected = await query.ToArrayAsync();

			var result = await query.ToLinqToDB().ToArrayAsync();
		}

		[Test]
		public async Task TestLoadFilter([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Products.Select(p => new
			{
				p.ProductName,
				OrderDetails = p.OrderDetails.Select(od => new
				{
					od.Discount,
					od.Order,
					od.Product.Supplier!.Products
				})
			});

			ctx.IsSoftDeleteFilterEnabled = true;

			var expected = await query.ToArrayAsync();
			var filtered = await query.ToLinqToDB().ToArrayAsync();

			Assert.That(filtered, Has.Length.EqualTo(expected.Length));
		}

		[Test]
		public async Task TestGetTable([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.GetTable<Customer>().Where(o => o.City != null);

			var expected = await query.ToArrayAsync();
		}

		[Test]
		public void TestInMemory()
		{
			var optionsBuilder = new DbContextOptionsBuilder<NorthwindContext>();

			optionsBuilder.UseInMemoryDatabase("sample");
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			using var ctx = new NorthwindContext(optionsBuilder.Options);
			ctx.Database.EnsureCreated();

			Assert.Throws<LinqToDBForEFToolsException>(() =>
			{
				ctx.Products.ToLinqToDB().ToArray();
			});

			Assert.Throws<LinqToDBForEFToolsException>(() =>
			{
				ctx.Products
					.Where(so => so.ProductId == -1)
					.Delete();
			});
		}

		[Test]
		public async Task TestContinuousQueries([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Orders
				.Include(o => o.OrderDetails)
				.ThenInclude(d => d.Product)
				.ThenInclude(p => p.OrderDetails);

			var expected = await query.ToArrayAsync();
			var result   = await query.ToLinqToDB().ToArrayAsync();
		}

		[Test]
		public async Task TestChangeTracker([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Orders
				.Include(o => o.OrderDetails)
				.ThenInclude(d => d.Product)
				.ThenInclude(p => p.OrderDetails);

			// var efResult = await query.ToArrayAsync();
			var result = await query.ToLinqToDB().ToArrayAsync();

			var orderDetail = result[0].OrderDetails.First();
			orderDetail.UnitPrice *= 1.1m;

			ctx.ChangeTracker.DetectChanges();
			var changedEntry = ctx.ChangeTracker.Entries().Single(e => e.State == EntityState.Modified);
			await ctx.SaveChangesAsync();
		}

		[Test]
		public async Task TestChangeTrackerDisabled1([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Orders
				.Include(o => o.OrderDetails)
				.ThenInclude(d => d.Product)
				.ThenInclude(p => p.OrderDetails)
				.AsNoTracking();

			// var efResult = await query.ToArrayAsync();
			var result = await query.ToLinqToDB().ToArrayAsync();

			var orderDetail = result[0].OrderDetails.First();
			orderDetail.UnitPrice *= 1.1m;

			ctx.ChangeTracker.DetectChanges();
			var changedEntry = ctx.ChangeTracker.Entries().SingleOrDefault(e => e.State == EntityState.Modified);
			Assert.That(changedEntry, Is.Null);
			await ctx.SaveChangesAsync();
		}

		[Test]
		public async Task TestChangeTrackerDisabled2([EFDataSources] string provider, [Values] bool enableFilter)
		{
			LinqToDBForEFTools.EnableChangeTracker = false;
			try
			{
				using var ctx = CreateContext(provider, enableFilter);

				var query = ctx.Orders
					.Include(o => o.OrderDetails)
					.ThenInclude(d => d.Product)
					.ThenInclude(p => p.OrderDetails);

				// var efResult = await query.ToArrayAsync();
				var result = await query.ToLinqToDB().ToArrayAsync();

				var orderDetail = result[0].OrderDetails.First();
				orderDetail.UnitPrice *= 1.1m;

				ctx.ChangeTracker.DetectChanges();
				var changedEntry = ctx.ChangeTracker.Entries().SingleOrDefault(e => e.State == EntityState.Modified);
				Assert.That(changedEntry, Is.Null);
				await ctx.SaveChangesAsync();
			}
			finally
			{
				LinqToDBForEFTools.EnableChangeTracker = true;
			}
		}

		// TODO: reenable after fix
		//[Test]
		//public async Task TestChangeTrackerTemporaryTable([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider, [Values] bool enableFilter)
		//{
		//	using var ctx = CreateContext(provider, enableFilter);

		//	var query = ctx.Orders;

		//	using var db = ctx.CreateLinqToDBConnection();

		//	using var temp = await db.CreateTempTableAsync(query, tableName: "#Orders");

		//	var result = temp.Take(2).ToList();

		//	ctx.Orders.Local.Should().BeEmpty();
		//}

		[Test]
		public void NavigationProperties([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query =
				from o in ctx.Orders
				from od in o.OrderDetails
				select new
				{
					ProductOrderDetails = od.Product.OrderDetails.Select(d => new {d.OrderId, d.ProductId, d.Quantity }).ToArray(),
					OrderDetail = new { od.OrderId, od.ProductId, od.Quantity },
					Product = new { od.Product.ProductId, od.Product.ProductName }
				};

			var efResult   = query.ToArray();
			var l2dbResult = query.ToLinqToDB().ToArray();

			// order child collection to avoid assert failures
			efResult = efResult.Select(e => new { ProductOrderDetails = e.ProductOrderDetails.OrderBy(r => r.OrderId).ThenBy(r => r.ProductId).ToArray(), e.OrderDetail, e.Product }).ToArray();
			l2dbResult = l2dbResult.Select(e => new { ProductOrderDetails = e.ProductOrderDetails.OrderBy(r => r.OrderId).ThenBy(r => r.ProductId).ToArray(), e.OrderDetail, e.Product }).ToArray();

			AreEqualWithComparer(efResult, l2dbResult);
		}

		[Test]
		public async Task TestSetUpdate([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var customer = await ctx.Customers.FirstAsync();

			var updatable = ctx.Customers.Where(c => c.CustomerId == customer.CustomerId)
					.Set(c => c.CompanyName, customer.CompanyName);

			var affected = await updatable
					.UpdateAsync();
		}

		[Test]
		public async Task FromSqlRaw([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using var ctx = CreateContext(provider);

			var id = 1;
			var query = ctx.Categories.FromSqlRaw("SELECT * FROM [dbo].[Categories] WHERE CategoryId = {0}", id);


			var efResult = await query.ToArrayAsyncEF();
			var linq2dbResult = await query.ToArrayAsyncLinqToDB();
		}

		[Test]
		public async Task FromSqlRaw2([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using var ctx = CreateContext(provider);

			var id = 1;
			var query = from c1 in ctx.Categories
						from c2 in ctx.Categories.FromSqlRaw("SELECT * FROM [dbo].[Categories] WHERE CategoryId = {0}", id)
						select c2;

			var efResult = await query.ToArrayAsyncEF();
			var linq2dbResult = await query.ToArrayAsyncLinqToDB();
		}

		[Test]
		public async Task FromSqlInterpolated([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using var ctx = CreateContext(provider);

			var id = 1;
			var query = ctx.Categories.FromSqlInterpolated($"SELECT * FROM [dbo].[Categories] WHERE CategoryId = {id}");

			var efResult = await query.AsNoTracking().ToArrayAsyncEF();
			var linq2dbResult = await query.ToArrayAsyncLinqToDB();
		}

		[Test]
		public async Task FromSqlInterpolated2([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using var ctx = CreateContext(provider);

			var id = 1;
			var query = from c1 in ctx.Categories
						from c2 in ctx.Categories.FromSqlInterpolated($"SELECT * FROM [dbo].[Categories] WHERE CategoryId = {id}")
						select c2;

			var efResult = await query.AsNoTracking().ToArrayAsyncEF();
			var linq2dbResult = await query.AsNoTracking().ToArrayAsyncLinqToDB();
		}

		[Test]
		// TODO: reenable after fix
		//public async Task TestDeleteFrom([EFDataSources] string provider)
		public async Task TestDeleteFrom([EFDataSources(TestProvName.AllSQLite, TestProvName.AllMySql, TestProvName.AllPostgreSQL)] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Customers.Where(x => x.IsDeleted).Take(20);

			var affected = await query
				.Where(x => query
					.Select(y => y.CustomerId)
					.Contains(x.CustomerId) && false
				)
				.ToLinqToDB()
				.DeleteAsync();
		}

		[Test]
		public void TestNullability([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			int? test = 1;
			var query = ctx.Employees.Where(e => e.EmployeeId == test);

			var expected = query.ToArray();
			var actual = query.ToLinqToDB().ToArray();

			AreEqualWithComparer(expected, actual);
		}

		[Test]
		public void TestUpdate([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			int? test = 1;
			ctx.Employees.IgnoreQueryFilters().Where(e => e.EmployeeId == test).Update(x => new Employee
			{
				Address = x.Address
			});
		}

		[Test]
		public async Task TestUpdateAsync([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			int? test = 1;
			await ctx.Employees.IgnoreQueryFilters().Where(e => e.EmployeeId == test).UpdateAsync(x => new Employee
			{
				Address = x.Address

			});
		}

		// TODO: reenable after fix
		//[Test]
		//public void TestCreateTempTable([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider, [Values] bool enableFilter)
		//{
		//	using var ctx = CreateContext(provider, enableFilter);

		//	using var db = ctx.CreateLinqToDBContext();
		//	using var temp = db.CreateTempTable(ctx.Employees, "#TestEmployees");

		//	Assert.That(temp.Count(), Is.EqualTo(ctx.Employees.Count()));
		//}

		[Test]
		public void TestForeignKey([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var resultEF = ctx.Employees.Include(e => e.ReportsToNavigation).ToArray();
			var result = ctx.Employees.Include(e => e.ReportsToNavigation).ToLinqToDB().ToArray();

			AreEqual(resultEF, result);
		}

		[Test]
		public void TestCommandTimeout([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			var timeoutErrorCode = -2;     // Timeout Expired
			var commandTimeout = 1;
			var commandExecutionTime = 5;
			var createProcessLongFunctionSql =   // function that takes @secondsNumber seconds
				@"CREATE OR ALTER FUNCTION dbo.[ProcessLong]
					(
						@secondsNumber int
					)
					RETURNS int
					AS
					BEGIN
						declare @startTime datetime = getutcdate()
						while datediff(second, @startTime, getutcdate()) < @secondsNumber
						begin
							set @startTime = @startTime
						end
						return 1
					END";
			var dropProcessLongFunctionSql = @"DROP FUNCTION IF EXISTS [dbo].[ProcessLong]";

			using var ctx = CreateContext(provider);

			try
			{
				ctx.Database.ExecuteSqlRaw(createProcessLongFunctionSql);
				ctx.Database.SetCommandTimeout(commandTimeout);

				var query = from p in ctx.Products
							select NorthwindContext.ProcessLong(commandExecutionTime);

				var exception = Assert.Throws<Microsoft.Data.SqlClient.SqlException>(() =>
					{
						var result = query.ToLinqToDB().First();
					})!;
				Assert.That(timeoutErrorCode, Is.EqualTo(exception.Number));
			}
			finally
			{
				ctx.Database.ExecuteSqlRaw(dropProcessLongFunctionSql);
			}
		}

		[Test]
		public void TestTagWith([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query = ctx.Employees.Include(e => e.ReportsToNavigation).TagWith("Tagged query");
			var resultEF = query.ToArray();
			var result = query.ToLinqToDB().ToArray();

			var str = query.ToLinqToDB().ToString();

			AreEqual(resultEF, result);

			str.Should().Contain("Tagged query");
		}

#if !NETFRAMEWORK
		[Test]
		public void TestTemporalTables([EFDataSources] string provider, [Values] bool enableFilter)
		{
			using var ctx = CreateContext(provider, enableFilter);

			var query1 = ctx.Products.TemporalAsOf(DateTime.UtcNow);
			var query2 = ctx.Products.TemporalFromTo(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
			var query3 = ctx.Products.TemporalAll();
			var query4 = ctx.Products.TemporalBetween(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
			var query5 = ctx.Products.TemporalContainedIn(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

			var result1 = query1.ToLinqToDB().ToArray();
			var result2 = query2.ToLinqToDB().ToArray();
			var result3 = query3.ToLinqToDB().ToArray();
			var result4 = query4.ToLinqToDB().ToArray();
			var result5 = query5.ToLinqToDB().ToArray();

			var allQuery =
				from p in ctx.Products.ToLinqToDB()
				from q1 in ctx.Products.TemporalAsOf(DateTime.UtcNow).Where(q => q.ProductId == p.ProductId).DefaultIfEmpty()
				from q2 in ctx.Products.TemporalFromTo(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow).Where(q => q.ProductId == p.ProductId).DefaultIfEmpty()
				from q3 in ctx.Products.TemporalAll().Where(q => q.ProductId == p.ProductId).DefaultIfEmpty()
				from q4 in ctx.Products.TemporalBetween(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow).Where(q => q.ProductId == p.ProductId).DefaultIfEmpty()
				from q5 in ctx.Products.TemporalContainedIn(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow).Where(q => q.ProductId == p.ProductId).DefaultIfEmpty()
				select p;

			var result = allQuery.ToArray();
		}
#endif
	}
}
