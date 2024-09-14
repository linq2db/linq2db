using System.Linq;
using System.Threading.Tasks;

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

		protected override void OnDatabaseCreated(string provider, IssueContext context)
		{
			base.OnDatabaseCreated(provider, context);
			using var db = context.CreateLinqToDBContext();

			db.Insert(new Parent() { Id = 1, ParentId = 2 });
			db.Insert(new Parent() { Id = 2 });
			db.Insert(new Child() { Id = 11, ParentId = 1 });
			db.Insert(new Child() { Id = 12, ParentId = 2 });
			db.Insert(new GrandChild() { Id = 21, ChildId = 11 });
			db.Insert(new GrandChild() { Id = 22, ChildId = 12 });
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4603")]
		public async ValueTask Issue4603Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var linq2DbCtx = ctx.CreateLinqToDBContext();

			var cte = linq2DbCtx.GetCte<Parent>(
				hierarchy =>
				{
					return (ctx.Parents
					.Include(x=> x.Children)
					.ThenInclude(x => x.GrandChildren)
					.Select(@t => @t)).Concat(ctx.Parents.Join(hierarchy, o => o.ParentId, c => c.Id, (d, c) => d));
				});

			var cteResult = await cte.ToListAsyncLinqToDB();

			Assert.That(cteResult, Has.Count.EqualTo(2));

			var p = cteResult.FirstOrDefault(r => r.Id == 1);
			Assert.That(p, Is.Not.Null);
			Assert.That(p.Children, Has.Count.EqualTo(1));
			var c = p.Children.FirstOrDefault(r => r.Id == 11);
			Assert.That(c, Is.Not.Null);
			Assert.That(c.GrandChildren, Has.Count.EqualTo(1));
			var gc = p.Children.FirstOrDefault(r => r.Id == 21);
			Assert.That(gc, Is.Not.Null);

			p = cteResult.FirstOrDefault(r => r.Id == 2);
			Assert.That(p, Is.Not.Null);
			Assert.That(p.Children, Has.Count.EqualTo(1));
			c = p.Children.FirstOrDefault(r => r.Id == 12);
			Assert.That(c, Is.Not.Null);
			Assert.That(c.GrandChildren, Has.Count.EqualTo(1));
			gc = p.Children.FirstOrDefault(r => r.Id == 22);
			Assert.That(gc, Is.Not.Null);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4570")]
		public void Issue4570Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var result = (from ua in ctx.Parents
						  where ua.ParentId == 55377
						  && true && ( true || false)
						  && ctx.Parents.Any()
						  select 1).ToLinqToDB();
			result.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3174")]
		public async ValueTask Issue3174Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBContext();

			var queryable = db.GetTable<ShadowTable>().OrderBy(p => p.Id).Skip(1).Take(2);
			var linqUsers = await queryable.ToListAsyncLinqToDB();
			using var tempUsers = await db.CreateTempTableAsync(queryable);
			var result = tempUsers.ToList();
		}
	}
}
