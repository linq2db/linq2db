using System;
using System.Diagnostics;
using System.Linq;

using FluentAssertions;

using LinqToDB.Common.Logging;
using LinqToDB.EntityFrameworkCore.Tests.Models.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL
{
	[TestFixture]
	public sealed class IdTests : TestBase, IDisposable
	{
		public IdTests()
		{
			_efContext = new TestContext(
				new DbContextOptionsBuilder()
					.ReplaceService<IValueConverterSelector, IdValueConverterSelector>()
					.UseLoggerFactory(LoggerFactory)
					.EnableSensitiveDataLogging()
					//.UseNpgsql("Server=DBHost;Port=5432;Database=IdTests;User Id=postgres;Password=TestPassword;Pooling=true;MinPoolSize=10;MaxPoolSize=100;")
					.UseNpgsql("Server=localhost;Port=5415;Database=IdTests;User Id=postgres;Password=Password12!;Pooling=true;MinPoolSize=10;MaxPoolSize=100;")
					.Options);
			_efContext.Database.EnsureDeleted();
			_efContext.Database.EnsureCreated();
		}

		IDataContext CreateLinqToDBContext(TestContext testContext)
		{
			var result = testContext.CreateLinqToDBContext();
			result.GetTraceSwitch().Level = TraceLevel.Verbose;
			return result;
		}

		readonly TestContext _efContext;

		[Test]
		[Ignore("Incomplete.")]
		public void TestInsertWithoutTracker([Values("test insert")] string name) 
			=> _efContext
				.Arrange(CreateLinqToDBContext)
				.Act(c => c.Insert(new Entity { Name = name }))
				.Assert(id => _efContext.Entities.Single(e => e.Id == id).Name.Should().Be(name));

		[Test]
		[Ignore("Incomplete.")]
		public void TestInsertWithoutNew([Values("test insert")] string name) 
			=> _efContext.Entities
				.Arrange(e => e.ToLinqToDBTable())
				.Act(e => e.InsertWithInt64Identity(() => new Entity {Name = name}))
				.Assert(id => _efContext.Entities.Single(e => e.Id == id).Name.Should().Be(name));

		[Test]
		[Ignore("Incomplete.")]
		public void TestInsertEfCore([Values("test insert ef")] string name) 
			=> _efContext
				.Arrange(c => c.Entities.Add(new Entity {Name = "test insert ef"}))
				.Act(_ => _efContext.SaveChanges())
				.Assert(_ => _efContext.Entities.Single().Name.Should().Be(name));

		[Test]
		[Ignore("Incomplete.")]
		public void TestIncludeDetails([Values] bool l2db, [Values] bool tracking)
			=> _efContext
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

		[Test]
		public void TestManyToManyIncludeTrackerPoison([Values] bool l2db)
			=> _efContext
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
		
		
		[Test]
		[Ignore("Incomplete.")]
		public void TestManyToManyInclude([Values] bool l2db, [Values] bool tracking)
			=> _efContext
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c => c.Entities
					.Include(e => e.Items)
					.ThenInclude(x => x.Item)
					.AsLinqToDB(l2db)
					.AsTracking(tracking)
					.ToArray())
				.Assert(m => m?[0].Items.First().Item.Should().BeSameAs(m[1].Items.First().Item));

		[Test]
		[Ignore("Incomplete.")]
		public void TestMasterInclude([Values] bool l2db, [Values] bool tracking)
			=> _efContext
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c => c
					.Details
					.Include(d => d.Master)
					.AsLinqToDB(l2db)
					.AsTracking(tracking)
					.ToArray())
				.Assert(m => m?[0].Master.Should().BeSameAs(m[1].Master));

		[Test]
		[Ignore("Incomplete.")]
		public void TestMasterInclude2([Values] bool l2db, [Values] bool tracking)
			=> _efContext
				.Arrange(c => InsertDefaults(CreateLinqToDBContext(c)))
				.Act(c => c
					.Details
					.Include(d => d.Master)
					.AsTracking(tracking)
					.AsLinqToDB(l2db)
					.ToArray())
				.Assert(m => m?[0].Master.Should().BeSameAs(m[1].Master));

		void InsertDefaults(IDataContext dataContext)
		{
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

		private sealed class TestContext : DbContext
		{
			public TestContext(DbContextOptions options) : base(options) { }
			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				base.OnModelCreating(modelBuilder);
				modelBuilder.Entity<Entity2Item>().HasKey(x => new { x.EntityId, x.ItemId});
				modelBuilder
					.UseSnakeCase()
					.UseIdAsKey()
					.UseOneIdSequence<long>("test", sn => $"nextval('{sn}')");
			}


			public DbSet<Entity> Entities { get; set; } = null!;
			public DbSet<Detail> Details { get; set; } = null!;
			public DbSet<SubDetail> SubDetails { get; set; } = null!;
			public DbSet<Item> Items { get; set; } = null!;
			public DbSet<Child> Children { get; set; } = null!;
		}

		public void Dispose() => _efContext.Dispose();
	}
}
