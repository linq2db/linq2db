using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5371Tests : TestBase
	{
		[Table]
		sealed class Item
		{
			[Column("id"), PrimaryKey] public int    Id    { get; set; }
			[Column("value")]          public string Value { get; set; } = null!;
		}

		[Test]
		public void DataParameterNamePreserved([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new Item { Id = 1, Value = "ONE" },
				new Item { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>("LOWER(\"value\") = LOWER({0})", new DataParameter("@p1", "ONE", DataType.NVarChar)))
				.ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(1);
		}

		[Test]
		public void DataParameterNamePreservedMultiple([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new Item { Id = 1, Value = "ONE" },
				new Item { Id = 2, Value = "two" },
				new Item { Id = 3, Value = "three" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("@p1", "ONE", DataType.NVarChar)})") ||
				             Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("@p2", "two", DataType.NVarChar)})"))
				.OrderBy(x => x.Id)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Id.ShouldBe(1);
			result[1].Id.ShouldBe(2);
		}

		[Test]
		public void DataParameterNameInSqlStatement([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new Item { Id = 1, Value = "ONE" },
				new Item { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var query = table
				.Where(x => Sql.Expr<bool>($"UPPER({Sql.FieldExpr(x.Value)}) = UPPER({new DataParameter("@p1", "ONE", DataType.NVarChar)})"));

			var parameters = query.GetStatement().CollectParameters();

			parameters.Length.ShouldBe(1);
			parameters[0].Name.ShouldBe("@p1");
		}

		[Test]
		public void DataParameterNullName([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new Item { Id = 1, Value = "ONE" },
				new Item { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter(null, "ONE", DataType.NVarChar)})"))
				.ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(1);
		}

		[Test]
		public void DataParameterEmptyName([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new Item { Id = 1, Value = "ONE" },
				new Item { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("", "ONE", DataType.NVarChar)})"))
				.ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(1);
		}

		[Test]
		public void DataParameterNullNameAndExplicitNameMixed([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new Item { Id = 1, Value = "ONE" },
				new Item { Id = 2, Value = "two" },
				new Item { Id = 3, Value = "three" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("@p1", "ONE", DataType.NVarChar)})") ||
				             Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter(null, "two", DataType.NVarChar)})"))
				.OrderBy(x => x.Id)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Id.ShouldBe(1);
			result[1].Id.ShouldBe(2);
		}
	}
}
