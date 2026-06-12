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
	/// <c>Insert&lt;T&gt;(this ITable&lt;T&gt;, T item, Expression&lt;Func&lt;IEntityInsertSpec&lt;T&gt;, IEntityInsertSpec&lt;T&gt;&gt;&gt; configure)</c>
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

		[Table("EntityInsertNoDefaultCtorTest")]
		public sealed class EntityRowNoDefaultCtor
		{
			public EntityRowNoDefaultCtor(int id)
			{
				Id = id;
			}

			[PrimaryKey] public int    Id      { get; set; }
			[Column]     public string Name    { get; set; } = null!;
			[Column]     public int    Version { get; set; }
		}

		[Test]
		public void NoDefaultConstructor([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRowNoDefaultCtor>();

			void Insert(int id)
			{
				var item = new EntityRowNoDefaultCtor(id) { Name = $"n{id}", Version = id };

				db.GetTable<EntityRowNoDefaultCtor>().Insert(
					item,
					b => b.Set(x => x.Version, s => s.Version + 1));
			}

			Insert(1);
			Insert(2);

			var rows = db.GetTable<EntityRowNoDefaultCtor>().OrderBy(r => r.Id).ToArray();
			rows.Length.ShouldBe(2);
			rows[0].Id     .ShouldBe(1);
			rows[0].Name   .ShouldBe("n1");
			rows[0].Version.ShouldBe(2);
			rows[1].Id     .ShouldBe(2);
			rows[1].Name   .ShouldBe("n2");
			rows[1].Version.ShouldBe(3);
		}

		/// <summary>
		/// Verifies that two consecutive entity-builder Inserts with different <c>item</c> values
		/// on the same <see cref="DataContext"/> share the cached query plan — i.e. the second
		/// call does NOT trigger a cache miss. If the entity's columns were inlined as SQL
		/// constants instead of parameters, the second call would generate different SQL and miss.
		/// </summary>
		[Test]
		public void QueryCache_ParameterisesItemValues([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityRow>();

			// Prime the cache (first invocation = miss).
			db.GetTable<EntityRow>().Insert(
				new EntityRow { Id = 1, Name = "first", Version = 10, CreatedBy = "u1" },
				b => b.Set(x => x.Version, s => s.Version + 1));

			var missBefore = db.GetTable<EntityRow>().GetCacheMissCount();

			// Second insert with DIFFERENT values on the SAME DataContext → same cache slot.
			db.GetTable<EntityRow>().Insert(
				new EntityRow { Id = 2, Name = "second", Version = 20, CreatedBy = "u2" },
				b => b.Set(x => x.Version, s => s.Version + 1));

			db.GetTable<EntityRow>().GetCacheMissCount().ShouldBe(missBefore);

			var rows = db.GetTable<EntityRow>().OrderBy(r => r.Id).ToArray();
			rows.Length    .ShouldBe(2);
			rows[0].Id     .ShouldBe(1);
			rows[0].Name   .ShouldBe("first");
			rows[0].Version.ShouldBe(11);
			rows[0].CreatedBy.ShouldBe("u1");
			rows[1].Id     .ShouldBe(2);
			rows[1].Name   .ShouldBe("second");
			rows[1].Version.ShouldBe(21);
			rows[1].CreatedBy.ShouldBe("u2");
		}

		#region Nested complex-column setters

		sealed class EntityInsertNestedName
		{
			public string? First { get; set; }
			public string? Last  { get; set; }
		}

		[Table("EntityInsertNestedTest")]
		[Column("First", "Name.First")]
		[Column("Last",  "Name.Last")]
		sealed class EntityInsertNestedRow
		{
			[PrimaryKey] public int                    Id   { get; set; }
			             public EntityInsertNestedName Name { get; set; } = null!;
		}

		// Unsupported: EntitySetterBuilder builds the setter as Expression.Bind(cd.MemberInfo, …) on a
		// SqlGenericConstructorExpression, which can't bind a nested leaf member on the root type
		// ("Property 'First' is not defined for type 'EntityInsertNestedRow'"). Needs nested member-init
		// grouping or a switch to the envelope shape used by the native Upsert path. The single-item
		// Upsert path already handles nested columns (UpsertTests.Single_Set_NestedColumn).
		[ActiveIssue("Entity Insert/Update + bulk Upsert .Set does not support nested complex-column member paths (EntitySetterBuilder MemberInit binding)")]
		[Test]
		public void Insert_NestedColumn([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<EntityInsertNestedRow>();

			db.GetTable<EntityInsertNestedRow>().Insert(
				new EntityInsertNestedRow { Id = 1, Name = new EntityInsertNestedName { First = "ins-first", Last = "seed-last" } },
				b => b.Set(x => x.Name.First, () => "set-first"));

			var row = db.GetTable<EntityInsertNestedRow>().Single();
			row.Name.First.ShouldBe("set-first");
			row.Name.Last .ShouldBe("seed-last");
		}

		#endregion
	}
}
