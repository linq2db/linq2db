using System.Linq;

using LinqToDB.EntityFrameworkCore.Tests.Interceptors;
using LinqToDB.EntityFrameworkCore.Tests.Interceptors.Extensions;
using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class InterceptorTests : NorthwindContextTestBase
	{
		static TestCommandInterceptor                _testCommandInterceptor           = new();
		static TestDataContextInterceptor            _testDataContextInterceptor       = new();
		static TestConnectionInterceptor             _testConnectionInterceptor        = new();
		static TestEntityServiceInterceptor          _testEntityServiceInterceptor     = new();
		static TestEfCoreAndLinqToDBComboInterceptor _testEfCoreAndLinqToDBInterceptor = new();

		static DbContextOptionsBuilder<NorthwindContextBase> CreateNorthwindOptions(DbContextOptionsBuilder<NorthwindContextBase> optionsBuilder)
		{
			return optionsBuilder.UseLinqToDB(builder =>
			{
				builder
					.AddInterceptor(_testCommandInterceptor)
					.AddInterceptor(_testDataContextInterceptor)
					.AddInterceptor(_testConnectionInterceptor)
					.AddInterceptor(_testEntityServiceInterceptor)
					.AddInterceptor(_testEfCoreAndLinqToDBInterceptor)
					.AddInterceptor(_testCommandInterceptor); //for checking the aggregated interceptors
			});
		}

		static DbContextOptionsBuilder<NorthwindContextBase> CreateNorthwindOptionsWithEfCoreInterceptorsOnly(DbContextOptionsBuilder<NorthwindContextBase> optionsBuilder)
		{
			return ((DbContextOptionsBuilder<NorthwindContextBase>)optionsBuilder
				.AddInterceptors(_testEfCoreAndLinqToDBInterceptor))
				.UseLinqToDB(builder => builder.UseEfCoreRegisteredInterceptorsIfPossible());
		}

		[SetUp]
		public void Setup()
		{
			_testCommandInterceptor.ResetInvocations();
			_testDataContextInterceptor.ResetInvocations();
			_testConnectionInterceptor.ResetInvocations();
			_testEntityServiceInterceptor.ResetInvocations();
			_testEfCoreAndLinqToDBInterceptor.ResetInvocations();
		}

		[Test]
		public void TestInterceptors([EFDataSources] string provider)
		{
			using (var ctx = CreateContext(provider, optionsBuilderSetter: CreateNorthwindOptions))
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
		public void TestExplicitDataContextInterceptors([EFDataSources] string provider)
		{
			using (var ctx = CreateContext(provider, optionsBuilderSetter: CreateNorthwindOptions))
			using (var linqToDBContext = ctx.CreateLinqToDBContext())
			{
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
		public void TestEfCoreSideOfComboInterceptor([EFDataSources] string provider)
		{
			using (var ctx = CreateContext(provider, optionsBuilderSetter: CreateNorthwindOptionsWithEfCoreInterceptorsOnly))
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
		public void TestLinqToDBSideOfComboInterceptor([EFDataSources] string provider)
		{
			using (var ctx = CreateContext(provider, optionsBuilderSetter: CreateNorthwindOptionsWithEfCoreInterceptorsOnly))
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
