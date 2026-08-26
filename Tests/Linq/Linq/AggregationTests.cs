using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class AggregationTests : TestBase
	{
		#region Model

		[Table]
		sealed class Item
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column, NotNull]
			public string Name { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ItemValue.ItemId))]
			public IQueryable<ItemValue> Values { get; set; } = null!;

			public static readonly Item[] Data =
			{
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" },
				new() { Id = 3, Name = "Item3" },
			};
		}

		[Table]
		sealed class ItemValue
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int ItemId { get; set; }

			[Column, NotNull]
			public string ValueName { get; set; } = null!;

			[Column]
			public string? Value { get; set; }

			[Association(ThisKey = nameof(ItemId), OtherKey = nameof(Item.Id), CanBeNull = false)]
			public Item Item { get; set; } = null!;

			public static readonly ItemValue[] Data =
			{
				new() { Id = 1, ItemId = 1, ValueName = "Value1", Value = "10" },
				new() { Id = 2, ItemId = 1, ValueName = "Value2", Value = "20" },
				new() { Id = 3, ItemId = 2, ValueName = "Value3", Value = "30" },
				new() { Id = 4, ItemId = 2, ValueName = "Value4", Value = "abc" }, // non-parseable
				new() { Id = 5, ItemId = 2, ValueName = "Value5", Value = null },   // null value
				new() { Id = 6, ItemId = 3, ValueName = "Value6", Value = "100" },
			};
		}

		#endregion

		[Test]
		public void SumByAssociationSubquery([DataSources] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(Item.Data);
			using var values = db.CreateLocalTable(ItemValue.Data);

			var query = from i in items.LoadWith(x => x.Values)
				group i by i.Id
				into g
				select new
				{
					g.Key,
					Value1Sum = g.Sum(x => x.Values
						.Where(v => v.ValueName == "Value1")
						.Select(v => Sql.ConvertTo<int?>.From(v.Value))
						.SingleOrDefault() ?? 0)
				};

			AssertQuery(query);
		}

		class User
		{
			public int    Id   { get; set; }
			public string Name { get; set; } = null!;
		}

		class UserMachineAssignment
		{
			public int    UserId    { get; set; }
			public string MachineId { get; set; } = null!;

			[Association(ThisKey = nameof(MachineId), OtherKey = nameof(Machine.Id), CanBeNull = false)]
			public Machine Machine { get; set; } = null!;
		}

		class Machine
		{
			public string Id   { get; set; } = null!;
			public string Name { get; set; } = null!;
		}

		[Test]
		public void LeftJoinToStringAggregate([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL15Plus)] string context)
		{
			using var db     = GetDataContext(context);

			var users = new[]
			{
				new User { Id = 1, Name = "User1" },
				new User { Id = 2, Name = "User2" },
			};

			var userMachineAssignments = new[]
			{
				new UserMachineAssignment { UserId = 1, MachineId = "M1" },
				new UserMachineAssignment { UserId = 1, MachineId = "M2" },
				new UserMachineAssignment { UserId = 2, MachineId = "M3" },
			};

			var machines = new[]
			{
				new Machine { Id = "M1", Name = "Machine1" },
				new Machine { Id = "M2", Name = "Machine2" },
				new Machine { Id = "M3", Name = "Machine3" },
			};

			using var usersTable                  = db.CreateLocalTable(users);
			using var userMachineAssignmentsTable = db.CreateLocalTable(userMachineAssignments);
			using var machinesTable               = db.CreateLocalTable(machines);

			var aggregatedQuery = 
				from uma in userMachineAssignmentsTable.LoadWith(x => x.Machine)
				group uma by uma.UserId into g
				select new
				{
					UserId = g.Key, 
					MachineNames = g.StringAggregate(", ", m => m.Machine.Name)
						.OrderBy(x => x.Machine.Name)
						.ToValue(),
					Count = g.Count()
				};

			var query =
				from u in usersTable
				from aq in aggregatedQuery.Where(aq => aq.UserId == u.Id)
					.DefaultIfEmpty()
				select new
				{
					u.Id,
					u.Name,
					aq.MachineNames,
					aq.Count
				};

			AssertQuery(query);
		}

		[Test]
		public void ClosureListCountTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var someList = new List<int>();

			using var db    = GetDataConnection(context);
			using var items = db.CreateLocalTable(Item.Data);

			var rows = items.Where(i => i.Id == someList.Count).ToArray();

			db.LastQuery!.ShouldNotContain("COUNT(");
			rows.ShouldBeEmpty();
		}

		[Test]
		public void ClosureListSumTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var someList = new List<int>();

			using var db    = GetDataConnection(context);
			using var items = db.CreateLocalTable(Item.Data);

			var rows = items.Where(i => i.Id == someList.Sum()).ToArray();

			db.LastQuery!.ShouldNotContain("SUM(");
			rows.ShouldBeEmpty();
		}

		[Test]
		public void ClosureListMinTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var someList = new List<int> { 0 };

			using var db    = GetDataConnection(context);
			using var items = db.CreateLocalTable(Item.Data);

			var rows = items.Where(i => i.Id == someList.Min()).ToArray();

			db.LastQuery!.ShouldNotContain("MIN(");
			rows.ShouldBeEmpty();
		}

		[Test]
		public void ClosureListMaxTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var someList = new List<int> { 0 };

			using var db    = GetDataConnection(context);
			using var items = db.CreateLocalTable(Item.Data);

			var rows = items.Where(i => i.Id == someList.Max()).ToArray();

			db.LastQuery!.ShouldNotContain("MAX(");
			rows.ShouldBeEmpty();
		}

		[Test]
		public void ClosureListAverageTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var someList = new List<int> { 0 };

			using var db    = GetDataConnection(context);
			using var items = db.CreateLocalTable(Item.Data);

			var rows = items.Where(i => i.Id == (int)someList.Average()).ToArray();

			db.LastQuery!.ShouldNotContain("AVG(");
			rows.ShouldBeEmpty();
		}

		/// <summary>
		/// A value added to a sum keeps its own precision rather than the summand column's.
		/// </summary>
		/// <remarks>
		/// The descriptor of the summed column describes how the sum is read back, and for a duration that is what
		/// carries its unit - but it does not describe how wide the sum is, because a sum outgrows the column it is
		/// taken from. A literal typed from that descriptor is narrowed to the column's own scale, and one finer than
		/// the column holds rounds to zero, which makes the addition disappear with nothing raised to report it.
		/// <para>
		/// Asserted on ClickHouse because it writes the width into the literal itself - <c>toDecimal64(…, 4)</c>
		/// against <c>toDecimal128(…, 10)</c> - so the narrowing is visible in the result rather than only in a plan.
		/// </para>
		/// </remarks>
		[Test]
		public void ValueBesideASumKeepsItsOwnPrecision([IncludeDataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var sums = db.Types
				.GroupBy(x => x.ID)
				.Select(g => g.Sum(x => x.MoneyValue))
				.ToArray();

			var shifted = db.Types
				.GroupBy(x => x.ID)
				.Select(g => g.Sum(x => x.MoneyValue))
				.Select(s => s + 0.00005m)
				.ToArray();

			// The addend is smaller than the column's declared scale, so an empty set would satisfy the comparison
			// below without exercising anything.
			sums.ShouldNotBeEmpty();

			// Order-insensitive because nothing here is about order: the two queries are executed separately and
			// grouped without one, and ClickHouse aggregates in parallel, so a positional match would hold by
			// accident. What is asserted is that the addend keeps its own scale, which is per-value.
			shifted.ShouldBe(sums.Select(s => s + 0.00005m), ignoreOrder: true);
		}
	}
}
