using System;

using LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping;
using LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.ForMapping;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL
{
	[TestFixture]
	public class ForMappingTests : ForMappingTestsBase
	{
		private bool _isDbCreated;

		protected override ForMappingContextBase CreateContext(Func<DataOptions, DataOptions>? optionsSetter = null)
		{
			var optionsBuilder = new DbContextOptionsBuilder<ForMappingContext>();
			//optionsBuilder.UseNpgsql("Server=DBHost;Port=5432;Database=ForMapping;User Id=postgres;Password=TestPassword;Pooling=true;MinPoolSize=10;MaxPoolSize=100;");
			optionsBuilder.UseNpgsql("Server=localhost;Port=5415;Database=ForMapping;User Id=postgres;Password=Password12!;Pooling=true;MinPoolSize=10;MaxPoolSize=100;");
			optionsBuilder.UseLoggerFactory(LoggerFactory);

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

		//Disabled, we cannot create such identity table.
		public override void TestBulkCopyWithIdentity()
		{
		}

	}
}
