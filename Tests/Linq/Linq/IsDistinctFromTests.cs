using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;
using System;
using System.Linq;
using FluentAssertions;

namespace Tests.Linq
{
	[TestFixture]
	public class IsDistinctFromTests : TestBase
	{
		private TempTable<Src> SetupSrcTable(IDataContext db)
		{
			var src = db.CreateLocalTable<Src>();
			db.Insert(new Src { Int = 2, NullableInt = 2, String = "abc", NullableString = "abc" });
			db.Insert(new Src { Int = 3, NullableInt = null, String = "def", NullableString = null });
			return src;
		}

		[Test]
		public void Ints(
			[DataSources]        string context, 
			[Values(2, 4, null)] int?   value)
		{
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count = src.Count(s => s.Int.IsDistinctFrom(value));
			count.Should().Be(value == 2 ? 1 : 2);

			count = src.Count(s => s.NullableInt.IsDistinctFrom(value));
			count.Should().Be(value != 4 ? 1 : 2);

			count = src.Count(s => s.Int.IsNotDistinctFrom(value));
			count.Should().Be(value == 2 ? 1 : 0);

			count = src.Count(s => s.NullableInt.IsNotDistinctFrom(value));
			count.Should().Be(value != 4 ? 1 : 0);
		}

		[Test]
		public void Strings(
			[DataSources]                string  context, 
			[Values("abc", "xyz", null)] string? value)
		{
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count = src.Count(s => s.String.IsDistinctFrom(value));
			count.Should().Be(value == "abc" ? 1 : 2);

			count = src.Count(s => s.NullableString.IsDistinctFrom(value));
			count.Should().Be(value != "xyz" ? 1 : 2);

			count = src.Count(s => s.String.IsNotDistinctFrom(value));
			count.Should().Be(value == "abc" ? 1 : 0);

			count = src.Count(s => s.NullableString.IsNotDistinctFrom(value));
			count.Should().Be(value != "xyz" ? 1 : 0);
		}

		[Test]
		public void OptimizeConstants(
			[DataSources]                string  context, 
			[Values(5, 6, null)]         int?    value)
		{
			using var db = GetDataContext(context);
			
			var src = db.SelectQuery(() => new { ID = 1 });

			int count = src.Count(s => 5.IsDistinctFrom(value));
			count.Should().Be(value == 5 ? 0 : 1);
			if (db is DataConnection c1)
				c1.LastQuery.Should().NotContainAny("5", "6");

			count = src.Count(s => 5.IsNotDistinctFrom(value));
			count.Should().Be(value == 5 ? 1 : 0);
			if (db is DataConnection c2)
				c2.LastQuery.Should().NotContainAny("5", "6");
		}

		class Src 
		{
			public int Int { get; set; }
			public int? NullableInt { get; set; }
			public string String { get; set; } = null!;
			public string? NullableString { get; set; }
		}
	}
}
