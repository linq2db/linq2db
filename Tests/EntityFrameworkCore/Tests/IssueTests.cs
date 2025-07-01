using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Shouldly;

using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;
using LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.IssueModel;
using LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.IssueModel;
using LinqToDB.Mapping;

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

		[ActiveIssue]
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

		[ActiveIssue]
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

		[ActiveIssue]
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
			Assert.That(p.Children, Is.Not.Null);
			Assert.That(p.Children, Has.Count.EqualTo(1));
			var c = p.Children.FirstOrDefault(r => r.Id == 11);
			Assert.That(c, Is.Not.Null);
			Assert.That(c.GrandChildren, Has.Count.EqualTo(1));
			var gc = p.Children.FirstOrDefault(r => r.Id == 21);
			Assert.That(gc, Is.Not.Null);

			p = result.FirstOrDefault(r => r.Id == 2);
			Assert.That(p, Is.Not.Null);
			Assert.That(p.Children, Is.Not.Null);
			Assert.That(p.Children, Has.Count.EqualTo(1));
			c = p.Children.FirstOrDefault(r => r.Id == 12);
			Assert.That(c, Is.Not.Null);
			Assert.That(c.GrandChildren, Has.Count.EqualTo(1));
			gc = p.Children.FirstOrDefault(r => r.Id == 22);
			Assert.That(gc, Is.Not.Null);
		}

		[ActiveIssue(TestProvName.AllPostgreSQL)]
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Name, Is.EqualTo("Bar"));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Name, Is.EqualTo("Baz"));
			}
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4626")]
		public void Issue4626Test2([EFDataSources(TestProvName.AllSQLite, TestProvName.AllMariaDB, TestProvName.AllMySql57, TestProvName.AllSqlServer2016Minus)] string provider)
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

		[ActiveIssue]
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

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/155")]
		public void Issue155Test([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			var fm = new FluentMappingBuilder();
			fm.Entity<Issue155Table>()
				.Property(e => e.LinkedFrom)
				.HasAttribute(new ExpressionMethodAttribute((IDataContext db, Issue155Table e) => db.GetTable<Issue155Table>().Where(r => Sql.Ext.PostgreSQL().ValueIsEqualToAny(e.Id, r.Linked)).ArrayAggregate(r => r.Id, Sql.AggregateModifier.Distinct).ToValue()));
			db.AddMappingSchema(fm.Build().MappingSchema);

			var result = db.GetTable<Issue155Table>().Where(e => e.Id == 1).Single();
			Assert.That(result.Linked, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Linked[0], Is.EqualTo(2));
				Assert.That(result.LinkedFrom, Has.Length.EqualTo(2));
				Assert.That(result.LinkedFrom, Does.Contain(2));
				Assert.That(result.LinkedFrom, Does.Contain(3));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4628")]
		public async ValueTask Issue4628Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var res = await ctx.Issue4628Others
				.Include(o => o.Values)
				.ToArrayAsyncLinqToDB();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Id, Is.EqualTo(1));
				Assert.That(res[0].Values, Has.Count.EqualTo(1));
			}

			var value = res[0].Values.Single();
			Assert.That(value, Is.TypeOf<Issue4628Inherited>());
			var typedValue = (Issue4628Inherited)value;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(typedValue.Id, Is.EqualTo(11));
				Assert.That(typedValue.OtherId, Is.EqualTo(1));
				Assert.That(typedValue.SomeValue, Is.EqualTo("Value 11"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4629")]
		public void Issue4629Test([EFDataSources(TestProvName.AllMySql57)] string provider)
		{
			using var ctx = CreateContext(provider);

			var posts = ctx.Issue4629Posts.AsQueryable()
				.OrderBy(p => p.Tags.Sum(t => t.Weight))
				.Where(p => p.Tags.Where(t => t.Weight > 1) // this line remove ORDER BY
					.Sum(t => t.Weight) > 5)
				.Take(10)
				.Select(id => new
				{
					Count = Sql.Ext.Count().Over().ToValue(),
					Id = id,
				})
				.ToLinqToDB().ToArray().OrderBy(r => r.Id.Id).ToArray();

			Assert.That(posts, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(posts[0].Id.Id, Is.EqualTo(1));
				Assert.That(posts[0].Count, Is.EqualTo(2));
				Assert.That(posts[1].Id.Id, Is.EqualTo(2));
				Assert.That(posts[1].Count, Is.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/201")]
		public void Issue201Test1([EFDataSources(TestProvName.AllMySql57)] string provider)
		{
			using var ctx = CreateContext(provider);

			var res = ctx.Parents
				.Where(x => x.Children.Select(y => y.IsActive).FirstOrDefault() == false)
				.ToLinqToDB()
				.Count();

			Assert.That(res, Is.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/201")]
		public void Issue201Test2([EFDataSources(TestProvName.AllMySql57)] string provider)
		{
			using var ctx = CreateContext(provider);

			var res = ctx.Parents
				.Where(x => !x.Children.Any(y => y.IsActive))
				.ToLinqToDB()
				.Count();

			Assert.That(res, Is.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4630")]
		public void Issue4630Test([EFDataSources(TestProvName.AllMySql57)] string provider)
		{
			using var ctx = CreateContext(provider);

			var id = 2;

			var res = ctx.Parents
					.Select(x => new { Index = Sql.Ext.RowNumber().Over().OrderBy(x.Id).ToValue(), Id = x.Id })
					.ToLinqToDB()
					.Where(pb => pb.Id == id)
					.Select(pb => pb.Index)
					.FirstOrDefault();

			Assert.That(res, Is.EqualTo(2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4638")]
		public void Issue4638Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Issue4624Items
				.GroupBy(p => p.AclNameId)
				.Select(p => new
				{
					Id = p.Key,
					Items = p.Select(tx => new
					{
						tx.CfAllowValue,
						tx.DateFrom
					}).OrderBy(tx => tx.DateFrom).ToList()
				});

			var result = query.ToLinqToDB().FirstOrDefault();
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/340")]
		public void Issue340Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Issue340Entities.Where(x => x.IsActive == true)
				.Select(x => new
				{
					Id = x.Id
				});

			_ = query.ToLinqToDB().ToList();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4640")]
		public void Issue4640Test([EFDataSources(TestProvName.AllMySql, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus)] string provider)
		{
			using var ctx = CreateContext(provider);

			var items = new Issue4640Table[]
			{
				new Issue4640Table()
				{
					Id = 1,
					Items =
					[
						new Issue4640Items() { Name = "record 1", Offset = -1 },
						new Issue4640Items() { Name = "record 2", Offset = 20 },
					]
				}
			};

			ctx.Issue4640.ToLinqToDB()
				.Merge()
				.Using(items)
				.On((t, s) => s.Id == t.Id)
				.InsertWhenNotMatched(s => new Issue4640Table()
				{
					Id = s.Id,
					Items = s.Items
				})
				.UpdateWhenMatched((t, s) => new Issue4640Table()
				{
					Items = s.Items,
				}).Merge();

			var record = ctx.Issue4640.ToLinqToDB().Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Id, Is.EqualTo(1));
				Assert.That(record.Items, Is.Not.Null);
			}

			Assert.That(record.Items, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Items[0].Name, Is.EqualTo("record 1"));
				Assert.That(record.Items[0].Offset, Is.EqualTo(-1));
				Assert.That(record.Items[1].Name, Is.EqualTo("record 2"));
				Assert.That(record.Items[1].Offset, Is.EqualTo(20));
			}

			items[0].Items![1] = new Issue4640Items() { Name = "record 3", Offset = 4 };

			ctx.Issue4640.ToLinqToDB()
				.Merge()
				.Using(items)
				.On((t, s) => s.Id == t.Id)
				.InsertWhenNotMatched(s => new Issue4640Table()
				{
					Id = s.Id,
					Items = s.Items
				})
				.UpdateWhenMatched((t, s) => new Issue4640Table()
				{
					Items = s.Items,
				}).Merge();

			record = ctx.Issue4640.ToLinqToDB().Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Id, Is.EqualTo(1));
				Assert.That(record.Items, Is.Not.Null);
			}

			Assert.That(record.Items, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Items[0].Name, Is.EqualTo("record 1"));
				Assert.That(record.Items[0].Offset, Is.EqualTo(-1));
				Assert.That(record.Items[1].Name, Is.EqualTo("record 3"));
				Assert.That(record.Items[1].Offset, Is.EqualTo(4));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4641")]
		public void Issue4641Test([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var ctx = CreateContext(provider);

			var items = new Issue4641Table[] { new Issue4641Table(),new Issue4641Table() };

			ctx.BulkCopy(items);

			var result = ((LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.IssueModel.IssueContext)ctx).Issue4641Table.OrderBy(r => r.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[1].Id, Is.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4642")]
		public async Task Issue4642Test([EFDataSources(TestProvName.AllMySql, TestProvName.AllSQLite, TestProvName.AllPostgreSQL16Minus)] string provider)
		{
			using var ctx = CreateContext(provider);

			var id = 1;
			var systemId = "system";
			var ids = new List<int>() { id };

			var resultEnum = ctx.Issue4642Table1
				.Where(x => ids.Contains(x.Id))
				.Join(ctx.Issue4642Table2.Where(x => x.SystemId == systemId), x => x.Id, x => x.Id, (x, y) => y)
				.ToLinqToDB()
				.MergeInto(ctx.Issue4642Table2)
				.OnTargetKey()
				.UpdateWhenMatched()
				.InsertWhenNotMatched()
				.MergeWithOutputAsync((s, x, y) => new { action = s, y.Id });

			await foreach (var item in resultEnum)
			{
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4643")]
		public void Issue4643Test([EFIncludeDataSources(TestProvName.AllPostgreSQL14Minus)] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			var item = new Issue4643Table()
			{
				Value = [DayOfWeek.Friday, DayOfWeek.Saturday]
			};
			db.Insert(item);

			var result = ctx.Set<Issue4643Table>().ToLinqToDB().Single();

			Assert.That(result.Value, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Value[0], Is.EqualTo(DayOfWeek.Friday));
				Assert.That(result.Value[1], Is.EqualTo(DayOfWeek.Saturday));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4644")]
		public void Issue4644Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			ctx.Issue4644Priced.ToLinqToDB().ToList();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4649")]
		public void Issue4649Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			var record = new Issue4649Table()
			{
				Id = -1,
				Name = "initial name"
			};

			var id = db.InsertWithInt32Identity(record);

			var inserted = ctx.Issue4649.Where(p => p.Id == id).Single();

			Assert.That(inserted.Name, Is.EqualTo("initial name"));

			var cnt = ctx.Issue4649.Where(d => d.Id == id).ToLinqToDB().Set(d => d.Name, "new name").Update();

			Assert.That(cnt, Is.EqualTo(1));

			var readByLinqToDB = ctx.Issue4649.Where(d => d.Id == id).ToLinqToDB().ToArray();

			Assert.That(readByLinqToDB, Has.Length.EqualTo(1));

			Assert.That(readByLinqToDB[0].Name, Is.EqualTo("new name"));

			var updated = ctx.Issue4649.Where(p => p.Id == id).Single();

			Assert.That(updated.Name, Is.EqualTo("new name"));
		}

		[ActiveIssue(Configurations = [TestProvName.AllSqlServer, TestProvName.AllMySql])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4653")]
		public void Issue4653Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Masters.ToLinqToDB();

			using var tr = ctx.Database.BeginTransaction();

			query.ToArray();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4662")]
		public void Issue4662Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			ctx.Issue4662.Add(new Issue4662Table()
			{
				Value = DayOfWeek.Wednesday
			});
			ctx.SaveChanges();
			ctx.Issue4662.ToLinqToDBTable().ToArray();
		}

#if NET8_0_OR_GREATER
		[ActiveIssue(Configurations = [TestProvName.AllMySql, TestProvName.AllSQLite, TestProvName.AllPostgreSQL])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4663")]
		public void Issue4663Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			ctx.BulkCopy(
				[new Issue4663Entity()
				{
					Id = 1,
					Value = new Issue4663Entity.MyComplexType("SomeValue")
				}]
			);
		}
#endif

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4666")]
		public void Issue4666Test([EFDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql)] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			using var tempTable = db.CreateTempTable(
			[
				new Issue4666Type1Entity() { Id = 1, Description = "Test1", Type1EntityProp = "Prop1" },
				new Issue4666Type1Entity() { Id = 2, Description = "Test2", Type1EntityProp = "Prop2" }
			], tableName: "Issue4666Temp");

			var destinationTable = db.GetTable<Issue4666Type1Entity>();

			destinationTable
				.Merge()
				.Using(tempTable)
				.On((target, source) => target.Id == source.Id)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.DeleteWhenNotMatchedBySourceAnd(i => i.Type == Issue4666EntityType.Type1)
				.Merge();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4667")]
		public void Issue4667Test([EFIncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string provider)
		{
			using var ctx = CreateContext(provider);

			var entities = new Issue4667Table[]
			{
				new Issue4667Table() { Id = 1, Payload = /*lang=json,strict*/ "{\"test\" : 1}", Headers = { { "property", "value" } } }
			};

			ctx.Set<Issue4667Table>()
				.ToLinqToDBTable()
				.Merge()
				.Using(entities)
				.OnTargetKey()
				.InsertWhenNotMatched()
				.Merge();
		}

#if NET8_0_OR_GREATER
		[ActiveIssue]
#endif
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4668")]
		public void Issue4668Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			ctx.Issue4668.ToLinqToDB().ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4669")]
		public async Task Issue4669Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Children
				.Where(x => x.Name!.Contains("Test"))
				.OrderBy(x => x.Name);

			await query.ToListAsyncLinqToDB();

			await ctx.Children
				.Where(p => p.Name!.StartsWith("Test"))
				.ToListAsyncEF();
		}

		[ActiveIssue(Configuration = TestProvName.AllPostgreSQL)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4671")]
		public void Issue4671Test1([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(Issue4671Entity1));
			var column = ed.Columns.Single(c => c.ColumnName == nameof(Issue4671Entity1.Id));

			Assert.That(column.IsIdentity);

			using var t1 = db.CreateLocalTable<Issue4671Entity1>();
			using var t2 = db.CreateTempTable<Issue4671Entity1>($"{nameof(Issue4671Entity1)}TMP");

			t1.Insert(() => new Issue4671Entity1() { Value = 1 });
			t2.Insert(() => new Issue4671Entity1() { Value = 2 });

			var res1 = t1.Single();
			var res2 = t2.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res1.Id, Is.EqualTo(1));
				Assert.That(res2.Id, Is.EqualTo(1));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4671")]
		public void Issue4671Test2([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(Issue4671Entity2));
			var column = ed.Columns.Single(c => c.ColumnName == nameof(Issue4671Entity2.Id));

			Assert.That(column.IsIdentity);

			using var t1 = db.CreateLocalTable<Issue4671Entity2>();
			using var t2 = db.CreateTempTable<Issue4671Entity2>($"{nameof(Issue4671Entity2)}TMP");

			t1.Insert(() => new Issue4671Entity2() { Value = 1 });
			t2.Insert(() => new Issue4671Entity2() { Value = 2 });

			var res1 = t1.Single();
			var res2 = t2.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res1.Id, Is.EqualTo(1));
				Assert.That(res2.Id, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task IssueEnumTest([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			ctx.IssueEnum.AddRange(new IssueEnumTable[]
				{
					new IssueEnumTable
					{
						Value = StatusEnum.Completed
					},
					new IssueEnumTable
					{
						Value = StatusEnum.Pending
					},
					new IssueEnumTable
					{
						Value = StatusEnum.Rejected
					},
					new IssueEnumTable
					{
						Value = StatusEnum.Reviewed
					},
					new IssueEnumTable
					{
						Value = StatusEnum.Verified
					},
				});
			ctx.SaveChanges();
			await ctx.IssueEnum.ToListAsyncLinqToDB();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4816")]
		public void Issue4816Test([EFIncludeDataSources(TestProvName.AllSqlServer2017Plus)] string provider)
		{
			using var ctx = CreateContext(provider);

			ctx.GetTable<Issue4816Table>()
				.GroupBy(r => r.Id)
				.Select(g => new
				{
					g.Key,
					VarChar = Sql.StringAggregate(g.Select(g => g.ValueVarChar), ",").ToValue(),
					NVarChar = Sql.StringAggregate(g.Select(g => g.ValueNVarChar), ",").ToValue(),
				})
				.ToArray();
		}

		#region Issue 4783

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4783")]
		public async ValueTask Issue4783Test([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var entities = new List<Issue4783Record>
			{
				new(0, "EF", Issue4783Status.Open, Issue4783Status.Open, Issue4783Status.Open, Issue4783Status.Open, Issue4783Status.Open, Issue4783Status.Open),
				new(0, "EF", Issue4783Status.Closed, Issue4783Status.Closed, Issue4783Status.Closed, Issue4783Status.Closed, Issue4783Status.Closed, Issue4783Status.Closed),
				new(0, "EF", Issue4783Status.Closed, Issue4783Status.Closed, Issue4783Status.Closed, null, null, null)
			};

			ctx.Issue4783Records.AddRange(entities);
			await ctx.SaveChangesAsync();

			await ctx.BulkCopyAsync(entities.Select(x => x with { Source = "linq2db" }));

			using var db = ctx.CreateLinqToDBConnection();
			var results = await db.GetTable<Issue4783RecordRaw>().OrderBy(r => r.Id).ToArrayAsync();

			using (Assert.EnterMultipleScope())
			{
				for (var i = 0; i < results.Length; i++)
				{
					Assert.That(results[i].StatusRaw,               Is.EqualTo((int)entities[i % entities.Count].StatusRaw),                      $"{results[i].Source}:({results[i].Id})");
					Assert.That(results[i].StatusString,            Is.EqualTo(entities[i % entities.Count].StatusString.ToString()),             $"{results[i].Source}:({results[i].Id})");
					Assert.That(results[i].StatusConverter,         Is.EqualTo(entities[i % entities.Count].StatusConverter.ToString()),          $"{results[i].Source}:({results[i].Id})");
					Assert.That(results[i].NullableStatusRaw,       Is.EqualTo((int?)entities[i % entities.Count].NullableStatusRaw),             $"{results[i].Source}:({results[i].Id})");
					Assert.That(results[i].NullableStatusString,    Is.EqualTo(entities[i % entities.Count].NullableStatusString?.ToString()),    $"{results[i].Source}:({results[i].Id})");
					Assert.That(results[i].NullableStatusConverter, Is.EqualTo(entities[i % entities.Count].NullableStatusConverter?.ToString()), $"{results[i].Source}:({results[i].Id})");
				}
			}
		}

		#endregion
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
