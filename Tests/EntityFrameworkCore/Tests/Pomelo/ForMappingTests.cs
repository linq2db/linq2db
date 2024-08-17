using System;

using LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping;
using LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.ForMapping;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo
{
	public class ForMappingTests : ForMappingTestsBase
	{
		private bool _isDbCreated;

		protected override ForMappingContextBase CreateContext(Func<DataOptions, DataOptions>? optionsSetter = null)
		{
			var optionsBuilder = new DbContextOptionsBuilder<ForMappingContext>();
			//var connectionString = "Server=DBHost;Port=3306;Database=TestData;Uid=TestUser;Pwd=TestPassword;charset=utf8;";
			var connectionString = "Server=localhost;Port=3316;Database=TestData;Uid=root;Pwd=root;charset=utf8;";
#if !NETFRAMEWORK
			optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
#else
			optionsBuilder.UseMySql(connectionString);
#endif
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
	}
}
