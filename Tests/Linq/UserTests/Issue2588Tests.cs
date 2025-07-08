using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2588Tests : TestBase
	{
		[Table]
		sealed class TestClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[ActiveIssue("Looks like ClickHouse processes query wrong", Configurations = [TestProvName.AllClickHouse])]
		[Test]
		public async Task AggregationWithNull([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TestClass>())
			{ 
				Assert.ThrowsAsync<InvalidOperationException>(() => db.GetTable<TestClass>()
					.Where(x => x.Id == 0)
					.Select(x => x.Value)
					.MaxAsync());

				var value1 = await db.GetTable<TestClass>()
					.Where(x => x.Id == 0)
					.Select(x => Sql.ToNullable(x.Value))
					.MaxAsync();

				Assert.That(value1, Is.Null);

				var value2 = await db.GetTable<TestClass>()
					.Where(x => x.Id == 0)
					.Select(x => (int?)x.Value)
					.DefaultIfEmpty(0)
					.MaxAsync();

				Assert.That(value2, Is.Zero);

				var value3 = await db.GetTable<TestClass>()
					.Where(x => x.Id == 0)
					.Select(x => x.Value)
					.DefaultIfEmpty(5)
					.MaxAsync();

				Assert.That(value3, Is.EqualTo(5));
			}
		}
	}
}
