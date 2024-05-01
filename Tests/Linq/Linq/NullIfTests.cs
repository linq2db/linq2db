using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class NullIfTests : TestBase
	{
		private TempTable<Src> SetupSrcTable(IDataContext db)
		{
			var data = new[]
			{
				new Src {Int = 2, NullableInt = 2,    String = "abc", NullableString = "abc"},
				new Src {Int = 3, NullableInt = null, String = "def", NullableString = null}
			};

			var src  = db.CreateLocalTable(data);
			return src;
		}

		[Test]
		public void Ints(
			[DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb = SetupSrcTable(db);

			var src = tb.OrderBy(_ => _.Int);

			var ints = src.Select(s => Sql.NullIf(s.Int, 2)).ToArray();
			ints[0].Should().Be(null);
			ints[1].Should().Be(3);

			ints = src.Select(s => Sql.NullIf(s.Int, 4)).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(3);

			ints = src.Select(s => Sql.NullIf(s.Int, default(int?))).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(3);

			ints = src.Select(s => Sql.NullIf(s.NullableInt, 2)).ToArray();
			ints[0].Should().Be(null);
			ints[1].Should().Be(null);

			ints = src.Select(s => Sql.NullIf(s.NullableInt, 4)).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(null);

			ints = src.Select(s => Sql.NullIf(s.NullableInt, default(int?))).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(null);
		}

		[Test]
		public void Strings(
			[DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb = SetupSrcTable(db);

			var src = tb.OrderBy(_ => _.Int);

			var strings = src.Select(s => Sql.NullIf(s.String, "abc")).ToArray();
			strings[0].Should().Be(null);
			strings[1].Should().Be("def");

			strings = src.Select(s => Sql.NullIf(s.String, "xyz")).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be("def");

			strings = src.Select(s => Sql.NullIf(s.String, null)).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be("def");

			strings = src.Select(s => Sql.NullIf(s.NullableString, "abc")).ToArray();
			strings[0].Should().Be(null);
			strings[1].Should().Be(null);

			strings = src.Select(s => Sql.NullIf(s.NullableString, "xyz")).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be(null);

			strings = src.Select(s => Sql.NullIf(s.NullableString, null)).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be(null);
		}		
		
		[Test]
		public void IntsSql(
			[DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb = SetupSrcTable(db);

			var src = tb.OrderBy(_ => _.Int);

			var ints = src.Select(s => Sql.AsSql(Sql.NullIf(s.Int, 2))).ToArray();
			ints[0].Should().Be(null);
			ints[1].Should().Be(3);

			ints = src.Select(s => Sql.AsSql(Sql.NullIf(s.Int, 4))).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(3);

			ints = src.Select(s => Sql.AsSql(Sql.NullIf(s.Int, default(int?)))).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(3);

			ints = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableInt, 2))).ToArray();
			ints[0].Should().Be(null);
			ints[1].Should().Be(null);

			ints = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableInt, 4))).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(null);

			ints = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableInt, default(int?)))).ToArray();
			ints[0].Should().Be(2);
			ints[1].Should().Be(null);
		}

		[Test]
		public void StringsSql(
			[DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb = SetupSrcTable(db);

			var src = tb.OrderBy(_ => _.Int);

			var strings = src.Select(s => Sql.AsSql(Sql.NullIf(s.String, "abc"))).ToArray();
			strings[0].Should().Be(null);
			strings[1].Should().Be("def");

			strings = src.Select(s => Sql.AsSql(Sql.NullIf(s.String, "xyz"))).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be("def");

			strings = src.Select(s => Sql.AsSql(Sql.NullIf(s.String, null))).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be("def");

			strings = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableString, "abc"))).ToArray();
			strings[0].Should().Be(null);
			strings[1].Should().Be(null);

			strings = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableString, "xyz"))).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be(null);

			strings = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableString, null))).ToArray();
			strings[0].Should().Be("abc");
			strings[1].Should().Be(null);
		}

		sealed class Src
		{
			public int Int { get; set; }
			public int? NullableInt { get; set; }
			public string String { get; set; } = null!;
			public string? NullableString { get; set; }
		}
	}
}
