using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
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
		public partial class Item
		{
			[Column("id", DataType = DataType.Int32, Precision = 32, Scale = 0), PrimaryKey, NotNull] public int Id { get; set; }
			[Column("value", DataType = DataType.Text), NotNull] public string? Value { get; set; }
		}

		[Test]
		public async Task WrongNameForDataParameter([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t2 = db.CreateLocalTable<Item>([new Item { Id = 1, Value = "One" }, new Item { Id = 2, Value = "Three" }]);

			var result = await db.GetTable<Item>()
						.Where(x => Sql.Expr<bool>("LOWER(\"value\") = LOWER(@p1)", new DataParameter("@p1", "ONE", DataType.NVarChar)))
						.ToArrayAsync();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(1);
			result[0].Value.ShouldBe("One");
		}
	}
}
