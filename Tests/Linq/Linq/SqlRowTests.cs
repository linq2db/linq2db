using LinqToDB;
using LinqToDB.Tools;
using NUnit.Framework;
using System;
using System.Linq;
using FluentAssertions;

using static LinqToDB.Sql;

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
