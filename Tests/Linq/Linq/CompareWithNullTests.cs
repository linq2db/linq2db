using FluentAssertions;
using LinqToDB;
using LinqToDB.Tools;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

		private TempTable<Src> SetupSrcTable(IDataContext db)
		{
			var entity = db.GetFluentMappingBuilder().Entity<Src>();
			entity.Property(e => e.CEnumA)
				.HasDataType(DataType.VarChar)
				.HasLength(20)
				.HasConversion(v => $"___{v}___", v => (CE)Enum.Parse(typeof(CE), v.Substring(3, v.Length - 6)));
			entity.Property(e => e.CEnumB)
				.HasDataType(DataType.VarChar)
				.HasLength(20)
				.HasConversion(v => $"___{v}___", v => (CE)Enum.Parse(typeof(CE), v.Substring(3, v.Length - 6)));

			return db.CreateLocalTable(Data);
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
			[DataSources] string context,
			[Values]      bool   withNullCompares,
			// Use an indirect index into the test case data instead of [ValuesSource].
			// The parameter (tuple including expression) is serialized into the test name
			// when creating baseline file and this would result in path names too long for Windows to handle.
			[Range(0, 35)] int index)
		{
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			var data = _conditions[index];

			var result = src
				.Where(data.where)
				.OrderBy(x => x.Id)
				.Select(x => x.Id)
				.ToList();

			result.Should().Equal(withNullCompares ? data.withNulls : data.withoutNulls);

			// CompareWithNullValues should behave exactly like C#
			if (withNullCompares)
			{
				var linqResult = Data
					.Where(data.where.Compile())
					.Select(x => x.Id)
					.ToList();

				result.Should().Equal(linqResult);
			}
		}

		public class Src
		{
			public int  Id     { get; set; }
			public int? A      { get; set; }
			public int? B      { get; set; }
			public ME?  EnumA  { get; set; }
			public ME?  EnumB  { get; set; }
			public CE?  CEnumA { get; set; }
			public CE?  CEnumB { get; set; }
		}

		public enum ME { [MapValue("A")] One, [MapValue("B")] Two }

		public enum CE { One, Two }
	}
}
