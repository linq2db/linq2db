using System.Linq;

using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

using LinqToDB.Mapping;
using LinqToDB.NHibernate.Tests.Models.Associations;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NHibernate;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	[TestFixture]
	public class MetadataReaderTests
	{
		static ISessionFactory BuildSessionFactory()
		{
			// Metadata-only: the SessionFactory is built solely to expose NHibernate's mapping
			// metadata to NHMetadataReader; these tests open no database connection, so the
			// dialect/driver need only be instantiable (Microsoft.Data.SqlClient is referenced).
			return Fluently.Configure()
				.Database(MsSqlConfiguration.MsSql2012
					.ConnectionString("Data Source=(local);Initial Catalog=metadata;Integrated Security=True;TrustServerCertificate=True")
					.Driver<global::NHibernate.Driver.MicrosoftDataSqlClientDriver>())
				// Do not connect to the database to import dialect reserved words:
				// these tests only read mapping metadata, they never open a session.
				.ExposeConfiguration(cfg => cfg.SetProperty("hbm2ddl.keywords", "none"))
				.Mappings(m => m.FluentMappings.AddFromAssembly(typeof(MetadataReaderTests).Assembly))
				.BuildSessionFactory();
		}

		[Test]
		public void TableAttribute_IsReadFromMapping()
		{
			using var sf = BuildSessionFactory();

			var reader = LinqToDBForNHibernateTools.GetMetadataReader(sf);
			reader.ShouldNotBeNull();

			var table = reader!.GetAttributes(typeof(Order)).OfType<TableAttribute>().SingleOrDefault();
			table.ShouldNotBeNull();
			table!.Name.ShouldBe("Orders");
		}

		[Test]
		public void ColumnAttribute_IdentityPrimaryKey()
		{
			using var sf = BuildSessionFactory();

			var reader = LinqToDBForNHibernateTools.GetMetadataReader(sf)!;

			var member = typeof(Order).GetProperty(nameof(Order.OrderId))!;
			var col    = reader.GetAttributes(typeof(Order), member).OfType<ColumnAttribute>().SingleOrDefault();

			col.ShouldNotBeNull();
			col!.Name.ShouldBe("OrderID");
			col.IsPrimaryKey.ShouldBeTrue();
			col.IsIdentity.ShouldBeTrue();
		}

		[Test]
		public void AssociationAttribute_OneToMany()
		{
			using var sf = BuildSessionFactory();

			var reader = LinqToDBForNHibernateTools.GetMetadataReader(sf)!;

			var member = typeof(Order).GetProperty(nameof(Order.OrderDetails))!;
			var assoc  = reader.GetAttributes(typeof(Order), member).OfType<AssociationAttribute>().SingleOrDefault();

			assoc.ShouldNotBeNull();
		}

		[Test]
		public void AssociationAttribute_ManyToOne()
		{
			using var sf = BuildSessionFactory();

			var reader = LinqToDBForNHibernateTools.GetMetadataReader(sf)!;

			var member = typeof(Order).GetProperty(nameof(Order.Customer))!;
			var assoc  = reader.GetAttributes(typeof(Order), member).OfType<AssociationAttribute>().SingleOrDefault();

			assoc.ShouldNotBeNull();
		}

		[Test]
		public void AssociationAttribute_ManyToOne_DifferentlyNamedForeignKey()
		{
			using var sf = BuildSessionFactory();

			var reader = LinqToDBForNHibernateTools.GetMetadataReader(sf)!;

			var member = typeof(Widget).GetProperty(nameof(Widget.Gadget))!;
			var assoc  = reader.GetAttributes(typeof(Widget), member).OfType<AssociationAttribute>().SingleOrDefault();

			assoc.ShouldNotBeNull();
			// ThisKey must name the source's foreign-key member (Widget.Gid), not the target's PK member name.
			assoc!.ThisKey.ShouldBe(nameof(Widget.Gid));
			assoc.OtherKey.ShouldBe(nameof(Gadget.GadgetId));
		}

		[Test]
		public void AssociationAttribute_OneToMany_ChildWithoutForeignKeyMember_Declines()
		{
			using var sf = BuildSessionFactory();

			var reader = LinqToDBForNHibernateTools.GetMetadataReader(sf)!;

			var member = typeof(Bin).GetProperty(nameof(Bin.Slots))!;

			// The child exposes no scalar for the BinId foreign key, so the association can't be expressed via
			// ThisKey/OtherKey; reading the metadata must decline gracefully rather than throw.
			AssociationAttribute? assoc = null;
			Should.NotThrow(() => assoc = reader.GetAttributes(typeof(Bin), member).OfType<AssociationAttribute>().SingleOrDefault());
			assoc.ShouldBeNull();
		}
	}
}
