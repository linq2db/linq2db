using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2588Tests : TestBase
	{
		[Table]
		class TestClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public async Task AggregationWithNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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

				var value2 = await db.GetTable<TestClass>()
					.Where(x => x.Id == 0)
					.Select(x => x.Value)
					.DefaultIfEmpty()
					.MaxAsync();
			}
		}
	}
}
