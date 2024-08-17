using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer.ValueConversion
{
	[TestFixture]
	public class ConvertorTests : TestBase
	{
		private DbContextOptions<ConvertorContext> _options;

		private sealed class ConvertorContext : DbContext
		{
			public ConvertorContext(DbContextOptions options) : base(options)
			{
			}

			[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
			public DbSet<SubDivision> Subdivisions { get; set; } = null!;
		}

		public ConvertorTests()
		{
			var optionsBuilder = new DbContextOptionsBuilder<ConvertorContext>();

			optionsBuilder
				.ReplaceService<IValueConverterSelector, IdValueConverterSelector>()
				.UseSqlServer(Settings.ConverterConnectionString)
				.UseLoggerFactory(LoggerFactory);

			_options = optionsBuilder.Options;
		}


		[Test]
		public void TestToList()
		{
			using (var ctx = new ConvertorContext(_options))
			using (var db = ctx.CreateLinqToDBConnection())
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();


				var result = db.InsertWithInt64Identity(new SubDivision()
					{ Code = "C1", Id = new Id<SubDivision, long>(0), Name = "N1", PermanentId = Guid.NewGuid() });

				result = db.InsertWithInt64Identity(new SubDivision()
					{ Code = "C2", Id = new Id<SubDivision, long>(1), Name = "N2", PermanentId = Guid.NewGuid() });

				result = db.InsertWithInt64Identity(new SubDivision()
					{ Code = "C3", Id = new Id<SubDivision, long>(2), Name = "N3", PermanentId = Guid.NewGuid() });
			
				var ef   = ctx.Subdivisions.Where(s => s.Id == 1L).ToArray();
				var result1 = ctx.Subdivisions.ToLinqToDB().Where(s => s.Id == 1L).ToArray();
				
				var id = new Id<SubDivision, long>?(0L.AsId<SubDivision>());
				var result2 = ctx.Subdivisions.ToLinqToDB().Where(s => s.Id == id).ToArray();
				
				var ids = new[] {1L.AsId<SubDivision>(), 2L.AsId<SubDivision>(),};
				_ = ctx.Subdivisions.ToLinqToDB().Where(s => ids.Contains(s.Id)).ToArray();
				
				_ = ctx.Subdivisions.ToLinqToDB().ToArray();

				Assert.Multiple(() =>
				{
					Assert.That(result1[0].Code, Is.EqualTo(ef[0].Code));
					Assert.That(result1[0].Id, Is.EqualTo(ef[0].Id));

					Assert.That(result2[0].Code, Is.EqualTo(ef[0].Code));
					Assert.That(result2[0].Id, Is.EqualTo(ef[0].Id));
				});
			}
		}
	}
}
