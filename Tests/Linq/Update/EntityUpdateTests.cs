using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// End-to-end tests for the entity-builder Update overload
	/// <c>Update&lt;T&gt;(this ITable&lt;T&gt;, T item, Expression&lt;Func&lt;IEntityUpdateSpec&lt;T&gt;, IEntityUpdateSpec&lt;T&gt;&gt;&gt; configure)</c>
	/// and its async sibling. Match is by primary key.
	/// </summary>
	[TestFixture]
	public class EntityUpdateTests : TestBase
	{
		[Table("EntityUpdateTest")]
		public sealed class EntityRow
		{
			[PrimaryKey]                     public int       Id        { get; set; }
			[Column]                         public string    Name      { get; set; } = null!;
			[Column]                         public int       Version   { get; set; }
			[Column]                         public DateTime? UpdatedAt { get; set; }
			[Column]                         public string?   UpdatedBy { get; set; }
		}

		[Table("EntityUpdateTestNoPk")]
		public sealed class EntityRowNoPk
		{
			[Column] public int    Id   { get; set; }
			[Column] public string Name { get; set; } = null!;
		}

		static EntityRow Seed(int id = 1, string name = "seed", int version = 1)
			=> new() { Id = id, Name = name, Version = version };

		[Test]
		public void Bare([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { Seed() });

			table.Update(
				new EntityRow { Id = 1, Name = "updated", Version = 2 },
				b => b);

			var row = table.Single();
			row.Name   .ShouldBe("updated");
			row.Version.ShouldBe(2);
		}

		[Test]
		public void Set_ContextFree([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { Seed() });

			var stamp = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

			table.Update(
				new EntityRow { Id = 1, Name = "x", Version = 5 },
				b => b.Set(x => x.UpdatedAt, () => stamp));

			table.Single().UpdatedAt.ShouldBe(stamp);
		}

		[Test]
		public void Set_FromSource([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { Seed() });

			table.Update(
				new EntityRow { Id = 1, Name = "alice", Version = 1 },
				b => b.Set(x => x.UpdatedBy, s => s.Name));

			table.Single().UpdatedBy.ShouldBe("alice");
		}

		[Test]
		public void Set_FromTargetAndSource([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { Seed(version: 10) });

			// Add the source-row's Version to the target's Version: 10 + 3 = 13.
			table.Update(
				new EntityRow { Id = 1, Name = "n", Version = 3 },
				b => b.Set(x => x.Version, (t, s) => t.Version + s.Version));

			table.Single().Version.ShouldBe(13);
		}

		[Test]
		public void Ignore([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { Seed(name: "old", version: 1) });

			// Ignore Name — only Version updates, Name keeps its existing value.
			table.Update(
				new EntityRow { Id = 1, Name = "should-not-write", Version = 99 },
				b => b.Ignore(x => x.Name));

			var row = table.Single();
			row.Name   .ShouldBe("old");
			row.Version.ShouldBe(99);
		}

		[Test]
		public async Task Async_Update([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { Seed() });

			await table.UpdateAsync(
				new EntityRow { Id = 1, Name = "async", Version = 3 },
				b => b);

			table.Single().Name.ShouldBe("async");
		}

		[Test]
		public void NoPrimaryKey_Throws([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new EntityRowNoPk { Id = 1, Name = "x" } });

			// Translator should refuse with a build-time error since the entity has no PK column.
			Action act = () => table.Update(new EntityRowNoPk { Id = 1, Name = "y" }, b => b);
			act.ShouldThrow<LinqToDBException>();
		}

		[Table("EntityUpdateNoDefaultCtorTest")]
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
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[]
			{
				new EntityRowNoDefaultCtor(1) { Name = "old1", Version = 1 },
				new EntityRowNoDefaultCtor(2) { Name = "old2", Version = 2 },
			});

			void Update(int id)
			{
				var item = new EntityRowNoDefaultCtor(id) { Name = $"new{id}", Version = id };

				table.Update(
					item,
					b => b.Set(x => x.Version, (t, s) => t.Version + s.Version));
			}

			Update(1);
			Update(2);

			var rows = table.OrderBy(r => r.Id).ToArray();
			rows.Length.ShouldBe(2);
			rows[0].Id     .ShouldBe(1);
			rows[0].Name   .ShouldBe("new1");
			rows[0].Version.ShouldBe(2);
			rows[1].Id     .ShouldBe(2);
			rows[1].Name   .ShouldBe("new2");
			rows[1].Version.ShouldBe(4);
		}

		/// <summary>
		/// Verifies that two consecutive entity-builder Updates with different <c>item</c> values
		/// on the same <see cref="DataContext"/> share the cached query plan — i.e. the second
		/// call does NOT trigger a cache miss. If the entity's columns (including the PK used in
		/// the WHERE predicate) were inlined as SQL constants instead of parameters, the second
		/// call would generate different SQL and miss.
		/// </summary>
		[Test]
		public void QueryCache_ParameterisesItemValues([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[]
			{
				new EntityRow { Id = 1, Name = "old1", Version = 1 },
				new EntityRow { Id = 2, Name = "old2", Version = 2 },
			});

			// Prime the cache (first invocation = miss).
			table.Update(
				new EntityRow { Id = 1, Name = "new1", Version = 5 },
				b => b.Set(x => x.Version, (t, s) => t.Version + s.Version));

			var missBefore = table.GetCacheMissCount();

			// Second update with DIFFERENT values on the SAME DataContext → same cache slot.
			table.Update(
				new EntityRow { Id = 2, Name = "new2", Version = 10 },
				b => b.Set(x => x.Version, (t, s) => t.Version + s.Version));

			table.GetCacheMissCount().ShouldBe(missBefore);

			var rows = table.OrderBy(r => r.Id).ToArray();
			rows.Length    .ShouldBe(2);
			rows[0].Id     .ShouldBe(1);
			rows[0].Name   .ShouldBe("new1");
			rows[0].Version.ShouldBe(1 + 5);
			rows[1].Id     .ShouldBe(2);
			rows[1].Name   .ShouldBe("new2");
			rows[1].Version.ShouldBe(2 + 10);
		}

		#region #3721 — dynamic / Sql.Property columns

		// Dynamic-column store mapped onto the physical EntityUpdateDynamicTest table; the "Name" column
		// is a dynamic (non-POCO) property reachable only through Sql.Property.
		[Table("EntityUpdateDynamicTest")]
		sealed class EntityUpdateDynamicStore
		{
			[PrimaryKey] public int Id { get; set; }

			[DynamicColumnsStore] public Dictionary<string, object> Values { get; set; } = new();
		}

		// Fixed-schema twin (same table name) used to materialise + seed the physical table.
		[Table("EntityUpdateDynamicTest")]
		sealed class EntityUpdateDynamicFullTable
		{
			[Column] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }
		}

		static MappingSchema ConfigureEntityUpdateDynamicSchema()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<EntityUpdateDynamicStore>()
					.HasPrimaryKey(e => e.Id)
					.DynamicColumnsStore(e => e.Values)
					.Property(x => Sql.Property<string?>(x, "Name"))
				.Build();

			return ms;
		}

		// #3721: update a dynamic / Sql.Property (non-POCO) column value through the entity-builder
		// Update .Set — match is by primary key. The .Set override now matches because BuildUpdateSetter
		// keys it on GetMemberAccessExpression (same Sql.Property form as the user selector); the value
		// (() => "dyn-update") is a client constant that parameterises normally.
		[Test]
		public void Update_Set_DynamicColumn([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db    = GetDataContext(context, ConfigureEntityUpdateDynamicSchema());
			using var table = db.CreateLocalTable(new[] { new EntityUpdateDynamicFullTable { Id = 1, Name = "seed" } });

			var store = db.GetTable<EntityUpdateDynamicStore>();

			store.Update(
				new EntityUpdateDynamicStore { Id = 1 },
				b => b.Set(x => Sql.Property<string?>(x, "Name"), () => "dyn-update"));

			store.Single(r => r.Id == 1).Values["Name"].ShouldBe("dyn-update");
		}

		// #3721: auto-derive a dynamic column value straight from the in-memory item's store dictionary
		// (no .Set override) — exercises store-based value extraction. Same non-parameterisable
		// store-getter-block root as the Insert auto-derive case. Gated.
		[ActiveIssue(3721)]
		[Test]
		public void Update_AutoDerive_DynamicColumn([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db    = GetDataContext(context, ConfigureEntityUpdateDynamicSchema());
			using var table = db.CreateLocalTable(new[] { new EntityUpdateDynamicFullTable { Id = 1, Name = "seed" } });

			var store = db.GetTable<EntityUpdateDynamicStore>();

			var item = new EntityUpdateDynamicStore { Id = 1 };
			item.Values["Name"] = "auto-update";

			store.Update(item, b => b);

			store.Single(r => r.Id == 1).Values["Name"].ShouldBe("auto-update");
		}

		#endregion

		#region Nested complex-column setters

		sealed class EntityUpdateNestedName
		{
			public string? First { get; set; }
			public string? Last  { get; set; }
		}

		[Table("EntityUpdateNestedTest")]
		[Column("First", "Name.First")]
		[Column("Last",  "Name.Last")]
		sealed class EntityUpdateNestedRow
		{
			[PrimaryKey] public int                    Id   { get; set; }
			             public EntityUpdateNestedName Name { get; set; } = null!;
		}

		// Nested complex-column .Set on entity Update (match by PK). Name.First comes from .Set;
		// Name.Last is auto-derived from the item. EntitySetterBuilder groups nested columns into a
		// MemberMemberBinding so the leaf binds on its own sub-object type, not (invalidly) on the root.
		[Test]
		public void Update_NestedColumn([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[]
			{
				new EntityUpdateNestedRow { Id = 1, Name = new EntityUpdateNestedName { First = "old-first", Last = "old-last" } }
			});

			table.Update(
				new EntityUpdateNestedRow { Id = 1, Name = new EntityUpdateNestedName { First = "ignored", Last = "upd-last" } },
				b => b.Set(x => x.Name.First, () => "upd-first"));

			var row = table.Single();
			row.Name.First.ShouldBe("upd-first");
			row.Name.Last .ShouldBe("upd-last");
		}

		#endregion
	}
}
