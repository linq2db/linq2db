using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

using static LinqToDB.Sql;

using DT = System.DateTime;
using DTO = System.DateTimeOffset;

namespace Tests.Linq
{
	[TestFixture]
	public class SqlRowTests : TestBase
	{
		private TempTable<Ints> SetupIntsTable(IDataContext db, string? tableName = null)
		{
			var data = new[]
			{
				new Ints { One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Nil = (int?)null }
			};

			return db.CreateLocalTable(tableName, data);
		}

		[Test]
		public void ServerSideOnly([DataSources] string context)
		{
			// SqlRow type can't be instantiated client-side, 
			// it's purely a LINQ construct that is converted into SQl code.
			
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			// Top-level select Row() is forbidden. It can be done in subquery only.

			Action selectRow = () => ints.Select(i => Row(i.One, 2)).ToList();
			selectRow.Should().Throw<LinqToDBException>();
		}

		[Test]
		public void IsNull([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) == null)
				.Should().Be(0);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(i.One, i.Nil, (int?)null) == null)
				.Should().Be(0);

			ints.Count(i => Row(i.Nil, (int?)null) == null)
				.Should().Be(1);
		}

		[Test]
		public void IsNotNull([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) != null)
				.Should().Be(1);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(i.One, i.Nil, (int?)null) != null)
				.Should().Be(0);

			ints.Count(i => Row(i.Nil, (int?)null) != null)
				.Should().Be(0);
		}

		[Test]
		public void Equals([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) == Row(i.One, i.One * 2, i.Four - 1))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Two, i.Four) == Row(i.One, i.Two, i.Three))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Nil, i.Three) == Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(0);

			ints.Count(i => Row(1, i.Nil, 3) == Row(i.One, i.Nil, i.Three))
				.Should().Be(0);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(1, (int?)null, 3) == Row(i.One, i.Nil, i.Three))
				.Should().Be(0);
		}

		[Test]
		public void Caching([DataSources] string context, [Values(1, 2, 3)] int r3)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) == Row(i.One, i.One * 2, r3))
				.Should().Be(r3 == 3 ? 1 : 0);
		}

		[Test]
		public void NotEquals([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) != Row(i.One, i.One * 2, i.Four - 1))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Two, i.Four) != Row(i.One, i.Two, i.Three))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Nil, i.Three) != Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(0);

			ints.Count(i => Row(1, i.Nil, 4) != Row(i.One, i.Nil, i.Three))
				.Should().Be(1);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(1, (int?)null, 4) != Row(i.One, i.Nil, i.Three))
				.Should().Be(1);
		}

		[Test]
		public void Greater([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) > Row(i.One, i.One * 2, i.Four - 1))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Two, i.Four) > Row(i.One, i.Two, i.Three))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Two, i.Four) > Row(i.One, i.Five, i.Three))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Nil, i.Four) > Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(0);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(2, (int?)null, 3) > Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(1);
		}

		[Test]
		public void GreaterEquals([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) >= Row(i.One, i.One * 2, i.Four - 1))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Two, i.Four) >= Row(i.One, i.Two, i.Three))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Two, i.Four) >= Row(i.One, i.Five, i.Three))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Nil, i.Four) >= Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(0);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(2, (int?)null, 3) >= Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(1);
		}

		[Test]
		public void Less([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) < Row(i.One, i.One * 2, i.Four - 1))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Two, i.Four) < Row(i.One, i.Two, i.Three))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Two, i.Four) < Row(i.One, i.Five, i.Three))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Nil, i.One) < Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(0);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(0, (int?)null, 3) < Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(1);
		}

		[Test]
		public void LessEquals([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) <= Row(i.One, i.One * 2, i.Four - 1))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Two, i.Four) <= Row(i.One, i.Two, i.Three))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Two, i.Four) <= Row(i.One, i.Five, i.Three))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Nil, i.One) <= Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(0);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row(0, (int?)null, 3) <= Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(1);
		}

		// looks like ClickHouse treats IN as correlated subquery and cannot handle outer column references
		[Test]
		public void In([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two).In(Row(1, i.One * 2)))
			    .Should().Be(1);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row((int?)i.One, i.Two, i.Three).In(
					Row((int?)i.One, i.One * 2, i.Four - 1),
					Row((int?)0, 7, 9),
					Row((int?)null, -1, i.Four)))
				.Should().Be(1);
			
			ints.Count(i => Row((int?)i.One, i.Two, i.Four).In(
					Row((int?)i.One, i.One * 2, i.Four - 1),
					Row((int?)0, 7, 9),
					Row((int?)null, 2, i.Four)))
				.Should().Be(0);

			ints.Count(i => Row(i.Nil, i.Two, i.Four).In(
					Row((int?)i.One, i.One * 2, i.Four - 1),
					Row((int?)0, 7, 9),
					Row((int?)null, 2, i.Four)))
				.Should().Be(0);
		}

		[Test]
		public void NotIn([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two).NotIn(Row(1, i.One * 2)))
			    .Should().Be(0);

			// Informix doesn't like null literals without casts, so following tests are skipped
			if (context.IsAnyOf(TestProvName.AllInformix)) return;

			ints.Count(i => Row((int?)i.One, i.Two, i.Three).NotIn(
					Row((int?)i.One, i.One * 2, i.Four - 1),
					Row((int?)0, 7, 9),
					Row((int?)null, -1, i.Four)))
				.Should().Be(0);
			
			ints.Count(i => Row((int?)i.One, i.Three, i.Four).NotIn(
					Row((int?)i.One, i.One * 2, i.Four - 1),
					Row((int?)0, 7, 9),
					Row((int?)null, 2, i.Four)))
				.Should().Be(1);

			ints.Count(i => Row((int?)i.One, i.Two, i.Four).NotIn(
					Row((int?)i.One, i.One * 2, i.Four - 1),
					Row((int?)0, 7, 9),
					Row((int?)null, 2, i.Four)))
				.Should().Be(0);

			ints.Count(i => Row(i.Nil, i.Two, i.Four).NotIn(
					Row((int?)i.One, i.One * 2, i.Four - 1),
					Row((int?)0, 7, 9),
					Row((int?)null, 2, i.Four)))
				.Should().Be(0);
		}

		[Test]
		public void Between([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two).Between(
					Row(i.One, i.One * 2),
					Row(i.One, i.One + i.One)))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Three).Between(
					Row(i.One, i.One),
					Row(i.One, i.Four)))
				.Should().Be(1);

			ints.Count(i => Row(i.One, i.Two).Between(
					Row(i.One, i.Three),
					Row(i.One, i.Two)))
				.Should().Be(0);

			ints.Count(i => Row(i.Two, i.Five).Between(
					Row(i.One, i.One),
					Row(i.Three, i.Two)))
				.Should().Be(1);

			ints.Count(i => Row(i.Two, i.Five).Between(
					Row(i.One, i.One),
					Row(i.Two, i.Two)))
				.Should().Be(0);

			ints.Count(i => Row(i.Two, i.Nil).Between(
					Row<int, int?>(i.One, i.One),
					Row<int, int?>(i.Three, i.One)))
				.Should().Be(1);

			ints.Count(i => Row(i.Two, i.Nil).Between(
					Row<int, int?>(i.Two, i.One),
					Row<int, int?>(i.Two, i.Three)))
				.Should().Be(0);

			ints.Count(i => Row<int, int?>(i.Two, i.Five).Between(
					Row(i.One, i.Nil),
					Row(i.Three, i.Nil)))
				.Should().Be(1);

			ints.Count(i => Row(i.Two, i.Nil).Between(
					Row(i.One, i.Nil),
					Row(i.Three, i.Nil)))
				.Should().Be(1);

			ints.Count(i => Row<int?, int>(i.Two, i.Two).Between(
					Row(i.Nil, i.One),
					Row<int?, int>(i.Three, i.Five)))
				.Should().Be(0);
		}

		[Test]
		public void NotBetween([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two).NotBetween(
					Row(i.One, i.One * 2),
					Row(i.One, i.One + i.One)))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Three).NotBetween(
					Row(i.One, i.One),
					Row(i.One, i.Four)))
				.Should().Be(0);

			ints.Count(i => Row(i.One, i.Two).NotBetween(
					Row(i.One, i.Three),
					Row(i.One, i.Two)))
				.Should().Be(1);

			ints.Count(i => Row(i.Two, i.Five).NotBetween(
					Row(i.One, i.One),
					Row(i.Three, i.Two)))
				.Should().Be(0);

			ints.Count(i => Row(i.Two, i.Five).NotBetween(
					Row(i.One, i.One),
					Row(i.Two, i.Two)))
				.Should().Be(1);

			ints.Count(i => Row(i.Two, i.Nil).NotBetween(
					Row<int, int?>(i.One, i.One),
					Row<int, int?>(i.Three, i.One)))
				.Should().Be(0);

			ints.Count(i => Row(i.Two, i.Nil).NotBetween(
					Row<int, int?>(i.Two, i.One),
					Row<int, int?>(i.Two, i.Three)))
				.Should().Be(0);

			ints.Count(i => Row<int, int?>(i.Two, i.Five).NotBetween(
					Row(i.One, i.Nil),
					Row(i.Three, i.Nil)))
				.Should().Be(0);

			ints.Count(i => Row(i.Two, i.Nil).NotBetween(
					Row(i.One, i.Nil),
					Row(i.Three, i.Nil)))
				.Should().Be(0);

			ints.Count(i => Row<int?, int>(i.Two, i.Two).NotBetween(
					Row(i.Nil, i.One),
					Row<int?, int>(i.Three, i.Five)))
				.Should().Be(0);
		}

		[Test]
		public void Overlaps(
			[IncludeDataSources(true, TestProvName.AllOracle/*, TestProvName.AllClickHouse, TestProvName.AllPostgreSQL, ProviderName.DB2 */)] string context)
		{
			// Postgre and DB2 have support but needs to know the type of parameters explicitely,
			// so this test wouldn't work without adding casts at every constant.

			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			// OVERLAPS is neither client-evaluated, nor converted in providers without native support.
			// So tests are short because we don't want to test all edge cases of provider implementation.
			// We simply want to check if valid SQL is generated for all basic support types

			ints.Count(i => Row(DT.Parse("2020-10-01"), DT.Parse("2020-10-05"))
				  .Overlaps(Row(DT.Parse("2020-10-03"), DT.Parse("2020-11-09"))))
				.Should().Be(1);

			ints.Count(i => Row(DTO.Parse("2020-10-05"), DTO.Parse("2020-10-01"))
				  .Overlaps(Row(DTO.Parse("2020-10-03"), DTO.Parse("2020-11-09"))))
				.Should().Be(1);

			ints.Count(i => Row(DT.Parse("2020-10-03"), TimeSpan.Parse("6"))
				  .Overlaps(Row(DT.Parse("2020-10-05"), TimeSpan.Parse("1"))))
				.Should().Be(1);

			ints.Count(i => Row(DT.Parse("2020-10-03"), (TimeSpan?)TimeSpan.Parse("6"))
				  .Overlaps(Row(DT.Parse("2020-10-05"), (TimeSpan?)null)))
				.Should().Be(1);
		}

		[Test]
		public void EqualToSelect(
			[IncludeDataSources(true,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite)] string context)
		{
			// This feature is not emulated when there's no native support.
			// So tests are not exhaustive, we just want to check that correct SQL is generated.
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);
			using var ints2 = SetupIntsTable(db, "Ints2");

			ints.Count(x => Row(x.One, x.Two, x.Three) == 
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.One + 1, 3)).Single())
				.Should().Be(1);

			// Because operator == is defined for `object`, .Single() is not required
			ints.Count(x => Row(x.One, x.Two, x.Three) == 
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.One + 1, 3)))
				.Should().Be(1);

			ints.Count(x => (from y in ints2
					where y.Nil == null
					select Row(y.One, y.One + 1, 3)) == Row(x.One, x.Two, x.Three))
				.Should().Be(1);

			ints.Count(x => Row(x.One, x.Two, x.Three) !=
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.One + 1, 4)).Single())
				.Should().Be(1);

			// Because operator != is defined for `object`, .Single() is not required
			ints.Count(x => Row(x.One, x.Two, x.Three) !=
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.One + 1, 4)))
				.Should().Be(1);
		}

		[Test]
		public void CompareToSelect(
			[IncludeDataSources(true,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite)] string context)
		{
			// This feature is not emulated when there's no native support.
			// So tests are not exhaustive, we just want to check that correct SQL is generated.
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);
			using var ints2 = SetupIntsTable(db, "Ints2");

			ints.Count(x => Row(x.One, x.Two, x.Nil) >
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.One, (int?)3)).Single())
				.Should().Be(1);

			ints.Count(x => Row(x.One, x.Two, x.Three) >=
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.One + 1, 3)).Single())
				.Should().Be(1);

			ints.Count(x => Row(x.One, x.Two, x.Nil) <
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.Three, (int?)3)).Single())
				.Should().Be(1);

			ints.Count(x => Row(x.One, x.Two, x.Three) <=
				(from y in ints2
				 where y.Nil == null
				 select Row(y.One, y.One + 1, 3)).Single())
				.Should().Be(1);
		}

		[Test]
		public void MixedTypes([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var data = new[]
			{
				new Mixed { Int = 1, Str = "One", Date = new DateTime(2001, 1, 1), Double = 1.0, Bool = true },
				new Mixed { Int = 2, Str = "Two", Date = new DateTime(2002, 2, 2), Double = 2.0, Bool = false },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			table.Count(t => 
					t.Int > 0 && 
					Row(t.Str, t.Double, t.Bool) == Row("One", 1.0, true) &&
					table.Any(u => Row(2, u.Date) > Row(u.Int, t.Date)))
				.Should().Be(1);
		}

		[Test]
		public void UpdateRowLiteral(
			[IncludeDataSources(true, ProviderName.DB2, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			var data = new[]
			{
				new Ints { One = 1,  Two = 2,  Three = 3,  Four = 4,  Five = 5,  Nil = (int?)null },
				new Ints { One = 10, Two = 20, Three = 30, Four = 40, Five = 50, Nil = (int?)null },
			};

			using var db   = GetDataContext(context);
			using var ints = db.CreateLocalTable(data);

			ints.Where(i => i.One == 10)
				.Set(i => i.One, i => i.Two * 5)
				.Set(i => Row(i.Two, i.Three), i => Row(200, i.Three * 10))
				.Set(i => Row(i.Four, i.Nil), i => Row(i.One * i.Four, (int?)600))
				.Update();

			ints.OrderBy(i => i.One)
				.ToList()
				.Should().Equal(
					new Ints { One = 1,   Two = 2,   Three = 3,   Four = 4,   Five = 5,  Nil = (int?)null },
					new Ints { One = 100, Two = 200, Three = 300, Four = 400, Five = 50, Nil = 600 });
		}

		// TODO: this test should be rewritten to use different table as values source as currently update optimizer removes subquery for most of providers
		[Test]
		public void UpdateRowSelect(
			[IncludeDataSources(true,
				ProviderName.DB2,
				TestProvName.AllPostgreSQL95Plus,
				TestProvName.AllSQLite,
				TestProvName.AllOracle)] string context)
		{
			var data = new[]
			{
				new Ints { One = 1,  Two = 2,  Three = 3,  Four = 4,  Five = 5,  Nil = (int?)null },
				new Ints { One = 10, Two = 20, Three = 30, Four = 40, Five = 50, Nil = (int?)null },
			};

			using var db   = GetDataContext(context);
			using var ints = db.CreateLocalTable(data);

			ints.Where(i => i.One == 10)
				.Set(i => i.One, i => i.Two * 5)
				.Set(
					i => Row(i.Two, i.Three),
					i => (from j in ints
						  where j.One == 1
						  select Row(i.Two * 10, j.Three * 100))
						 .Single())
				.Set(
					i => Row(i.Four, i.Nil),
					i => db.SelectQuery(() => Row(i.One * i.Four, (int?)600))
					       .Single())
				.Update();

			ints.OrderBy(i => i.One)
				.ToList()
				.Should().Equal(
					new Ints { One = 1,   Two = 2,   Three = 3,   Four = 4,   Five = 5,  Nil = (int?)null },
					new Ints { One = 100, Two = 200, Three = 300, Four = 400, Five = 50, Nil = 600 });
		}

		sealed class Ints : IEquatable<Ints>
		{
			public int  One   { get; set; }
			public int  Two   { get; set; }
			public int  Three { get; set; }
			public int  Four  { get; set; }
			public int  Five  { get; set; }
			public int? Nil   { get; set; }

			public bool Equals(Ints? other)
			{
				return other       != null
					&& other.One   == One
					&& other.Two   == Two
					&& other.Three == Three
					&& other.Four  == Four
					&& other.Five  == Five
					&& other.Nil   == Nil;
			}
		}

		sealed class Mixed
		{
			public int      Int    { get; set; }
			public string?  Str    { get; set; }
			public DateTime Date   { get; set; }
			public double   Double { get; set; }
			public bool     Bool   { get; set; }
		}

		#region Issue 3631
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3631")]
		public void Issue3631Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3631Table.Data);

			var result = tb
					.Where(x => Row(x.Country, x.State).In(Row("US", "CA"), Row("US", "NY")))
					.ToList();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3631")]
		public void Issue3631Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3631Table.Data);

			var items = new List<(string Country, string State)>();
			items.Add(("US", "CA"));
			items.Add(("US", "NY"));
			var rows = items.Select(item => Row(item.Country, item.State)).ToList();

			var result = tb
					.Where(x => Row(x.Country, x.State).In(rows))
					.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3631")]
		public void Issue3631Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3631Table.Data);

			var items = new List<(string Country, string State)>();
			items.Add(("US", "CA"));
			items.Add(("US", "NY"));

			var result = tb
					.Where(x => Row(x.Country, x.State).In(items.Select(item => Row(item.Country, item.State))))
					.ToList();
		}

		// missing API
		//[Test(Description = "https://github.com/linq2db/linq2db/issues/3631")]
		//public void Issue3631Test4([DataSources] string context)
		//{
		//	using var db = GetDataContext(context);
		//	using var tb = db.CreateLocalTable(Issue3631Table.Data);

		//	var items = new List<(string Country, string State)>();
		//	items.Add(("US", "CA"));
		//	items.Add(("US", "NY"));

		//	var result = tb
		//			.Where(x => Row(x.Country, x.State).In(items))
		//			.ToList();
		//}

		[Table]
		sealed class Issue3631Table
		{
			[Column(Length = 2), NotNull] public string Country { get; set; } = null!;
			[Column(Length = 2), NotNull] public string State { get; set; } = null!;

			public static readonly Issue3631Table[] Data = new[]
			{
				new Issue3631Table() { Country = "US", State = "AL" },
				new Issue3631Table() { Country = "US", State = "AZ" },
				new Issue3631Table() { Country = "US", State = "CA" },
				new Issue3631Table() { Country = "US", State = "FL" },
				new Issue3631Table() { Country = "US", State = "IN" },
				new Issue3631Table() { Country = "US", State = "OH" },
				new Issue3631Table() { Country = "US", State = "NY" },
				new Issue3631Table() { Country = "CA", State = "AB" },
				new Issue3631Table() { Country = "CA", State = "ON" },
			};
		}
		#endregion
	}
}
