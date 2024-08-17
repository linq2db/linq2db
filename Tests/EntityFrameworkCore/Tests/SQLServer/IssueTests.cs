using System.Diagnostics.CodeAnalysis;
using System.Linq;

using FluentAssertions;

using LinqToDB.DataProvider.SqlServer;
using LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.IssueModel;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer
{
	[TestFixture]
	public class IssueTests : TestBase
	{
		private DbContextOptions<IssueContext> _options;
		private bool _created;

		public IssueTests()
		{
			InitOptions();
		}

		[MemberNotNull(nameof(_options))]
		private void InitOptions()
		{
			var optionsBuilder = new DbContextOptionsBuilder<IssueContext>();

			optionsBuilder.UseSqlServer(Settings.IssuesConnectionString);
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			_options = optionsBuilder.Options;
		}

		private IssueContext CreateContext()
		{
			var ctx = new IssueContext(_options);

			if (!_created)
			{
				//ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
				_created = true;
			}

			return ctx;
		}


		[Test]
		public void Issue73Test()
		{
			using var ctx = CreateContext();

			var q = ctx.Issue73Entities
				.Where(x => x.Name == "Name1_3")
				.Select(x => x.Parent!.Name + ">" + x.Name);

			var efItems = q.ToList();
			var linq2dbItems = q.ToLinqToDB().ToList();

			AreEqual(efItems, linq2dbItems);
		}

		[Test]
		public void Issue117Test()
		{
			using var ctx = CreateContext();

			var userId = 1;

			var query =
				from p in ctx.Patents.Include(p => p.Assessment)
				where p.Assessment == null || (p.Assessment.TechnicalReviewerId != userId)
				select new { PatentId = p.Id, UserId = userId };

			var resultEF = query.ToArray();

			using var db = ctx.CreateLinqToDBConnection();

			_ = query.ToLinqToDB(db).ToArray();

			Assert.That(db.LastQuery, Does.Not.Contain("INNER"));
		}

		[Test]
		public void Issue321Test()
		{
			using var ctx = CreateContext();

			var _ = ctx.Patents.AsSqlServer().ToLinqToDB().ToArray();
		}

#if !NETFRAMEWORK
		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/345")]
		public void AsNoTrackingWithIdentityResolutionHandling()
		{
			using var ctx = CreateContext();

			_ = ctx.Patents.AsNoTrackingWithIdentityResolution().ToLinqToDB().ToArray();
		}
#endif
	}
}
