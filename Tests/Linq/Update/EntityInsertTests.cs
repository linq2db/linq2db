using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// End-to-end tests for the entity-builder Insert overload
	/// <c>Insert&lt;T&gt;(this ITable&lt;T&gt;, T item, Expression&lt;Func&lt;IEntityInsertBuilder&lt;T&gt;, IEntityInsertBuilder&lt;T&gt;&gt;&gt; configure)</c>
	/// and its async sibling.
	/// </summary>
	[TestFixture]
	public class EntityInsertTests : TestBase
	{
		[Table("EntityInsertTest")]
		public sealed class EntityRow
		{
			[PrimaryKey]                     public int       Id        { get; set; }
			[Column]                         public string    Name      { get; set; } = null!;
			[Column]                         public int       Version   { get; set; }
			[Column]                         public DateTime? CreatedAt { get; set; }
			[Column]                         public string?   CreatedBy { get; set; }
		}

		[Test]
		public void Bare([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRow>();

			db.GetTable<EntityRow>().Insert(
				new EntityRow { Id = 1, Name = "n1", Version = 7, CreatedBy = "u1" },
				b => b);

			var row = db.GetTable<EntityRow>().Single();
			row.Id       .ShouldBe(1);
			row.Name     .ShouldBe("n1");
			row.Version  .ShouldBe(7);
			row.CreatedBy.ShouldBe("u1");
		}

		[Test]
		public void Set_ContextFree([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRow>();

			var stamp = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

			db.GetTable<EntityRow>().Insert(
				new EntityRow { Id = 1, Name = "n", Version = 1 },
				b => b.Set(x => x.CreatedAt, () => stamp));

			db.GetTable<EntityRow>().Single().CreatedAt.ShouldBe(stamp);
		}

		[Test]
		public void Set_FromSource([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRow>();

			// Pull CreatedBy from item.Name (use the item via the s parameter).
			db.GetTable<EntityRow>().Insert(
				new EntityRow { Id = 1, Name = "alice", Version = 1 },
				b => b.Set(x => x.CreatedBy, s => s.Name));

			db.GetTable<EntityRow>().Single().CreatedBy.ShouldBe("alice");
		}

		[Test]
		public void Ignore([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRow>();

			// Ignored columns are not written; CreatedBy stays at the column's default (NULL).
			db.GetTable<EntityRow>().Insert(
				new EntityRow { Id = 1, Name = "n", Version = 1, CreatedBy = "should-be-skipped" },
				b => b.Ignore(x => x.CreatedBy));

			db.GetTable<EntityRow>().Single().CreatedBy.ShouldBeNull();
		}

		[Test]
		public void Multiple_SetAndIgnore([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRow>();

			var stamp = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

			db.GetTable<EntityRow>().Insert(
				new EntityRow { Id = 1, Name = "n", Version = 1, CreatedBy = "raw" },
				b => b
					.Set   (x => x.CreatedAt, () => stamp)
					.Set   (x => x.Version,   s  => s.Version + 100)
					.Ignore(x => x.CreatedBy));

			var row = db.GetTable<EntityRow>().Single();
			row.CreatedAt.ShouldBe(stamp);
			row.Version  .ShouldBe(101);
			row.CreatedBy.ShouldBeNull();
		}

		[Test]
		public async Task Async_Insert([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRow>();

			await db.GetTable<EntityRow>().InsertAsync(
				new EntityRow { Id = 1, Name = "async", Version = 3 },
				b => b);

			db.GetTable<EntityRow>().Single().Name.ShouldBe("async");
		}
	}
}
