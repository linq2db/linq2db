using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	public class Issue5328Tests : TestBase
	{
		[Table("Table1_5328")]
		public class Table1
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string? ContractNumber { get; set; }
		}

		[Table("Table2_5328")]
		public class Table2
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int LinkedId { get; set; }

			[Column]
			public string? ContractNumber { get; set; }

			[Column]
			public decimal Amount { get; set; }
		}

		[Table("Table3_5328")]
		public class Table3
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int LinkedId { get; set; }

			[Column]
			public string? ContractNumber { get; set; }

			[Column]
			public string? Type { get; set; }

			[Column]
			public decimal Amount { get; set; }
		}

		public class TestEntity
		{
			public Table1? Table1 { get; set; }
			public decimal? AddedAmounts { get; set; }
			public decimal? Amount1 { get; set; }
			public decimal? Amount2 { get; set; }
		}

		[Test]
		public void TestLeftJoinWithDefaultIfEmpty([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var table1Data = new[]
			{
				new Table1 { Id = 1, ContractNumber = "C001" },
				new Table1 { Id = 2, ContractNumber = "C002" }
			};

			var table2Data = new[]
			{
				new Table2 { Id = 1, LinkedId = 1, ContractNumber = "C001", Amount = 100m }
			};

			var table3Data = new[]
			{
				new Table3 { Id = 1, LinkedId = 1, ContractNumber = "C001", Type = "Type1", Amount = 500m },
				new Table3 { Id = 2, LinkedId = 1, ContractNumber = "C001", Type = "Type2", Amount = 200m },
				new Table3 { Id = 3, LinkedId = 2, ContractNumber = "C002", Type = "Type1", Amount = 300m },
				new Table3 { Id = 4, LinkedId = 2, ContractNumber = "C002", Type = "Type2", Amount = 150m }
			};

			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable(table1Data);
			using var table2 = db.CreateLocalTable(table2Data);
			using var table3 = db.CreateLocalTable(table3Data);

			var result = (
				from t1 in table1
				from supplementTotal in (
					from t2 in table2.LeftJoin(t2 => t2.LinkedId == t1.Id)
					group t2 by t2.ContractNumber into t2Group
					select new
					{
						supplementAmount = t2Group.Sum(can => can.Amount)
					}
				).DefaultIfEmpty()
				from transactionsTotals in (
					from t3 in table3.LeftJoin(t3 => t3.LinkedId == t1.Id)
					group t3 by t3.ContractNumber into t3Group
					select new
					{
						amount1 = t3Group.Where(dt => dt.Type == "Type1").Sum(d => d.Amount),
						amount2 = t3Group.Where(dt => dt.Type == "Type2").Sum(d => d.Amount)
					}
				).DefaultIfEmpty()
				select new TestEntity
				{
					Table1 = t1,
					AddedAmounts = (Sql.ToNullable(transactionsTotals.amount1) ?? 0m) - (Sql.ToNullable(supplementTotal.supplementAmount) ?? 0m),
					Amount1 = transactionsTotals.amount1,
					Amount2 = transactionsTotals.amount2,
				}
			).ToList();

			result.Count.ShouldBe(2);

			var result1 = result.First(r => r.Table1!.Id == 1);
			result1.Amount1.ShouldBe(500m);
			result1.Amount2.ShouldBe(200m);
			result1.AddedAmounts.ShouldBe(400m, "AddedAmounts should be 400 for ID 1");

			var result2 = result.First(r => r.Table1!.Id == 2);
			result2.Amount1.ShouldBe(300m);
			result2.Amount2.ShouldBe(150m);
			result2.AddedAmounts.ShouldNotBeNull("AddedAmounts should not be null when left join returns no results");
			result2.AddedAmounts.ShouldBe(300m, "AddedAmounts should be 300 (300-0) for ID 2");
		}
	}
}
