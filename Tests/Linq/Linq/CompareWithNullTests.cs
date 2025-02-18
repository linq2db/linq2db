using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CompareWithNullTests : TestBase
	{
		private static readonly Src[] Data = new[]
		{
			new Src { Id = 100, A = null, B = null, EnumA = null,   EnumB = null,   CEnumA = null,   CEnumB = null   },
			new Src { Id = 101, A = null, B = 1,    EnumA = null,   EnumB = ME.One, CEnumA = null,   CEnumB = CE.One },
			new Src { Id = 110, A = 1,    B = null, EnumA = ME.One, EnumB = null,   CEnumA = CE.One, CEnumB = null   },
			new Src { Id = 111, A = 1,    B = 1,    EnumA = ME.One, EnumB = ME.One, CEnumA = CE.One, CEnumB = CE.One },
			new Src { Id = 112, A = 1,    B = 2,    EnumA = ME.One, EnumB = ME.Two, CEnumA = CE.One, CEnumB = CE.Two },
			new Src { Id = 121, A = 2,    B = 1,    EnumA = ME.Two, EnumB = ME.One, CEnumA = CE.Two, CEnumB = CE.One },
		};

		private readonly MappingSchema mappingSchema = BuildMappingSchema();

		private static MappingSchema BuildMappingSchema()
		{
			var ms = new MappingSchema();
#pragma warning disable CA2263 // Prefer generic overload when type is known, but Enum.Parse<E> is not available in .net fx
			var entity = new FluentMappingBuilder(ms).Entity<Src>();
			entity.Property(e => e.CEnumA)
				.HasDataType(DataType.VarChar)
				.HasLength(20)
				.HasConversion(v => $"___{v}___", v => (CE)Enum.Parse(typeof(CE), v.Substring(3, v.Length - 6)));
			entity.Property(e => e.CEnumB)
				.HasDataType(DataType.VarChar)
				.HasLength(20)
				.HasConversion(v => $"___{v}___", v => (CE)Enum.Parse(typeof(CE), v.Substring(3, v.Length - 6)));
			entity.Build();
#pragma warning restore CA2263 // Prefer generic overload when type is known
			return ms;
		}

		private static readonly (Expression<Func<Src, bool>> where, int[] withoutNulls, int[] withNulls)[] _conditions 
			= new (Expression<Func<Src, bool>> where, int[] withoutNulls, int[] withNulls)[]
			{
				(x => x.A == x.B, new[] { 111 },      new[] { 100, 111 }),
				(x => x.A != x.B, new[] { 112, 121 }, new[] { 101, 110, 112, 121 }),
				(x => x.A >= x.B, new[] { 111, 121 }, new[] { 111, 121 }),
				(x => x.A >  x.B, new[] { 121 },      new[] { 121 }),
				(x => x.A <= x.B, new[] { 111, 112 }, new[] { 111, 112 }),
				(x => x.A <  x.B, new[] { 112 },      new[] { 112 }),

				(x => !(x.A == x.B), new[] { 112, 121 }, new[] { 101, 110, 112, 121 }),
				(x => !(x.A != x.B), new[] { 111 },      new[] { 100, 111 }),
				(x => !(x.A >= x.B), new[] { 112 },      new[] { 100, 101, 110, 112 }),
				(x => !(x.A >  x.B), new[] { 111, 112 }, new[] { 100, 101, 110, 111, 112 }),
				(x => !(x.A <= x.B), new[] { 121 },      new[] { 100, 101, 110, 121 }),
				(x => !(x.A <  x.B), new[] { 111, 121 }, new[] { 100, 101, 110, 111, 121 }),

				(x => x.EnumA == x.EnumB, new[] { 111 },      new[] { 100, 111 }),
				(x => x.EnumA != x.EnumB, new[] { 112, 121 }, new[] { 101, 110, 112, 121 }),
				(x => x.EnumA >= x.EnumB, new[] { 111, 121 }, new[] { 111, 121 }),
				(x => x.EnumA >  x.EnumB, new[] { 121 },      new[] { 121 }),
				(x => x.EnumA <= x.EnumB, new[] { 111, 112 }, new[] { 111, 112 }),
				(x => x.EnumA <  x.EnumB, new[] { 112 },      new[] { 112 }),

				(x => !(x.EnumA == x.EnumB), new[] { 112, 121 }, new[] { 101, 110, 112, 121 }),
				(x => !(x.EnumA != x.EnumB), new[] { 111 },      new[] { 100, 111 }),
				(x => !(x.EnumA >= x.EnumB), new[] { 112 },      new[] { 100, 101, 110, 112 }),
				(x => !(x.EnumA >  x.EnumB), new[] { 111, 112 }, new[] { 100, 101, 110, 111, 112 }),
				(x => !(x.EnumA <= x.EnumB), new[] { 121 },      new[] { 100, 101, 110, 121 }),
				(x => !(x.EnumA <  x.EnumB), new[] { 111, 121 }, new[] { 100, 101, 110, 111, 121 }),

				(x => x.CEnumA == x.CEnumB, new[] { 111 },      new[] { 100, 111 }),
				(x => x.CEnumA != x.CEnumB, new[] { 112, 121 }, new[] { 101, 110, 112, 121 }),
				(x => x.CEnumA >= x.CEnumB, new[] { 111, 121 }, new[] { 111, 121 }),
				(x => x.CEnumA >  x.CEnumB, new[] { 121 },      new[] { 121 }),
				(x => x.CEnumA <= x.CEnumB, new[] { 111, 112 }, new[] { 111, 112 }),
				(x => x.CEnumA <  x.CEnumB, new[] { 112 },      new[] { 112 }),

				(x => !(x.CEnumA == x.CEnumB), new[] { 112, 121 }, new[] { 101, 110, 112, 121 }),
				(x => !(x.CEnumA != x.CEnumB), new[] { 111 },      new[] { 100, 111 }),
				(x => !(x.CEnumA >= x.CEnumB), new[] { 112 },      new[] { 100, 101, 110, 112 }),
				(x => !(x.CEnumA >  x.CEnumB), new[] { 111, 112 }, new[] { 100, 101, 110, 111, 112 }),
				(x => !(x.CEnumA <= x.CEnumB), new[] { 121 },      new[] { 100, 101, 110, 121 }),
				(x => !(x.CEnumA <  x.CEnumB), new[] { 111, 121 }, new[] { 100, 101, 110, 111, 121 }),
			};

		[Test]
		public void Functional(
			// This test can run in all providers, but it adds 72 tests per provider.
			// As the behaviour is the same everywhere, we can speed things up by 
			// only running for in a single provider.
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values]                                    bool   withNullCompares,
			// Use an indirect index into the test case data instead of [ValuesSource].
			// The parameter (tuple including expression) is serialized into the test name
			// when creating baseline file and this would result in path names too long for Windows to handle.
			[Range(0, 35)]                              int index)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(mappingSchema).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = db.CreateLocalTable(Data);

			var (where, withoutNulls, withNulls) = _conditions[index];

			var conditionStr = new ExpressionPrinter().PrintExpression(where);

			var result = src
				.Where(where)
				.OrderBy(x => x.Id)
				.Select(x => x.Id)
				.TagQuery(conditionStr)
				.ToList();

			// CompareWithNullValues should behave exactly like C#
			if (withNullCompares)
			{
				var linqResult = Data
					.Where(where.Compile())
					.Select(x => x.Id)
					.ToList();

				result.Should().Equal(linqResult);
			}

			result.Should().Equal(withNullCompares ? withNulls : withoutNulls);
		}

		[Test]
		public void Options(
			// This test can run in all providers, but the same everywhere
			// as we're just testing the compiler options
			[IncludeDataSources(ProviderName.SQLiteMS)] string       context,
			[Values]                                    CompareNulls option)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(mappingSchema).UseCompareNulls(option));
			using var src = db.CreateLocalTable(Data);
			
			// == null always translates to IS NULL
			var result = src.Where(x => x.A == null).Count();
			result.Should().Be(2);
			
			// == default is the same as == null
			result = src.Where(x => x.A == default).Count();
			result.Should().Be(2);

			// LikeClr should obviously match.
			// LikeSqlExceptParameters sniffs parameters and should translate to IS NULL.
			// LikeSql should translate straight to x.A = p, which should have no result.
			int? p = null;
			result = src.Where(x => x.A == p).Count();
			result.Should().Be(option == CompareNulls.LikeSql ? 0 : 2);
		}

		[Test]
		public void OracleEmptyStrings(
			[IncludeDataSources(TestProvName.AllOracle)] string      context,
			[Values]                                    CompareNulls option)
		{
			using var db  = GetDataContext(context, o => o.UseCompareNulls(option));
			using var src = db.CreateLocalTable(new[]
			{
				new Src { Id = 1, Text = "abc" },
				new Src { Id = 2, Text = null  },
			});
			
			// "" is the same as null in Oracle and == ""  should always translates to IS NULL
			int result = src.Where(x => x.Text == "").Select(x => x.Id).FirstOrDefault();
			result.Should().Be(2);

			// LikeSql should translate straight to x.A = p, which should have no result.
			var p = "";
			result = src.Where(x => x.Text == p).Select(x => x.Id).FirstOrDefault();
			result.Should().Be(option == CompareNulls.LikeSql ? 0 : 2);
		}

		public class Src
		{
			public int     Id     { get; set; }
			public int?    A      { get; set; }
			public int?    B      { get; set; }
			public ME?     EnumA  { get; set; }
			public ME?     EnumB  { get; set; }
			public CE?     CEnumA { get; set; }
			public CE?     CEnumB { get; set; }
			public string? Text   { get; set; }
		}

		public enum ME { [MapValue("A")] One, [MapValue("B")] Two }

		public enum CE { One, Two }
	}
}
