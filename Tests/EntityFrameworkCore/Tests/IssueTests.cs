using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using LinqToDB.DataProvider.SqlServer;
using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;
using LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.IssueModel;
using LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.IssueModel;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class IssueTests : ContextTestBase<IssueContextBase>
	{
		protected override IssueContextBase CreateProviderContext(string provider, DbContextOptions<IssueContextBase> options)
		{
			return provider switch
			{
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL) => new PostgreSQL.Models.IssueModel.IssueContext(options),
				_ when provider.IsAnyOf(TestProvName.AllMySql) => new Pomelo.Models.IssueModel.IssueContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSQLite) => new SQLite.Models.IssueModel.IssueContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSqlServer) => new SqlServer.Models.IssueModel.IssueContext(options),
				_ => throw new InvalidOperationException($"{nameof(CreateProviderContext)} is not implemented for provider {provider}")
			};
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
		public async ValueTask Issue4603Test([EFDataSources(TestProvName.AllMySql57)] string provider)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3491")]
		public void Issue3491Test([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBContext();

			db
				.GetTable<PostgreTable>()
				.Where(x => x.Id == 1)
				.Set(
					d => Sql.Row(d.Title, d.SearchVector),
					t => (from x in db.GetTable<PostgreTable>()
						  where t.Id == x.Id
						  select Sql.Row(t.Title, EF.Functions.ToTsVector("test")))
						  .Single())
				.Update();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4012")]
		public void Issue4012Test([EFDataSources(TestProvName.AllMySql57)] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBContext();

			var commentsHierarchyCteAns = db.GetCte<Parent>(commentHierarchy =>
			{
				return db.GetTable<Parent>()
				.Concat
				(
					from c in db.GetTable<Parent>()
					from c2 in commentHierarchy.InnerJoin(eh => c.Id == eh.ParentId)
					select c
				);
			});
			var commentsHierarchyCteDes = db.GetCte<Parent>(commentHierarchy =>
			{
				return db.GetTable<Parent>()
				.Concat
				(
					from c in db.GetTable<Parent>()
					from c2 in commentHierarchy.InnerJoin(eh => c.ParentId == eh.Id)
					select c
				);
			});

			var query = commentsHierarchyCteAns.Union(commentsHierarchyCteDes);

			var result = query.LoadWith(c => c.Children).ToList();

			Assert.That(result, Has.Count.EqualTo(2));

			var p = result.FirstOrDefault(r => r.Id == 1);
			Assert.That(p, Is.Not.Null);
			Assert.That(p.Children, Has.Count.EqualTo(1));
			var c = p.Children.FirstOrDefault(r => r.Id == 11);
			Assert.That(c, Is.Not.Null);
			Assert.That(c.GrandChildren, Has.Count.EqualTo(1));
			var gc = p.Children.FirstOrDefault(r => r.Id == 21);
			Assert.That(gc, Is.Not.Null);

			p = result.FirstOrDefault(r => r.Id == 2);
			Assert.That(p, Is.Not.Null);
			Assert.That(p.Children, Has.Count.EqualTo(1));
			c = p.Children.FirstOrDefault(r => r.Id == 12);
			Assert.That(c, Is.Not.Null);
			Assert.That(c.GrandChildren, Has.Count.EqualTo(1));
			gc = p.Children.FirstOrDefault(r => r.Id == 22);
			Assert.That(gc, Is.Not.Null);
		}

#if NET6_0
		[ActiveIssue(Configurations = [TestProvName.AllPostgreSQL, TestProvName.AllMySql])]
#else
		[ActiveIssue(TestProvName.AllPostgreSQL)]
#endif
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4333")]
		public void Issue4333Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBContext();

			var data = new[]
			{
				new IdentityTable() { Name = "Bar" },
				new IdentityTable() { Name = "Baz" },
			};

			using var table = db.CreateTempTable(data);

			var result = table.OrderBy(e => e.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(2));

			Assert.Multiple(() =>
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Name, Is.EqualTo("Bar"));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Name, Is.EqualTo("Baz"));
			});
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4624")]
		public void Issue4624Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Issue4624ItemTicketDates.ToLinqToDB()
				.Where(p => p.Id < 1000)
				.GroupBy(p => p.Item)
				.Select(p => new
				{
					AclItemId = p.Key.Id,
					AclItemName = p.Key.Name,
					TotalEntryCount = p.Key.Entries.Sum(ec => ec.EntriesCount),
					TotalEntryAllowed = p.Key.ItemTicketDates.Select(at => new
					{
						EntryCount = at.EntryCount
					}).ToList()
				});

			var result = query.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/66")]
		public void Issue66TestNoTracking([EFDataSources] string provider, [Values] bool efFirst)
		{
			using var ctx = CreateContext(provider);

			var result = efFirst
				? ctx.Details.Include(d => d.Master).AsNoTracking().ToLinqToDB().ToArray()
				: ctx.Details.Include(d => d.Master).ToLinqToDB().AsNoTracking().ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			if (efFirst)
			{
				Assert.That(result[0].Master, Is.Not.EqualTo(result[1].Master));
			}
			else
			{
				Assert.That(result[0].Master, Is.EqualTo(result[1].Master));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/66")]
		public void Issue66TestWithTracking([EFDataSources] string provider, [Values] bool efFirst)
		{
			using var ctx = CreateContext(provider);

			var result = efFirst
				? ctx.Details.Include(d => d.Master).AsTracking().ToLinqToDB().ToArray()
				: ctx.Details.Include(d => d.Master).ToLinqToDB().AsTracking().ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			Assert.That(result[0].Master, Is.EqualTo(result[1].Master));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4625")]
		public void Issue4625TestDefault([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var result = ctx.Types
				.ToLinqToDB()
				.Where(x => x.Id == 2)
				.Set(x => x.DateTimeOffset, TestData.DateTime)
				.Set(x => x.DateTimeOffsetN, TestData.DateTime)
				.Update();

			Assert.That(result, Is.EqualTo(1));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4625")]
		public void Issue4625TestWithConverter([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var result = ctx.Types
				.ToLinqToDB()
				.Where(x => x.Id == 2)
				.Set(x => x.DateTimeOffsetWithConverter, TestData.DateTime)
				.Set(x => x.DateTimeOffsetNWithConverter, TestData.DateTime)
				.Update();

			Assert.That(result, Is.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4626")]
		public void Issue4626Test1([EFDataSources(TestProvName.AllSQLite, TestProvName.AllMariaDB, TestProvName.AllMySql57, TestProvName.AllSqlServer2022Minus, TestProvName.AllPostgreSQL15Minus)] string provider)
		{
			using var ctx = CreateContext(provider);

			_ = (from c in ctx.Parents
				 select new
				 {
					 Key = c.Id,
					 Subquery = (
					 from p in c.Children
					 group p by p.ParentId into g
					 select new
					 {
						 Tag = g.Key,
						 Sum = g.Sum(p => p.Id),
						 Des = g.Issue4626AnyValue(p => p.Name)
					 }).ToArray()
				 })
					  .ToLinqToDB()
					  .ToArray();
		}

		[ActiveIssue(Details = "Marked as explicit due to stack overlow")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4626")]
		public void Issue4626Test2([EFDataSources(TestProvName.AllSQLite, TestProvName.AllMariaDB, TestProvName.AllMySql57)] string provider)
		{
			using var ctx = CreateContext(provider);

			_ = (from c in ctx.Parents
				 select new
				 {
					 Key = c.Id,
					 Subquery = (
					 from p in c.Children
					 group p by p.ParentId into g
					 select new
					 {
						 Tag = g.Key,
						 Sum = g.Sum(p => p.Id),
						 Des = g.StringAggregate(", ", p => p.Name).ToValue()
					 }).ToArray()
				 })
					  .ToLinqToDB()
					  .ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4627")]
		public void Issue4627Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			_ = ctx.Containers
				.Select(c => new
				{
					ChildItems = c.ChildItems.Select(ch => new
					{
						ContainerId = ch.Parent.ContainerId,
					}),
				})
				.ToLinqToDB()
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/129")]
		public void Issue129Test([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBContext();

			var keyColumn = db.MappingSchema.GetEntityDescriptor(typeof(Issue129Table)).Columns.Single(c => c.ColumnName == nameof(Issue129Table.Key));

			Assert.That(keyColumn.IsIdentity, Is.False);

			using var t = db.CreateTempTable<Issue129Table>();
		}
	}

	#region Test Extensions
	public static class TestExtensions
	{
		[Sql.Extension("ANY_VALUE({value})", ServerSideOnly = true, IsAggregate = true)]
		public static TItem Issue4626AnyValue<TSource, TItem>(this IEnumerable<TSource> src, [ExprParameter] Expression<Func<TSource, TItem>> value)
		{
			throw new InvalidOperationException();
		}
	}
	#endregion
}
