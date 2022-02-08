using LinqToDB;
using LinqToDB.Tools;
using NUnit.Framework;
using System;
using System.Linq;
using FluentAssertions;

using static LinqToDB.Sql;
using DT = System.DateTime;
using DTO = System.DateTimeOffset;

namespace Tests.Linq
{
	[TestFixture]
	public class SqlRowTests : TestBase
	{
		private TempTable<Ints> SetupIntsTable(IDataContext db)
		{
			var data = new[]
			{
				new Ints { One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Nil = (int?)null }
			};

			return db.CreateLocalTable(data);
		}

		[Test]
		public void IsNull([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

			ints.Count(i => Row(i.One, i.Two, i.Three) == null)
				.Should().Be(0);

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

			ints.Count(i => Row(1, (int?)null, 3) == Row(i.One, i.Nil, i.Three))
				.Should().Be(0);
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

			ints.Count(i => Row(0, (int?)null, 3) <= Row(i.One, (int?)i.Two, i.Three))
				.Should().Be(1);
		}

		[Test]
		public void In([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

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
		public void NotIn([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			using var ints = SetupIntsTable(db);

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
			[DataSources(TestProvName.AllOracle, TestProvName.AllPostgreSQL, ProviderName.InformixDB2)] string context)
		{
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

			ints.Count(i => Row(DT.Parse("2020-10-05"), TimeSpan.Parse("6"))
				  .Overlaps(Row(DT.Parse("2020-10-03"), TimeSpan.Parse("1"))))
				.Should().Be(1);

			ints.Count(i => Row(DT.Parse("2020-10-05"), TimeSpan.Parse("6"))
				  .Overlaps(Row(DT.Parse("2020-10-03"), (TimeSpan?)null)))
				.Should().Be(1);
		}

		class Ints
		{
			public int  One   { get; set; }
			public int  Two   { get; set; }
			public int  Three { get; set; }
			public int  Four  { get; set; }
			public int  Five  { get; set; }
			public int? Nil   { get; set; }
		}
	}
}
