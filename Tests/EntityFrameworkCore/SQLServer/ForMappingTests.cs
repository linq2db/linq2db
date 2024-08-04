using System;
using System.Linq;
using FluentAssertions;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.EntityFrameworkCore.BaseTests;
using LinqToDB.EntityFrameworkCore.BaseTests.Models.ForMapping;
using LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.ForMapping;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer
{
	[TestFixture]
	public class ForMappingTests : ForMappingTestsBase
	{
		private bool _isDbCreated;

		protected override ForMappingContextBase CreateContext(Func<DataOptions, DataOptions>? optionsSetter = null)
		{
			var optionsBuilder = new DbContextOptionsBuilder<ForMappingContext>();
			optionsBuilder.UseSqlServer(Settings.ForMappingConnectionString);
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);

			if (optionsSetter! != null)
				optionsBuilder.UseLinqToDB(builder => builder.AddCustomOptions(optionsSetter));

			var options = optionsBuilder.Options;
			var ctx = new ForMappingContext(options);

			if (!_isDbCreated)
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();

				_isDbCreated = true;
			}

			return ctx;
		}

		[Test]
		public void TestStringMappings()
		{
			using (var db = CreateContext())
			{
				var ms = LinqToDBForEFTools.GetMappingSchema(db.Model, db, null);
				var ed = ms.GetEntityDescriptor(typeof(StringTypes));

				ed.Columns.First(c => c.MemberName == nameof(StringTypes.AsciiString)).DataType.Should()
					.Be(DataType.VarChar);

				ed.Columns.First(c => c.MemberName == nameof(StringTypes.UnicodeString)).DataType.Should()
					.Be(DataType.NVarChar);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/349")]
		public void TestColumnLengthMappings()
		{
			using (var db = CreateContext())
			{
				var ms = LinqToDBForEFTools.GetMappingSchema(db.Model, db, null);
				var ed = ms.GetEntityDescriptor(typeof(TypesTable));

				ed.Columns.First(c => c.MemberName == nameof(TypesTable.DateTime)).Length.Should().BeNull();
				ed.Columns.First(c => c.MemberName == nameof(TypesTable.String)).Length.Should().Be(100);
			}
		}

		[Test]
		public void TestDialectUse()
		{
			using var db = CreateContext(o => o.UseSqlServer("TODO:remove after fix from linq2db (not used)", SqlServerVersion.v2005));
			using var dc = db.CreateLinqToDBConnectionDetached();
			Assert.That(dc.MappingSchema.DisplayID, Does.Contain("2005"));
		}
	}
}
