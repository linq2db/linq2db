using System;
using LinqToDB.EntityFrameworkCore.BaseTests;
using LinqToDB.EntityFrameworkCore.BaseTests.Models.ForMapping;
using LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.ForMapping;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.SQLite
{
	[TestFixture]
	public class ForMappingTests : ForMappingTestsBase
	{
		protected override ForMappingContextBase CreateContext(Func<DataOptions, DataOptions>? optionsSetter = null)
		{
			var optionsBuilder = new DbContextOptionsBuilder<ForMappingContext>();
			optionsBuilder.UseSqlite("DataSource=:memory:");
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);

			if (optionsSetter! != null)
				optionsBuilder.UseLinqToDB(builder => builder.AddCustomOptions(optionsSetter));

			var options = optionsBuilder.Options;
			var ctx = new ForMappingContext(options);

			ctx.Database.OpenConnection();
			ctx.Database.EnsureCreated();

			return ctx;
		}
	}
}
