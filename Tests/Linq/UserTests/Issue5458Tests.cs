using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// <see href="https://github.com/linq2db/linq2db/issues/5458"/>
	/// </summary>
	[TestFixture]
	public class Issue5458Tests : TestBase
	{
		[Table]
		sealed class StringTable
		{
			[Column, PrimaryKey] public int     Id    { get; set; }
			[Column]             public string? Value { get; set; }
		}

		[Table]
		sealed class OtherTable
		{
			[Column, PrimaryKey] public int Id { get; set; }
		}

		sealed class ResultDTO
		{
			public StringTable? Entity            { get; set; }
			public string?      TranslatedMessage { get; set; }
		}

		[Test]
		public void UnionAllWithJoinAndIsNullOrEmpty([DataSources] string context)
		{
			using var db     = GetDataContext(context);
			using var table  = db.CreateLocalTable(new[]
			{
				new StringTable { Id = 1, Value = "hello" },
				new StringTable { Id = 2, Value = null    },
			});
			using var table2 = db.CreateLocalTable(new[] { new OtherTable { Id = 1 }, new OtherTable { Id = 2 } });

			var query =
				from t in table.Where(t => t.Id <= 1).UnionAll(table.Where(t => t.Id > 1))
				join o in db.GetTable<OtherTable>() on t.Id equals o.Id
				select new ResultDTO
				{
					Entity            = t,
					TranslatedMessage = !string.IsNullOrEmpty(t.Value) ? t.Value : "default",
				};

			var result = query.OrderBy(r => r.Entity!.Id).ToList();

			result.Count.ShouldBe(2);
			result[0].TranslatedMessage.ShouldBe("hello");
			result[1].TranslatedMessage.ShouldBe("default");
		}

		[Test]
		public void UnionAllStringIsNullOrEmpty([DataSources] string context)
		{
			var data = new[]
			{
				new StringTable { Id = 1, Value = "hello" },
				new StringTable { Id = 2, Value = ""      },
				new StringTable { Id = 3, Value = null    },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				table.Where(t => t.Id <= 2)
				.UnionAll(table.Where(t => t.Id > 2))
				.Select(t => new
				{
					t.Id,
					IsEmpty = string.IsNullOrEmpty(t.Value),
				})
				.OrderBy(t => t.Id);

			var result = query.ToList();

			result.Count.ShouldBe(3);
			result[0].IsEmpty.ShouldBeFalse();
			result[1].IsEmpty.ShouldBeTrue();
			result[2].IsEmpty.ShouldBeTrue();
		}
	}
}
