using System.Linq;

using FluentAssertions;

using LinqToDB.DataProvider.SqlServer;
using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class IssueTests : ContextTestBase<IssueContext>
	{
		protected override IssueContext CreateProviderContext(string provider, DbContextOptions<IssueContext> options)
		{
			return new IssueContext(options);
		}

		[Test]
		public void Issue73Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var q = ctx.Issue73Entities
				.Where(x => x.Name == "Name1_3")
				.Select(x => x.Parent!.Name + ">" + x.Name);

			var efItems = q.ToList();
			var linq2dbItems = q.ToLinqToDB().ToList();

			AreEqual(efItems, linq2dbItems);
		}

		[Test]
		public void Issue117Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

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
		public void Issue321Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var _ = ctx.Patents.AsSqlServer().ToLinqToDB().ToArray();
		}

#if !NETFRAMEWORK
		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/345")]
		public void AsNoTrackingWithIdentityResolutionHandling([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			_ = ctx.Patents.AsNoTrackingWithIdentityResolution().ToLinqToDB().ToArray();
		}
#endif
	}
}
