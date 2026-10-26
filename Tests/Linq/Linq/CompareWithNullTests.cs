using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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
			var entity = new FluentMappingBuilder(ms).Entity<Src>();
			entity.Property(e => e.CEnumA)
				.HasDataType(DataType.VarChar)
				.HasLength(20)
				.HasConversion(v => $"___{v}___", v => Enum.Parse<CE>(v.Substring(3, v.Length - 6)));
			entity.Property(e => e.CEnumB)
				.HasDataType(DataType.VarChar)
				.HasLength(20)
				.HasConversion(v => $"___{v}___", v => Enum.Parse<CE>(v.Substring(3, v.Length - 6)));
			entity.Build();
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
			// This test can run in all providers, but it adds 108 tests per provider.
			// As the behaviour is the same everywhere, we can speed things up by
			// only running in a single provider.
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values]                                    CompareNulls compareNulls,
			// Use an indirect index into the test case data instead of [ValuesSource].
			// The parameter (tuple including expression) is serialized into the test name
			// when creating baseline file and this would result in path names too long for Windows to handle.
			[Range(0, 35)]                              int index)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(mappingSchema).UseCompareNulls(compareNulls));
			using var src = db.CreateLocalTable(Data);

			var (where, withoutNulls, withNulls) = _conditions[index];

			var conditionStr = new ExpressionPrinter().PrintExpression(where);

			var result = src
				.Where(where)
				.OrderBy(x => x.Id)
				.Select(x => x.Id)
				.TagQuery(conditionStr)
				.ToArray();

			if (compareNulls == CompareNulls.LikeClr)
			{
				var linqResult = Data
					.Where(where.Compile())
					.Select(x => x.Id)
					.ToArray();

				// LikeClr should behave like C#
				result.ShouldBeEquivalentTo(linqResult);
				// Which should be the same as expectations
				result.ShouldBeEquivalentTo(withNulls);
			}
			else
			{
				// As this test uses DB columns only, no parameters, LikeSql and LikeSqlExceptParameters should behave the same way.
				// See `Options` test below for a test case that differentiates them.
				result.ShouldBeEquivalentTo(withoutNulls);
			}
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
			result.ShouldBe(2);

			// == default is the same as == null
			result = src.Where(x => x.A == default).Count();
			result.ShouldBe(2);

			// LikeClr should obviously match.
			// LikeSqlExceptParameters sniffs parameters and should translate to IS NULL.
			// LikeSql should translate straight to x.A = p, which should have no result.
			int? p = null;
			result = src.Where(x => x.A == p).Count();
			result.ShouldBe(option == CompareNulls.LikeSql ? 0 : 2);
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
			result.ShouldBe(2);

			// LikeSql should translate straight to x.A = p, which should have no result.
			var p = "";
			result = src.Where(x => x.Text == p).Select(x => x.Id).FirstOrDefault();
			result.ShouldBe(option == CompareNulls.LikeSql ? 0 : 2);
		}

		const int HeadOfficeRegion = 7;

		[Table]
		sealed class RegionalCustomer
		{
			[PrimaryKey] public int  Id       { get; set; }
			[Column    ] public int? RegionId { get; set; }
		}

		[Table]
		sealed class RegionalOrder
		{
			[PrimaryKey] public int Id         { get; set; }
			[Column    ] public int CustomerId { get; set; }
		}

		private static readonly RegionalCustomer[] RegionalCustomers = new[]
		{
			new RegionalCustomer { Id = 1, RegionId = null             },
			new RegionalCustomer { Id = 2, RegionId = 5                },
			new RegionalCustomer { Id = 3, RegionId = HeadOfficeRegion },
		};

		private static readonly RegionalOrder[] RegionalOrders = new[]
		{
			new RegionalOrder { Id = 10, CustomerId = 1 },
			new RegionalOrder { Id = 20, CustomerId = 2 },
			new RegionalOrder { Id = 30, CustomerId = 3 },
		};

		// An optional filter left unset is a parameter that evaluates to NULL, not the NULL literal. Under the default
		// CompareNulls.LikeClr comparing it keeps the CLR meaning: null == 7 is false and null != 7 is true.
		[Test]
		public void UnsetFilterComparedWithConstant([DataSources] string context)
		{
			using var db        = GetDataContext(context);
			using var customers = db.CreateLocalTable(RegionalCustomers);

			int? selectedRegion = null;

			var headOfficeSelected = customers
				.Where(c => Sql.AsSql(selectedRegion) == HeadOfficeRegion);

			var otherRegionSelected = customers
				.Where(c => Sql.AsSql(selectedRegion) != HeadOfficeRegion);

			AssertQuery(headOfficeSelected);
			AssertQuery(otherRegionSelected);
		}

		[Test]
		public void UnsetFilterInCompositeJoinKey([DataSources] string context)
		{
			using var db        = GetDataContext(context);
			using var customers = db.CreateLocalTable(RegionalCustomers);
			using var orders    = db.CreateLocalTable(RegionalOrders);

			int? selectedRegion = null;

			var query =
				from c in customers
				join o in orders
					on new { c.Id, Region = selectedRegion } equals new { Id = o.CustomerId, Region = (int?)HeadOfficeRegion }
				select o;

			AssertQuery(query);
		}

		[Test]
		public void UnsetFilterNotInRegionList([DataSources] string context)
		{
			using var db        = GetDataContext(context);
			using var customers = db.CreateLocalTable(RegionalCustomers);

			int? selectedRegion = null;

			// an unset filter is found only in the list of a customer without a region, so every other customer is kept
			var query = customers
				.Where(c => !new int?[] { c.RegionId, HeadOfficeRegion }.Contains(selectedRegion));

			AssertQuery(query);
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
