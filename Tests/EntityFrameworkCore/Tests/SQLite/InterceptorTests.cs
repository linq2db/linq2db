using System.Data.Common;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.EntityFrameworkCore.Tests.Interceptors;
using LinqToDB.EntityFrameworkCore.Tests.Interceptors.Extensions;
using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;
using LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.Northwind;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.SQLite
{
	[TestFixture]
	public class InterceptorTests : TestBase
	{
		private const string SQLITE_CONNECTION_STRING = "DataSource=NorthwindInMemory;Mode=Memory;Cache=Shared";
		private readonly DbContextOptions _northwindOptions;
		private readonly DbContextOptions _northwindOptionsWithEfCoreInterceptorsOnly;
		private DbConnection? _dbConnection;
		static TestCommandInterceptor _testCommandInterceptor = new();
		static TestDataContextInterceptor _testDataContextInterceptor = new();
		static TestConnectionInterceptor _testConnectionInterceptor = new();
		static TestEntityServiceInterceptor _testEntityServiceInterceptor = new();
		static TestEfCoreAndLinqToDBComboInterceptor _testEfCoreAndLinqToDBInterceptor = new();

		static InterceptorTests()
		{
			LinqToDBForEFTools.Initialize();
			DataConnection.TurnTraceSwitchOn();
		}

		static DbContextOptions<NorthwindContext> CreateNorthwindOptions()
		{
			var optionsBuilder = new DbContextOptionsBuilder<NorthwindContext>();
			optionsBuilder.UseSqlite(SQLITE_CONNECTION_STRING);
			optionsBuilder.UseLinqToDB(builder =>
			{
				builder
					.AddInterceptor(_testCommandInterceptor)
					.AddInterceptor(_testDataContextInterceptor)
					.AddInterceptor(_testConnectionInterceptor)
					.AddInterceptor(_testEntityServiceInterceptor)
					.AddInterceptor(_testEfCoreAndLinqToDBInterceptor)
					.AddInterceptor(_testCommandInterceptor); //for checking the aggregated interceptors
			});
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			return optionsBuilder.Options;
		}

		static DbContextOptions<NorthwindContext> CreateNorthwindOptionsWithEfCoreInterceptorsOnly()
		{
			var optionsBuilder = new DbContextOptionsBuilder<NorthwindContext>();
			optionsBuilder.UseSqlite(SQLITE_CONNECTION_STRING);
			optionsBuilder.AddInterceptors(_testEfCoreAndLinqToDBInterceptor);
			optionsBuilder.UseLinqToDB(builder => builder.UseEfCoreRegisteredInterceptorsIfPossible());
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			return optionsBuilder.Options;
		}

		public InterceptorTests()
		{
			_northwindOptions = CreateNorthwindOptions();
			_northwindOptionsWithEfCoreInterceptorsOnly = CreateNorthwindOptionsWithEfCoreInterceptorsOnly();
		}

		private NorthwindContext CreateContext()
		{
			var ctx = new NorthwindContext(_northwindOptions);
			return ctx;
		}

		private NorthwindContext CreateContextWithoutLinqToDBExtensions()
		{
			var ctx = new NorthwindContext(_northwindOptionsWithEfCoreInterceptorsOnly);
			return ctx;
		}

		[SetUp]
		public void Setup()
		{
			_dbConnection = new SqliteConnection(SQLITE_CONNECTION_STRING);
			_dbConnection.Open();
			using var ctx = new NorthwindContext(_northwindOptions);
			ctx.Database.EnsureDeleted();
			if (ctx.Database.EnsureCreated())
			{
				NorthwindData.Seed(ctx);
			}
			var options = ctx.GetLinqToDBOptions();
			if (options?.DataContextOptions.Interceptors != null)
			{
				foreach (var interceptor in options.DataContextOptions.Interceptors)
				{
					((TestInterceptor)interceptor).ResetInvocations();
				}
			}

			using var ctx2 = new NorthwindContext(_northwindOptionsWithEfCoreInterceptorsOnly);
			var options2 = ctx2.GetLinqToDBOptions();
			if (options2?.DataContextOptions.Interceptors != null)
			{
				foreach (var interceptor in options2.DataContextOptions.Interceptors)
				{
					((TestInterceptor)interceptor).ResetInvocations();
				}
			}
		}

		public override void OnAfterTest()
		{
			_dbConnection?.Close();
			base.OnAfterTest();
		}

		[Test]
		public void TestInterceptors()
		{
			using (var ctx = CreateContext())
			{
				var query =
					from pd in ctx.Products
					where pd.ProductId > 0
					orderby pd.ProductId
					select pd;
				var items = query.Take(2).ToLinqToDB().ToArray();
			}

			Assert.Multiple(() =>
			{
				Assert.That(_testCommandInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testConnectionInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testEntityServiceInterceptor.HasInterceptorBeenInvoked, Is.True);

				//the following check is false because linq2db context is never closed together
				//with the EF core context
				Assert.That(_testDataContextInterceptor.HasInterceptorBeenInvoked, Is.False);
			});
		}

		[Test]
		public void TestExplicitDataContextInterceptors()
		{
			using (var ctx = CreateContext())
			{
				using var linqToDBContext = ctx.CreateLinqToDBContext();
				var query =
					from pd in ctx.Products
					where pd.ProductId > 0
					orderby pd.ProductId
					select pd;
				var items = query.Take(2).ToLinqToDB(linqToDBContext).ToArray();
				var items2 = query.Take(2).ToLinqToDB(linqToDBContext).ToArray();
			}

			Assert.Multiple(() =>
			{
				Assert.That(_testCommandInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testDataContextInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testConnectionInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testEntityServiceInterceptor.HasInterceptorBeenInvoked, Is.True);
			});
		}

		[Test]
		public void TestEfCoreSideOfComboInterceptor()
		{
			using (var ctx = CreateContextWithoutLinqToDBExtensions())
			{
				var query =
					from pd in ctx.Products
					where pd.ProductId > 0
					orderby pd.ProductId
					select pd;
				var items = query.Take(2).ToArray();
			}
			Assert.That(_testEfCoreAndLinqToDBInterceptor.HasInterceptorBeenInvoked, Is.True);
		}

		[Test]
		public void TestLinqToDBSideOfComboInterceptor()
		{
			using (var ctx = CreateContextWithoutLinqToDBExtensions())
			{
				var query =
					from pd in ctx.Products
					where pd.ProductId > 0
					orderby pd.ProductId
					select pd;
				var items = query.Take(2).ToLinqToDB().ToArray();
			}
			Assert.That(_testEfCoreAndLinqToDBInterceptor.HasInterceptorBeenInvoked, Is.True);
		}
	}
}
