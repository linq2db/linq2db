using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// <see href="https://github.com/linq2db/linq2db/issues/5684"/>
	/// </summary>
	[TestFixture]
	public class Issue5684Tests : TestBase
	{
		[Table]
		sealed class User
		{
			[Column, PrimaryKey] public int    Id        { get; set; }
			[Column]             public string Name      { get; set; } = null!;
			[Column]             public bool   IsVisible { get; set; }

			public static readonly User[] Data =
			{
				new() { Id = 1, Name = "Alpha", IsVisible = true  },
				new() { Id = 2, Name = "Beta",  IsVisible = false },
				new() { Id = 3, Name = "Gamma", IsVisible = true  },
			};
		}

		[Test]
		public void NestedUnionAllWithOrderBy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(User.Data);

			var query1 = table.Where(x => x.IsVisible);
			var query2 = table.Where(x => !x.IsVisible);
			var query3 = table.Where(x => x.Name.Length > 10);
			var query4 = table.Where(x => x.Name.Length < 1000);

			var union1 = query1.UnionAll(query2);
			var union2 = query3.UnionAll(query4);

			var unionAll = union1.UnionAll(union2);

			var ordered = unionAll.OrderByDescending(x => x.Name);

			var result = ordered.ToList();

			// Expected multiset via LINQ to Objects (order-independent: OrderBy(Name) is not a stable key with duplicates).
			var expected = User.Data.Where(x => x.IsVisible)
				.Concat(User.Data.Where(x => !x.IsVisible))
				.Concat(User.Data.Where(x => x.Name.Length > 10))
				.Concat(User.Data.Where(x => x.Name.Length < 1000))
				.Select(x => (x.Id, x.Name, x.IsVisible))
				.OrderBy(x => x.Id)
				.ToList();

			result.Select(x => (x.Id, x.Name, x.IsVisible))
				.OrderBy(x => x.Id)
				.ShouldBe(expected);
		}
	}
}
