#if NET6_0_OR_GREATER

using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class UnionByMethodTests : TestBase
	{
		[Table]
		public class UnionByTable
		{
			[Column] public int    Id     { get; set; }
			[Column] public int    Key    { get; set; }
			[Column] public string Value  { get; set; } = null!;
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void UnionByBasic([DataSources] string context)
		{
			var left = new[]
			{
				new UnionByTable { Id = 1, Key = 10, Value = "a" },
				new UnionByTable { Id = 2, Key = 20, Value = "b" },
				new UnionByTable { Id = 3, Key = 30, Value = "c" },
			};

			var right = new[]
			{
				new UnionByTable { Id = 4, Key = 20, Value = "d" },
				new UnionByTable { Id = 5, Key = 30, Value = "e" },
				new UnionByTable { Id = 6, Key = 40, Value = "f" },
			};

			using var db         = GetDataContext(context);
			using var leftTable  = db.CreateLocalTable("UnionByLeft",  left);
			using var rightTable = db.CreateLocalTable("UnionByRight", right);

			var query = leftTable
				.UnionBy(rightTable, x => x.Key)
				.OrderBy(x => x.Key);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void UnionByWithFilter([DataSources] string context)
		{
			var left = new[]
			{
				new UnionByTable { Id = 1, Key = 10, Value = "a" },
				new UnionByTable { Id = 2, Key = 20, Value = "b" },
				new UnionByTable { Id = 3, Key = 30, Value = "c" },
			};

			var right = new[]
			{
				new UnionByTable { Id = 4, Key = 20, Value = "d" },
				new UnionByTable { Id = 5, Key = 30, Value = "e" },
				new UnionByTable { Id = 6, Key = 40, Value = "f" },
			};

			using var db         = GetDataContext(context);
			using var leftTable  = db.CreateLocalTable("UnionByLeft",  left);
			using var rightTable = db.CreateLocalTable("UnionByRight", right);

			var query = leftTable
				.UnionBy(rightTable.Where(x => x.Key >= 30), x => x.Key)
				.OrderBy(x => x.Key);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void UnionBySameTable([DataSources] string context)
		{
			var data = new[]
			{
				new UnionByTable { Id = 1, Key = 10, Value = "a" },
				new UnionByTable { Id = 2, Key = 10, Value = "b" },
				new UnionByTable { Id = 3, Key = 20, Value = "c" },
				new UnionByTable { Id = 4, Key = 20, Value = "d" },
				new UnionByTable { Id = 5, Key = 30, Value = "e" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = table
				.OrderBy(x => x.Id)
				.UnionBy(table, x => x.Key)
				.OrderBy(x => x.Id);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted]
		public void UnionByWithComparerShouldFail([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var left = new[]
			{
				new UnionByTable { Id = 1, Key = 10, Value = "a" },
				new UnionByTable { Id = 2, Key = 20, Value = "b" },
			};

			var right = new[]
			{
				new UnionByTable { Id = 3, Key = 20, Value = "c" },
				new UnionByTable { Id = 4, Key = 30, Value = "d" },
			};

			using var db         = GetDataContext(context);
			using var leftTable  = db.CreateLocalTable("UnionByLeft",  left);
			using var rightTable = db.CreateLocalTable("UnionByRight", right);

			var comparer = EqualityComparer<int>.Default;

			_= leftTable
				.UnionBy(rightTable, x => x.Key, comparer)
				.OrderBy(x => x.Key)
				.ToList();
		}
	}
}

#endif
