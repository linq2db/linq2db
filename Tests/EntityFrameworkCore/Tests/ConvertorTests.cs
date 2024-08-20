using System;
using System.Linq;

using LinqToDB.EntityFrameworkCore.Tests.Models.ValueConversion;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class ConvertorTests : ContextTestBase<ConvertorContext>
	{
		protected override ConvertorContext CreateProviderContext(string provider, DbContextOptions<ConvertorContext> options)
		{
			return new ConvertorContext(options);
		}

		protected override DbContextOptionsBuilder<ConvertorContext> ProviderSetup(string provider, string connectionString, DbContextOptionsBuilder<ConvertorContext> optionsBuilder)
		{
			return base.ProviderSetup(provider, connectionString, optionsBuilder)
				.ReplaceService<IValueConverterSelector, IdValueConverterSelector>();
		}

		[Test]
		public void TestToList([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

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
