using System;
using System.IO;
using System.Linq;

using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NHibernate;
using NHibernate.Tool.hbm2ddl;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises the core attach path: linq2db querying over an open NHibernate <see cref="ISession"/>'s
	/// ADO connection, using mapping metadata synthesized from NHibernate by <c>NHMetadataReader</c>.
	/// Uses a file-based SQLite database (System.Data.SQLite / NHibernate SQLite20Driver).
	/// </summary>
	[TestFixture]
	public class AttachPathTests
	{
		string          _dbFile = null!;
		ISessionFactory _sf     = null!;

		[OneTimeSetUp]
		public void Setup()
		{
			_dbFile = Path.Combine(Path.GetTempPath(), "nh_l2db_" + Guid.NewGuid().ToString("N") + ".db");

			// A real SQLite file backs this fixture, so NHibernate can connect at BuildSessionFactory
			// time and import the dialect's reserved words; that enables auto-quoting of identifiers
			// like "Order" in the generated DDL (SchemaExport). Do NOT set hbm2ddl.keywords=none here.
			var cfg = Fluently.Configure()
				.Database(SQLiteConfiguration.Standard.UsingFile(_dbFile))
				.Mappings(m => m.FluentMappings.AddFromAssembly(typeof(AttachPathTests).Assembly))
				.BuildConfiguration();

			_sf = cfg.BuildSessionFactory();
			new SchemaExport(cfg).Create(false, true);
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_sf?.Dispose();
			if (_dbFile != null && File.Exists(_dbFile))
				File.Delete(_dbFile);
		}

		[Test]
		public void GetTable_RunsSqlThroughSessionConnection()
		{
			using var session = _sf.OpenSession();
			using var db      = session.CreateLinqToDbConnection();

			// Empty table, but this builds and executes real SQL over the session's ADO connection.
			var customers = db.GetTable<Customer>().ToList();

			customers.ShouldNotBeNull();
		}

		[Test]
		public void GetTable_ReturnsRowInsertedViaNHibernate()
		{
			const string id = "ALFKI";

			using (var session = _sf.OpenSession())
			using (var tx      = session.BeginTransaction())
			{
				session.Save(new Customer { CustomerId = id, CompanyName = "Alfreds Futterkiste" });
				tx.Commit();
			}

			using (var session = _sf.OpenSession())
			using (var db      = session.CreateLinqToDbConnection())
			{
				var names = db.GetTable<Customer>()
					.Where (c => c.CustomerId == id)
					.Select(c => c.CompanyName)
					.ToList();

				names.ShouldHaveSingleItem();
				names[0].ShouldBe("Alfreds Futterkiste");
			}
		}
	}
}
