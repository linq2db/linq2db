using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentNHibernate.Cfg;
using LinqToDB.NHibernateExtension.BaseTests;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace LinqToDB.NHibernateExtension.SqlServer.Tests
{
	public class SqlServerTests : TestsBase
	{
		private NHibernate.Cfg.Configuration _configuration;
		private ISessionFactory _sessionFactory;

		public SqlServerTests()
		{
			var connectionString = "Server=.;Database=NorthwindNH;Integrated Security=SSPI";

			// Standard Fluent NHibernate configuration
			var fluentConfig = Fluently.Configure()
				.Database(FluentNHibernate.Cfg.Db.MsSqlConfiguration.MsSql2008.ConnectionString(connectionString)
					.UseReflectionOptimizer()
					.AdoNetBatchSize(10)
					//.ShowSql()
					.FormatSql()
					.IsolationLevel(System.Data.IsolationLevel.ReadCommitted)
				)

				// This is the default in NH 3.2 but fluent tries to use castle without this
				.ProxyFactoryFactory(typeof(NHibernate.Bytecode.DefaultProxyFactoryFactory))

				// Add the new fluent mappings
				.Mappings(x => x.FluentMappings
					.AddFromAssembly(typeof(TestsBase).Assembly)
					.Conventions.AddAssembly(typeof(TestsBase).Assembly)

				);

			_configuration = fluentConfig.BuildConfiguration();

			_configuration.SetInterceptor(new NhQueryHint.NhSqlAppenderInterceptor());

			if (_configuration == null)
				throw new Exception("Cannot build NHibernate configuration");

			_sessionFactory = _configuration.BuildSessionFactory();
		}


		[Test]
		public void LoadWithTests()
		{
			using var session = _sessionFactory.OpenSession();
			using var db = session.CreateLinqToDbConnection();

			var result = db.GetTable<Customer>()
				.LoadWith(x => x.Orders)
					.ThenLoad(x => x.Customer)
				.LoadWith(x => x.Orders)
					.ThenLoad(x => x.OrderDetails)
				.Where(c => c.Orders.Any()).ToList();
		}

		[Test]
		public void CompositeKeyTests()
		{
			using var session = _sessionFactory.OpenSession();
			using var db = session.CreateLinqToDbConnection();

			var result = db.GetTable<OrderDetail>()
				.LoadWith(x => x.Order)
				.ToList();
		}

		[Test]
		public void QueryHintTests()
		{
			using var session = _sessionFactory.OpenSession();
			using var db = session.CreateLinqToDbConnection();

			using (NhQueryHint.Recompile())
			{
				var result = session.Query<OrderDetail>()
					.ToList();
			}
		}


		[Test]
		public void DistinctTest()
		{
			var od = new OrderDetail[] { new() { Quantity = 1 }, new() { Quantity = 1 }, new() { Quantity = 2 } };

			var result = GetDistinctValue(od, "Quantity").ToList();
		}

		public static IEnumerable<object> GetDistinctValue<T>(IEnumerable<T> records, string propertyName)
		{
			var parameterExpression = Expression.Parameter(typeof(T), "e");
			var body = (Expression)Expression.Property(parameterExpression, propertyName);
			if (body.Type != typeof(object))
			{
				body = Expression.Convert(body, typeof(object));
			}

			var lambda = Expression.Lambda(body, parameterExpression);
	
			// turn IEnumerable into IQueryable
			var queryable = records.AsQueryable();

			var queryExpression = queryable.Expression;


			// records.Select(e => (object)e.propertyName)
			queryExpression = Expression.Call(typeof(Queryable), nameof(Queryable.Select),
				new[] { typeof(T), typeof(object) }, queryExpression, lambda);

			// records.Select(e => (object)e.propertyName).Distinct()

			queryExpression = Expression.Call(typeof(Queryable), nameof(Queryable.Distinct), new[] { typeof(object) },
				queryExpression);

			var resultQuery = queryable.Provider.CreateQuery<object>(queryExpression);

			// turn IQueryable into IEnumerable
			return resultQuery.AsEnumerable();
		}

	}
}
