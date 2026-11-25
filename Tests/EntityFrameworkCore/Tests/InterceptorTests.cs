using System.Linq;
using System.Linq.Expressions;

using LinqToDB.EntityFrameworkCore.Tests.Interceptors;
using LinqToDB.EntityFrameworkCore.Tests.Interceptors.Extensions;
using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using NUnit.Framework;

using Shouldly;

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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_testCommandInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testConnectionInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testEntityServiceInterceptor.HasInterceptorBeenInvoked, Is.True);

				//#5364: the implicit ToLinqToDB() context now closes its connection after each
				//command (CloseAfterUse), so the data-context close interceptor is invoked
				Assert.That(_testDataContextInterceptor.HasInterceptorBeenInvoked, Is.True);
			}
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_testCommandInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testDataContextInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testConnectionInterceptor.HasInterceptorBeenInvoked, Is.True);
				Assert.That(_testEntityServiceInterceptor.HasInterceptorBeenInvoked, Is.True);
			}
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

		/// <summary>
		/// Filters the exposed query by the limit it holds, if any, baked in as a constant: its rewrites depend on more
		/// than the query and the EF model.
		/// </summary>
		sealed class LimitingImplementation : LinqToDBForEFToolsImplDefault
		{
			public int? Limit { get; set; }

			public override Expression TransformExpression(Expression expression, IDataContext? dc, DbContext? ctx, IModel? model, bool isQueryExpression)
			{
				expression = base.TransformExpression(expression, dc, ctx, model, isQueryExpression);

				if (isQueryExpression || Limit == null || expression.Type != typeof(IQueryable<int>))
					return expression;

				var id = Expression.Parameter(typeof(int), "id");

				return Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Where),
					[typeof(int)],
					expression,
					Expression.Quote(Expression.Lambda(Expression.LessThanOrEqual(id, Expression.Constant(Limit.Value)), id)));
			}
		}

		// The default implementation's query interceptor declares a cache key, so the queries it translates are shared
		// as they are without it.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303")]
		public void DefaultImplementationInterceptorHasCacheKey([EFDataSources] string provider)
		{
			try
			{
				using var ctx = CreateContext(provider);
				using var db  = ctx.CreateLinqToDBConnection();

				((IInterceptable<IQueryExpressionInterceptor>)db).Interceptor
					.ShouldBeAssignableTo<IInterceptorCacheKey>();
			}
			finally
			{
				// The metadata reader cached for the model keeps this context's services, and the MySQL provider needs
				// them alive when it first translates a method, which this context never does.
				LinqToDBForEFTools.ClearCaches();
			}
		}

		// A custom implementation is not trusted to rewrite queries alike for every context, so a query translated for
		// one is not reused for another without checking the rewrite.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303")]
		public void CustomImplementationRewritesAreNotShared([EFDataSources] string provider)
		{
			var original       = LinqToDBForEFTools.Implementation;
			var implementation = new LimitingImplementation();

			LinqToDBForEFTools.Implementation = implementation;

			try
			{
				for (var i = 0; i < 2; i++)
				{
					foreach (var limit in new int?[] { null, 2 })
					{
						implementation.Limit = limit;

						using var ctx = CreateContext(provider);
						using var db  = ctx.CreateLinqToDBConnection();

						var expected = ctx.Products
							.IgnoreQueryFilters()
							.Select(p => p.ProductId)
							.Where(id => limit == null || id <= limit)
							.OrderBy(id => id)
							.ToArray();

						db.GetTable<Product>()
							.Select(p => p.ProductId)
							.ToArray()
							.OrderBy(id => id)
							.ToArray()
							.ShouldBe(expected, $"limit {limit?.ToString() ?? "none"}, round {i}");
					}
				}
			}
			finally
			{
				LinqToDBForEFTools.Implementation = original;
			}
		}
	}
}
