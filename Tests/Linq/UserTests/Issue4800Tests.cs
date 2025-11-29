using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4800Tests : TestBase
	{
		// Data Model Classes
		public class TesteeCoverage
		{
			public int    Id          { get; set; }
			public int    TestId      { get; set; }
			public int    TesteeId    { get; set; }
			public int    Status      { get; set; }
			public string Token       { get; set; } = default!;
			public int    StructureId { get; set; }
			public int    SoleId      { get; set; }
			public int    Distance    { get; set; }
			public int    Placement   { get; set; }
		}

		public class Test
		{
			[PrimaryKey] public int    Id   { get; set; }
			public string Name { get; set; } = default!;
		}

		public class Executant
		{
			[PrimaryKey] public int Id     { get; set; }
			public int TestId { get; set; }
		}

		public class Testee
		{
			[PrimaryKey] public int Id          { get; set; }
			public int WorkblankId { get; set; }
		}

		public class Outfit
		{
			[PrimaryKey] public int WorkblankId { get; set; }
			[PrimaryKey] public int AccountId   { get; set; }
		}

		public class Account
		{
			[PrimaryKey] public int    Id      { get; set; }
			public string Name    { get; set; } = default!;
			public string Surname { get; set; } = default!;
		}

		public class Coverage
		{
			[PrimaryKey] public int Id { get; set; }
			public int     TestId      { get; set; }
			public int     TesteeId    { get; set; }
			public int     StructureId { get; set; }
			public int     SoleId      { get; set; }
			public decimal Distance    { get; set; }
			public int     Placement   { get; set; }
			public int     Status      { get; set; }
			public string Token { get; set; } = default!;
		}

		static List<Coverage> GetCoverages() => new List<Coverage>
		{
			new Coverage { Id = 1, TestId = 1, TesteeId = 1, Status = 14650, Distance = 10.5m, Placement = 2 },
			new Coverage { Id = 2, TestId = 2, TesteeId = 2, Status = 14650, Distance = 5.3m, Placement  = 1 }
		};

		static List<Test> GetTests() => new List<Test>
		{
			new Test { Id = 1 },
			new Test { Id = 2 }
		};

		static List<Testee> GetTestees() => new List<Testee>
		{
			new Testee { Id = 1, WorkblankId = 1 },
			new Testee { Id = 2, WorkblankId = 2 }
		};

		static List<Outfit> GetOutfits() => new List<Outfit>
		{
			new Outfit { WorkblankId = 1, AccountId = 1 },
			new Outfit { WorkblankId = 2, AccountId = 2 }
		};

		static List<Account> GetAccounts() => new List<Account>
		{
			new Account { Id = 1, Name = "John", Surname = "Doe" },
			new Account { Id = 2, Name = "Jane", Surname = "Smith" }
		};

		static List<Executant> GetExecutants() => new List<Executant>
		{
			new Executant { Id = 1, TestId = 1 },
			new Executant { Id = 2, TestId = 2 }
		};

		[YdbTableNotFound]
		[Test]
		public void JoinUsingAggregateResult([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(GetCoverages());
			using var testTable = db.CreateLocalTable(GetTests());
			using var executantTable = db.CreateLocalTable(GetExecutants());
			using var testeeTable = db.CreateLocalTable(GetTestees());
			using var outfitTable = db.CreateLocalTable(GetOutfits());
			using var accountTable = db.CreateLocalTable(GetAccounts());

			var query =
				from coverage in db.GetTable<Coverage>()
				join test in db.GetTable<Test>() on coverage.TestId equals test.Id
				join executant in db.GetTable<Executant>() on test.Id equals executant.TestId into executants
				from executant in executants.DefaultIfEmpty()
				join testee in db.GetTable<Testee>() on coverage.TesteeId equals testee.Id into testees
				from testee in testees.DefaultIfEmpty()
				join outfit in db.GetTable<Outfit>() on testee.WorkblankId equals outfit.WorkblankId into outfits
				from outfit in outfits.DefaultIfEmpty()
				join account in db.GetTable<Account>() on outfit.AccountId equals account.Id into accounts
				from account in accounts.DefaultIfEmpty()
				where coverage.Status == 14650
				select new
				{
					Coverage = coverage,
					Test     = test,
					Account  = account
				} into item
				group item by new { item.Coverage.Token, item.Coverage.TesteeId, item.Coverage.TestId, item.Coverage.StructureId, item.Coverage.SoleId, item.Coverage.Distance, item.Coverage.Placement } into elements
				select new
				{
					elements.Key.Token,
					elements.Key.TesteeId,
					elements.Key.TestId,
					elements.Key.StructureId,
					elements.Key.SoleId,
					elements.Key.Distance,
					elements.Key.Placement,
					Win = elements.Key.Placement == 1 ? 1 : 0
				} into entry
				group entry by entry.Token into elements
				select new
				{
					elements.Key,
					TesteeId    = elements.Min(item => item.TesteeId),
					StructureId = elements.Min(item => item.StructureId),
					SoleId      = elements.Min(item => item.SoleId),
					Wins        = elements.Sum(item => item.Win),
					Runs        = elements.Select(item => item.TestId).Count(),
					Distance    = elements.Average(item => item.Distance),
					Placement   = elements.Average(item => item.Placement)
				};

			var query2 =
				from q in query
				join t in testeeTable on q.TesteeId equals t.Id into gj
				from t in gj.DefaultIfEmpty()
				select new { q.TesteeId };

			var result = query2.ToList();

			result.Count.ShouldBe(1);
		}
	}
}
