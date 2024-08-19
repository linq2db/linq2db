using System;
using System.Diagnostics;
using System.Linq;

using FluentAssertions;

using LinqToDB.Common.Logging;
using LinqToDB.EntityFrameworkCore.Tests.Models.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public sealed class IdTests : ContextTestBase<IdTestContext>
	{
		protected override IdTestContext CreateProviderContext(string provider, DbContextOptions<IdTestContext> options)
		{
			return new IdTestContext(options);
		}

		protected override DbContextOptionsBuilder<IdTestContext> ProviderSetup(string provider, string connectionString, DbContextOptionsBuilder<IdTestContext> optionsBuilder)
		{
			return base.ProviderSetup(provider, connectionString, optionsBuilder)
				.EnableSensitiveDataLogging()
				.ReplaceService<IValueConverterSelector, IdValueConverterSelector>();
		}

		IDataContext CreateLinqToDBContext(IdTestContext testContext)
		{
			var result = testContext.CreateLinqToDBContext();
			result.GetTraceSwitch().Level = TraceLevel.Verbose;
			return result;
		}

		[Test]
		public void TestInsertWithoutTracker([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			Assert.Ignore("Incomplete. Works separately, but broken when run with other tests");

			using var ctx = CreateContext(provider);
			using (var db = ctx.CreateLinqToDBContext()) Cleanup(db);

			var name = "test insert";

			ctx
				.Arrange(CreateLinqToDBContext)
				.Act(c => c.Insert(new Entity { Name = name }))
				.Assert(id => ctx.Entities.Single(e => e.Id == id).Name.Should().Be(name));
		}

		[Test]
		public void TestInsertWithoutNew([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var ctx = CreateContext(provider);
			using (var db = ctx.CreateLinqToDBContext()) Cleanup(db);

			var name = "test insert";

			ctx.Entities
				.Arrange(e => e.ToLinqToDBTable())
				.Act(e => e.InsertWithInt64Identity(() => new Entity { Name = name }))
				.Assert(id => ctx.Entities.Single(e => e.Id == id).Name.Should().Be(name));
		}

		[Test]
		public void TestInsertEfCore([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
#if !NET8_0_OR_GREATER
			Assert.Ignore("NotSupportedException. EF6- limitation?");
#endif

			using var ctx = CreateContext(provider);
			using (var db = ctx.CreateLinqToDBContext()) Cleanup(db);

			var name = "test insert ef";

			ctx
				.Arrange(c => c.Entities.Add(new Entity { Name = "test insert ef" }))
				.Act(_ => ctx.SaveChanges())
				.Assert(_ => ctx.Entities.Single().Name.Should().Be(name));
		}

		[Test]
		public void TestIncludeDetails([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider, [Values] bool l2db, [Values] bool tracking)
		{
			using var ctx = CreateContext(provider);

			ctx
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c => c
					.Entities
					.Where(e => e.Name == "Alpha")
					.Include(e => e.Details)
					.ThenInclude(d => d.Details)
					.Include(e => e.Children)
					.AsLinqToDB(l2db)
					.AsTracking(tracking)
					.ToArray())
				.Assert(e => e?.First().Details.First().Details.Count().Should().Be(2));
		}

		[Test]
		public void TestManyToManyIncludeTrackerPoison([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider, [Values] bool l2db)
		{
			using var ctx = CreateContext(provider);

			ctx
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c =>
				{
					var q = c.Entities
						.Include(e => e.Items)
						.ThenInclude(x => x.Item);
					var f = q.AsLinqToDB(l2db).AsTracking().ToArray();
					var s = q.AsLinqToDB(!l2db).AsTracking().ToArray();
					return (First: f, Second: s);
				})
				.Assert(r => r.First[0].Items.Count().Should().Be(r.Second[0].Items.Count()));
		}

		[Test]
		public void TestManyToManyInclude([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider, [Values] bool l2db, [Values] bool tracking)
		{
			if (!l2db && !tracking)
				Assert.Ignore("Incomplete");

			using var ctx = CreateContext(provider);

			ctx
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c => c.Entities
					.Include(e => e.Items)
					.ThenInclude(x => x.Item)
					.AsLinqToDB(l2db)
					.AsTracking(tracking)
					.ToArray())
				.Assert(m => m?[0].Items.First().Item.Should().BeSameAs(m[1].Items.First().Item));
		}

		[Test]
		public void TestMasterInclude([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider, [Values] bool l2db, [Values] bool tracking)
		{
			if (!l2db && !tracking)
				Assert.Ignore("Incomplete");

			using var ctx = CreateContext(provider);

			ctx
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c => c
					.Details
					.Include(d => d.Master)
					.AsLinqToDB(l2db)
					.AsTracking(tracking)
					.ToArray())
				.Assert(m => m?[0].Master.Should().BeSameAs(m[1].Master));
		}

		[Test]
		public void TestMasterInclude2([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider, [Values] bool l2db, [Values] bool tracking)
		{
			if (!tracking)
				Assert.Ignore("Incomplete");

			using var ctx = CreateContext(provider);

			ctx
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c => c
					.Details
					.Include(d => d.Master)
					.AsTracking(tracking)
					.AsLinqToDB(l2db)
					.ToArray())
				.Assert(m => m?[0].Master.Should().BeSameAs(m[1].Master));
		}

		void Cleanup(IDataContext ctx)
		{
			using var _ = new DisableBaseline("cleanup");
			ctx.GetTable<Child>().Delete();
			ctx.GetTable<SubDetail>().Delete();
			ctx.GetTable<Detail>().Delete();
			ctx.GetTable<Entity2Item>().Delete();
			ctx.GetTable<Entity>().Delete();
		}

		void InsertDefaults(IDataContext dataContext)
		{
			Cleanup(dataContext);

			using var _ = new DisableBaseline("setup db");

			var a = dataContext.InsertAsId(new Entity {Name = "Alpha"});
			var b = dataContext.InsertAsId(new Entity {Name = "Bravo"});
			var d = dataContext.InsertAsId(new Detail {Name = "First", MasterId = a});
			var r = dataContext.InsertAsId(new Item {Name = "Red"});
			var g = dataContext.InsertAsId(new Item {Name = "Green"});
			var w = dataContext.InsertAsId(new Item {Name = "White"});

			dataContext.Insert(new Detail {Name = "Second", MasterId = a});
			dataContext.Insert(new SubDetail {Name = "Plus", MasterId = d});
			dataContext.Insert(new SubDetail {Name = "Minus", MasterId = d});
			dataContext.Insert(new Child {Name = "One", ParentId = a});
			dataContext.Insert(new Child {Name = "Two", ParentId = a});
			dataContext.Insert(new Child {Name = "Three", ParentId = a});
			dataContext.Insert(new Entity2Item {EntityId = a, ItemId = r});
			dataContext.Insert(new Entity2Item {EntityId = a, ItemId = g});
			dataContext.Insert(new Entity2Item {EntityId = b, ItemId = r});
			dataContext.Insert(new Entity2Item {EntityId = b, ItemId = w});
		}
	}
}
