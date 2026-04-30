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
	/// End-to-end tests for the entity-builder Update overload
	/// <c>Update&lt;T&gt;(this ITable&lt;T&gt;, T item, Expression&lt;Func&lt;IEntityUpdateBuilder&lt;T&gt;, IEntityUpdateBuilder&lt;T&gt;&gt;&gt; configure)</c>
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
	}
}
