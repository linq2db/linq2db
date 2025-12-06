using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

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

	}
}
