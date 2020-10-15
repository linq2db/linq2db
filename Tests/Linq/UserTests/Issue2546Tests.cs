using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2546Tests : TestBase
	{
		class Issue2546Class
		{
			[Column] [PrimaryKey] public int Id { get; set; }
			[Column] public string? Value { get; set; }
			public string? Value2 { get; set; }
		}

		[Test]
		public void TestContainerParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<Issue2546Class>()
				.Property(m => m.Value2)
				.HasSkipOnInsert()
				.HasSkipOnUpdate()
				.IsExpression(x => Sql.Property<object>(x, "Value"));

			var data = new[] { new Issue2546Class { Id = 1, Value = "Hello World" } };

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable(data))
			{
				var container = new Issue2546Class();
				container.Value2 = "Hello World";

				var something = (from x in db.GetTable<Issue2546Class>()
					where x.Value2 == container.Value2
					select x).ToList();

				Assert.That(something.Count, Is.EqualTo(1));
			}
		}
	}
}
