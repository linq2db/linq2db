using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class NullIfTests : TestBase
	{
		private TempTable<Src> SetupSrcTable(IDataContext db)
		{
			var data = new[]
			{
				new Src {Id = 1, Int = 2, NullableInt = 2,    String = "abc", NullableString = "abc"},
				new Src {Id = 2, Int = 3, NullableInt = null, String = "def", NullableString = null}
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

			var q1 = src.Select(s => Sql.NullIf(s.Int, 2));
			var ints = q1.ToArray();
			q1.Count(x => x == null).ShouldBe(1);
			q1.Count(x => x != null).ShouldBe(1);
			ints[0].ShouldBe(null);
			ints[1].ShouldBe(3);

			var q2 = src.Select(s => Sql.NullIf(s.Int, 4));
			ints = q2.ToArray();
			q2.Count(x => x == null).ShouldBe(0);
			q2.Count(x => x != null).ShouldBe(2);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(3);

			var q3 = src.Select(s => Sql.NullIf(s.Int, default(int?)));
			ints = q3.ToArray();
			q3.Count(x => x == null).ShouldBe(0);
			q3.Count(x => x != null).ShouldBe(2);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(3);

			var q4 = src.Select(s => Sql.NullIf(s.NullableInt, 2));
			ints = q4.ToArray();
			q4.Count(x => x == null).ShouldBe(2);
			q4.Count(x => x != null).ShouldBe(0);
			ints[0].ShouldBe(null);
			ints[1].ShouldBe(null);

			var q5 = src.Select(s => Sql.NullIf(s.NullableInt, 4));
			ints = q5.ToArray();
			q5.Count(x => x == null).ShouldBe(1);
			q5.Count(x => x != null).ShouldBe(1);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(null);

			var q6 = src.Select(s => Sql.NullIf(s.NullableInt, default(int?)));
			ints = q6.ToArray();
			q6.Count(x => x == null).ShouldBe(1);
			q6.Count(x => x != null).ShouldBe(1);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(null);
		}

		[Test]
		public void Strings(
			[DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb = SetupSrcTable(db);

			var src = tb.OrderBy(_ => _.Int);

			var q1 = src.Select(s => Sql.NullIf(s.String, "abc"));
			var strings = q1.ToArray();
			q1.Count(x => x == null).ShouldBe(1);
			q1.Count(x => x != null).ShouldBe(1);
			strings[0].ShouldBe(null);
			strings[1].ShouldBe("def");

			var q2 = src.Select(s => Sql.NullIf(s.String, "xyz"));
			strings = q2.ToArray();
			q2.Count(x => x == null).ShouldBe(0);
			q2.Count(x => x != null).ShouldBe(2);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe("def");

			var q3 = src.Select(s => Sql.NullIf(s.String, null));
			strings = q3.ToArray();
			q3.Count(x => x == null).ShouldBe(0);
			q3.Count(x => x != null).ShouldBe(2);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe("def");

			var q4 = src.Select(s => Sql.NullIf(s.NullableString, "abc"));
			strings = q4.ToArray();
			q4.Count(x => x == null).ShouldBe(2);
			q4.Count(x => x != null).ShouldBe(0);
			strings[0].ShouldBe(null);
			strings[1].ShouldBe(null);

			var q5 = src.Select(s => Sql.NullIf(s.NullableString, "xyz"));
			strings = q5.ToArray();
			q5.Count(x => x == null).ShouldBe(1);
			q5.Count(x => x != null).ShouldBe(1);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe(null);

			var q6 = src.Select(s => Sql.NullIf(s.NullableString, null));
			strings = q6.ToArray();
			q6.Count(x => x == null).ShouldBe(1);
			q6.Count(x => x != null).ShouldBe(1);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe(null);
		}		
		
		[Test]
		public void IntsSql(
			[DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb = SetupSrcTable(db);

			var src = tb.OrderBy(_ => _.Int);

			var q1 = src.Select(s => Sql.AsSql(Sql.NullIf(s.Int, 2)));
			var ints = q1.ToArray();
			q1.Count(x => x == null).ShouldBe(1);
			q1.Count(x => x != null).ShouldBe(1);
			ints[0].ShouldBe(null);
			ints[1].ShouldBe(3);

			var q2 = src.Select(s => Sql.AsSql(Sql.NullIf(s.Int, 4)));
			ints = q2.ToArray();
			q2.Count(x => x == null).ShouldBe(0);
			q2.Count(x => x != null).ShouldBe(2);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(3);

			var q3 = src.Select(s => Sql.AsSql(Sql.NullIf(s.Int, default(int?))));
			ints = q3.ToArray();
			q3.Count(x => x == null).ShouldBe(0);
			q3.Count(x => x != null).ShouldBe(2);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(3);

			var q4 = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableInt, 2)));
			ints = q4.ToArray();
			q4.Count(x => x == null).ShouldBe(2);
			q4.Count(x => x != null).ShouldBe(0);
			ints[0].ShouldBe(null);
			ints[1].ShouldBe(null);

			var q5 = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableInt, 4)));
			ints = q5.ToArray();
			q5.Count(x => x == null).ShouldBe(1);
			q5.Count(x => x != null).ShouldBe(1);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(null);

			var q6 = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableInt, default(int?))));
			ints = q6.ToArray();
			q6.Count(x => x == null).ShouldBe(1);
			q6.Count(x => x != null).ShouldBe(1);
			ints[0].ShouldBe(2);
			ints[1].ShouldBe(null);
		}

		[Test]
		public void StringsSql(
			[DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb = SetupSrcTable(db);

			var src = tb.OrderBy(_ => _.Int);

			var q1 = src.Select(s => Sql.AsSql(Sql.NullIf(s.String, "abc")));
			var strings = q1.ToArray();
			q1.Count(x => x == null).ShouldBe(1);
			q1.Count(x => x != null).ShouldBe(1);
			strings[0].ShouldBe(null);
			strings[1].ShouldBe("def");

			var q2 = src.Select(s => Sql.AsSql(Sql.NullIf(s.String, "xyz")));
			strings = q2.ToArray();
			q2.Count(x => x == null).ShouldBe(0);
			q2.Count(x => x != null).ShouldBe(2);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe("def");

			var q3 = src.Select(s => Sql.AsSql(Sql.NullIf(s.String, null)));
			strings = q3.ToArray();
			q3.Count(x => x == null).ShouldBe(0);
			q3.Count(x => x != null).ShouldBe(2);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe("def");

			var q4 = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableString, "abc")));
			strings = q4.ToArray();
			q4.Count(x => x == null).ShouldBe(2);
			q4.Count(x => x != null).ShouldBe(0);
			strings[0].ShouldBe(null);
			strings[1].ShouldBe(null);

			var q5 = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableString, "xyz")));
			strings = q5.ToArray();
			q5.Count(x => x == null).ShouldBe(1);
			q5.Count(x => x != null).ShouldBe(1);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe(null);

			var q6 = src.Select(s => Sql.AsSql(Sql.NullIf(s.NullableString, null)));
			strings = q6.ToArray();
			q6.Count(x => x == null).ShouldBe(1);
			q6.Count(x => x != null).ShouldBe(1);
			strings[0].ShouldBe("abc");
			strings[1].ShouldBe(null);
		}

		sealed class Src
		{
			[PrimaryKey] public int Id { get; set; }
			public int Int { get; set; }
			public int? NullableInt { get; set; }
			public string String { get; set; } = null!;
			public string? NullableString { get; set; }
		}
	}
}
