using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore.BaseTests;
using LinqToDB.EntityFrameworkCore.BaseTests.Models.Northwind;
using LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.Inheritance;
using LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.Northwind;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer
{
	[TestFixture]
	public class ToolsTests : TestsBase
	{
#if NETFRAMEWORK
		private DbContextOptions? _inheritanceOptions;
#endif
		private readonly DbContextOptions _options;
		private readonly DbContextOptions<NorthwindContext> _inMemoryOptions;

		static ToolsTests()
		{
			LinqToDBForEFTools.Initialize();
			DataConnection.TurnTraceSwitchOn();
		}

#if NETFRAMEWORK
		static DbContextOptions CreateInheritanceOptions()
		{
			var optionsBuilder = new DbContextOptionsBuilder<InheritanceContext>();
			//new SqlServerDbContextOptionsBuilder(optionsBuilder);

			optionsBuilder.UseSqlServer("Server=.;Database=InheritanceEFCore;Integrated Security=SSPI");
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);
			optionsBuilder.EnableSensitiveDataLogging();

			return optionsBuilder.Options;
		}
#endif

		public ToolsTests()
		{
			var optionsBuilder = new DbContextOptionsBuilder<NorthwindContext>();
			//new SqlServerDbContextOptionsBuilder(optionsBuilder);

			optionsBuilder.UseSqlServer(Settings.NorthwindConnectionString);
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);

			_options = optionsBuilder.Options;

			optionsBuilder = new DbContextOptionsBuilder<NorthwindContext>();
			//new SqlServerDbContextOptionsBuilder(optionsBuilder);

			optionsBuilder.UseInMemoryDatabase("sample");
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);

			_inMemoryOptions = optionsBuilder.Options;
		}

		private NorthwindContext CreateContextInMemory()
		{
			var ctx = new NorthwindContext(_inMemoryOptions);
			ctx.Database.EnsureCreated();
			return ctx;
		}

		private void SetIdentityInsert(DbContext ctx, string tableName, bool isOn)
		{
			var str = $"SET IDENTITY_INSERT {tableName} " + (isOn ? "ON" : "OFF");
#pragma warning disable CA1031 // Do not catch general exception types
			try
			{
				ctx.Database.ExecuteSqlRaw(str);
			}
			catch
			{
				// swallow
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}

		private NorthwindContext CreateContext(bool enableFilter)
		{
			var ctx = new NorthwindContext(_options);
			ctx.IsSoftDeleteFilterEnabled = enableFilter;
			//ctx.Database.EnsureDeleted();
			if (ctx.Database.EnsureCreated())
			{
				NorthwindData.Seed(ctx);
			}			
			return ctx;
		}

#if NETFRAMEWORK
		private InheritanceContext CreateInheritanceContext()
		{
			var recreate = _inheritanceOptions == null;

			_inheritanceOptions ??= CreateInheritanceOptions();

			var ctx = new InheritanceContext(_inheritanceOptions);
			if (recreate)
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			return ctx;
		}
#endif

		[Test]
		public void TestToList([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			using (var db = ctx.CreateLinqToDBConnection())
			{
				var items = db.GetTable<Order>()
					.LoadWith(d => d.OrderDetails)
					.ThenLoad(d => d.Product).ToList();
			}
		}

		[Test]
		public void TestShadowProperty([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query = ctx.Products.Select(p => new
				{
					Quantity = EF.Property<string>(p, "QuantityPerUnit")
				});

				var expected = query.ToArray();
				var result = query.ToLinqToDB().ToArray();
			}
		}

		IQueryable<Product> ProductQuery(NorthwindContext ctx)
		{
			return ctx.Products.Where(p => p.OrderDetails.Count > 0);
		}

		[Test]
		public void TestCallback([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
#pragma warning disable CA1866 // Use char overload
				var query = ProductQuery(ctx)
					.Where(pd => pd.ProductName.StartsWith("a"));
#pragma warning restore CA1866 // Use char overload

				query.Where(p => p.ProductName == "a").Delete();
			}
		}


		[Test]
		public void TestContextRetrieving([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
#pragma warning disable CA1866 // Use char overload
				var query = ProductQuery(ctx)
					.ToLinqToDB()
					.Where(pd => pd.ProductName.StartsWith("a"));
#pragma warning restore CA1866 // Use char overload
			}
		}

		[Test]
		public void TestDelete([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
#pragma warning disable CA1866 // Use char overload
				var query = ProductQuery(ctx)
					.Where(pd => pd.ProductName.StartsWith("a"));
#pragma warning restore CA1866 // Use char overload
			}
		}

		[Test]
		public void TestNestingFunctions([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query =
					from pd in ProductQuery(ctx)
					from pd2 in ProductQuery(ctx)
					where pd.ProductId == pd2.ProductId
					orderby pd.ProductId
					select new { pd, pd2 };

				var items1 = query.ToArray();
				var items2 = query.ToLinqToDB().ToArray();
			}
		}

		[Test]
		public void TestCreateFromOptions()
		{
			using (var db = _options.CreateLinqToDBConnection())
			{
			}
		}

		[Test]
		public void TestFunctions()
		{
			using (var ctx = CreateContext(false))
			{
				var query = from p in ctx.Orders
					//where EF.Functions.Like(p., "a%") || true
					//orderby p.ProductId
					select new
					{
						p.OrderId,
						// Date = Model.TestFunctions.GetDate(),
						// Len = Model.TestFunctions.Len(p.Name),
						DiffYear1 = SqlServerDbFunctionsExtensions.DateDiffYear(EF.Functions, p.ShippedDate, p.OrderDate),
						DiffYear2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffYear(EF.Functions, p.ShippedDate, p.OrderDate.Value),
						DiffMonth1 = SqlServerDbFunctionsExtensions.DateDiffMonth(EF.Functions, p.ShippedDate, p.OrderDate),
						DiffMonth2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffMonth(EF.Functions, p.ShippedDate, p.OrderDate.Value),
						DiffDay1 = SqlServerDbFunctionsExtensions.DateDiffDay(EF.Functions, p.ShippedDate, p.OrderDate),
						DiffDay2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffDay(EF.Functions, p.ShippedDate, p.OrderDate.Value),
						DiffHour1 = SqlServerDbFunctionsExtensions.DateDiffHour(EF.Functions, p.ShippedDate, p.OrderDate),
						DiffHour2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffHour(EF.Functions, p.ShippedDate, p.OrderDate.Value),
						DiffMinute1 = SqlServerDbFunctionsExtensions.DateDiffMinute(EF.Functions, p.ShippedDate, p.OrderDate),
						DiffMinute2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffMinute(EF.Functions, p.ShippedDate, p.OrderDate.Value),
						DiffSecond1 = SqlServerDbFunctionsExtensions.DateDiffSecond(EF.Functions, p.ShippedDate, p.OrderDate),
						DiffSecond2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffSecond(EF.Functions, p.ShippedDate, p.OrderDate.Value),
						DiffMillisecond1 = SqlServerDbFunctionsExtensions.DateDiffMillisecond(EF.Functions, p.ShippedDate, p.ShippedDate!.Value.AddMilliseconds(100)),
						DiffMillisecond2 = p.OrderDate == null ? null : SqlServerDbFunctionsExtensions.DateDiffMillisecond(EF.Functions, p.ShippedDate, p.ShippedDate.Value.AddMilliseconds(100)),
					};

//				var items1 = query.ToArray();
				var items2 = query.ToLinqToDB().ToArray();
			}
		}

		[Test]
		public async Task TestTransaction([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				await using (var transaction = await ctx.Database.BeginTransactionAsync())
				using (var db = ctx.CreateLinqToDBConnection())
				{

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
			}
		}

		[Test]
		public void TestView([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			using (var db = ctx.CreateLinqToDBConnection())
			{
#pragma warning disable CA1866 // Use char overload
				var query = ProductQuery(ctx)
					.ToLinqToDB(db)
					.Where(pd => pd.ProductName.StartsWith("a"));
#pragma warning restore CA1866 // Use char overload

				var items = query.ToArray();
			}
		}


		[Test]
		public void TestTransformation([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		[Test]
		public void TestTransformationTable([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		[Test]
		public void TestDemo2([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query =
					from p in ctx.Products
					from op in ctx.Products.LeftJoin(op => op.ProductId != p.ProductId && op.ProductName == p.ProductName)
					where Sql.ToNullable(op.ProductId) == null
					select p;

				query = query.ToLinqToDB();

				var str = query.ToString();

				var items = query.ToArray();
			}
		}

		[Test]
		public void TestKey()
		{
			using (var ctx = CreateContext(false))
			{
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
		}

		[Test]
		public void TestAssociations()
		{
			using (var ctx = CreateContext(false))
			{
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
		}

		[Repeat(2)]
		[Test]
		public void TestGlobalQueryFilters([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		public async Task TestAsyncMethods([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		[Test]
		public async Task TestInclude([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query = ctx.Orders
					.Include(o => o.Employee!)
					.ThenInclude(e => e.EmployeeTerritories)
					.ThenInclude(et => et.Territory)
					.Include(o => o.OrderDetails)
					.ThenInclude(d => d.Product);

				var expected = await query.ToArrayAsync();

				var result = await query.ToLinqToDB().ToArrayAsync();
			}
		}

		[Test]
		public async Task TestEager([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		[Test]
		public async Task TestIncludeString([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query = ctx.Orders
					.Include("Employee.EmployeeTerritories")
					.Include(o => o.OrderDetails)
					.ThenInclude(d => d.Product);

				var expected = await query.ToArrayAsync();

				var result = await query.ToLinqToDB().ToArrayAsync();
			}
		}

		[Test]
		public async Task TestLoadFilter([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		[Test]
		public async Task TestGetTable([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query = ctx.GetTable<Customer>()
					.Where(o => o.City != null);

				var expected = await query.ToArrayAsync();
			}
		}

		[Test]
		public void TestInMemory()
		{
			using (var ctx = CreateContextInMemory())
			{
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
		}

		[Test]
		public async Task TestContinuousQueries([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query = ctx.Orders
					.Include(o => o.OrderDetails)
					.ThenInclude(d => d.Product)
					.ThenInclude(p => p.OrderDetails);

				var expected = await query.ToArrayAsync();
				var result   = await query.ToLinqToDB().ToArrayAsync();
			}
		}

		[Test]
		public async Task TestChangeTracker([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		[Test]
		public async Task TestChangeTrackerDisabled1([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		[Test]
		public async Task TestChangeTrackerDisabled2([Values(true, false)] bool enableFilter)
		{
			LinqToDBForEFTools.EnableChangeTracker = false;
			try
			{
				using (var ctx = CreateContext(enableFilter))
				{
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
			}
			finally
			{
				LinqToDBForEFTools.EnableChangeTracker = true;
			}
		}

		[Test]
		public async Task TestChangeTrackerTemporaryTable([Values(true, false)] bool enableFilter)
		{
			using var ctx = CreateContext(enableFilter);

			var query = ctx.Orders;

			using var db = ctx.CreateLinqToDBConnection();

			using var temp = await db.CreateTempTableAsync(query, tableName: "#Orders");

			var result = temp.Take(2).ToList();

			ctx.Orders.Local.Should().BeEmpty();
		}

		[Test]
		public void NavigationProperties()
		{
			using (var ctx = CreateContext(false))
			{
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
				
				AreEqualWithComparer(efResult, l2dbResult);
			}
		}

		[Test]
		public async Task TestSetUpdate([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var customer = await ctx.Customers.FirstAsync();

				var updatable = ctx.Customers.Where(c => c.CustomerId == customer.CustomerId)
					.Set(c => c.CompanyName, customer.CompanyName);

				var affected = await updatable
					.UpdateAsync();
			}
		}

		[Test]
		public async Task FromSqlRaw()
		{
			using (var ctx = CreateContext(false))
			{
				var id = 1;
				var query = ctx.Categories.FromSqlRaw("SELECT * FROM [dbo].[Categories] WHERE CategoryId = {0}", id);


				var efResult = await query.ToArrayAsyncEF();
				var linq2dbResult = await query.ToArrayAsyncLinqToDB();
			}
		}

		[Test]
		public async Task FromSqlRaw2()
		{
			using (var ctx = CreateContext(false))
			{
				var id = 1;
				var query = from c1 in ctx.Categories
					from c2 in ctx.Categories.FromSqlRaw("SELECT * FROM [dbo].[Categories] WHERE CategoryId = {0}", id)
					select c2;

				var efResult = await query.ToArrayAsyncEF();
				var linq2dbResult = await query.ToArrayAsyncLinqToDB();
			}
		}

		[Test]
		public async Task FromSqlInterpolated()
		{
			using (var ctx = CreateContext(false))
			{
				var id = 1;
				var query = ctx.Categories.FromSqlInterpolated($"SELECT * FROM [dbo].[Categories] WHERE CategoryId = {id}");

				var efResult = await query.AsNoTracking().ToArrayAsyncEF();
				var linq2dbResult = await query.ToArrayAsyncLinqToDB();
			}
		}

		[Test]
		public async Task FromSqlInterpolated2()
		{
			using (var ctx = CreateContext(false))
			{
				var id = 1;
				var query = from c1 in ctx.Categories
					from c2 in ctx.Categories.FromSqlInterpolated($"SELECT * FROM [dbo].[Categories] WHERE CategoryId = {id}")
					select c2;

				var efResult = await query.AsNoTracking().ToArrayAsyncEF();
				var linq2dbResult = await query.AsNoTracking().ToArrayAsyncLinqToDB();
			}
		}

		[Test]
		public async Task TestDeleteFrom()
		{
			using (var ctx = CreateContext(false))
			{
				var query = ctx.Customers.Where(x => x.IsDeleted).Take(20);

				var affected = await query
					.Where(x => query
						.Select(y => y.CustomerId)
						.Contains(x.CustomerId) && false
					)
					.ToLinqToDB()
					.DeleteAsync();
			}
		}

		[Test]
		public void TestNullability([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				int? test = 1;
				var query = ctx.Employees.Where(e => e.EmployeeId == test);

				var expected = query.ToArray();
				var actual = query.ToLinqToDB().ToArray();

				AreEqualWithComparer(expected, actual);
			}
		}

		[Test]
		public void TestUpdate([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				int? test = 1;
				ctx.Employees.IgnoreQueryFilters().Where(e => e.EmployeeId == test).Update(x => new Employee
				{
					Address = x.Address

				});
			}
		}

		[Test]
		public async Task TestUpdateAsync([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				int? test = 1;
				await ctx.Employees.IgnoreQueryFilters().Where(e => e.EmployeeId == test).UpdateAsync(x => new Employee
				{
					Address = x.Address

				});
			}
		}

		[Test]
		public void TestCreateTempTable([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				using var db = ctx.CreateLinqToDBContext();
				using var temp = db.CreateTempTable(ctx.Employees, "#TestEmployees");

				Assert.That(temp.Count(), Is.EqualTo(ctx.Employees.Count()));
			}
		}


		[Test]
		public void TestForeignKey([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var resultEF = ctx.Employees.Include(e => e.ReportsToNavigation).ToArray();
				var result = ctx.Employees.Include(e => e.ReportsToNavigation).ToLinqToDB().ToArray();

				AreEqual(resultEF, result);
			}
		}


		[Test]
		public void TestCommandTimeout()
		{
			int timeoutErrorCode = -2;     // Timeout Expired
			int commandTimeout = 1;
			int commandExecutionTime = 5;
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

			using (var ctx = CreateContext(false))
			{
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
		}

		[Test]
		public void TestTagWith([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
				var query = ctx.Employees.Include(e => e.ReportsToNavigation).TagWith("Tagged query");
				var resultEF = query.ToArray();
				var result = query.ToLinqToDB().ToArray();

				var str = query.ToLinqToDB().ToString();

				AreEqual(resultEF, result);

				str.Should().Contain("Tagged query");
			}
		}


#if !NETFRAMEWORK
		[Test]
		public void TestTemporalTables([Values(true, false)] bool enableFilter)
		{
			using (var ctx = CreateContext(enableFilter))
			{
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
		}

		static DbContextOptions<InheritanceContext> CreateInheritanceOptions()
		{
			var optionsBuilder = new DbContextOptionsBuilder<InheritanceContext>();

			optionsBuilder.UseSqlServer(Settings.InheritanceConnectionString);
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);
			optionsBuilder.EnableSensitiveDataLogging();

			return optionsBuilder.Options;
		}

		private DbContextOptions? _inheritanceOptions;

		private InheritanceContext CreateInheritanceContext()
		{
			var recreate = _inheritanceOptions == null;

			_inheritanceOptions ??= CreateInheritanceOptions();

			var ctx = new InheritanceContext(_inheritanceOptions);
			if (recreate)
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			return ctx;
		}
#endif

		[Test]
		public void TestInheritanceBulkCopy([Values] BulkCopyType copyType)
		{
			using (var ctx = CreateInheritanceContext())
			{
				var data = new BlogBase[] { new Blog() { Url = "BlogUrl" }, new RssBlog() { Url = "RssUrl" } };

				ctx.BulkCopy(new BulkCopyOptions(){ BulkCopyType = BulkCopyType.RowByRow }, data);

				var items = ctx.Blogs.ToArray();

				items[0].Should().BeOfType<Blog>();
				((Blog)items[0]).Url.Should().Be("BlogUrl");

				items[1].Should().BeOfType<RssBlog>();
				((RssBlog)items[1]).Url.Should().Be("RssUrl");
			}
		}

		/*
		[Test]
		public void TestInheritanceShadowBulkCopy([Values] BulkCopyType copyType)
		{
			using (var ctx = CreateInheritanceContext())
			{
				var data = new ShadowBlogBase[] { new ShadowBlog() { Url = "BlogUrl" }, new ShadowRssBlog() { Url = "RssUrl" } };

				ctx.BulkCopy(new BulkCopyOptions(){ BulkCopyType = BulkCopyType.RowByRow }, data);

				var items = ctx.ShadowBlogs.ToArray();

				items[0].Should().BeOfType<ShadowBlog>();
				((ShadowBlog)items[0]).Url.Should().Be("BlogUrl");

				items[1].Should().BeOfType<ShadowRssBlog>();
				((ShadowRssBlog)items[1]).Url.Should().Be("RssUrl");
			}
		}
		*/
	}
}
