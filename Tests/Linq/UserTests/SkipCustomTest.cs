using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class SkipCustomTest : TestBase
	{
		public class SkipCustomAttribute : SkipBaseAttribute
		{
			public SkipCustomAttribute()
			{
				Affects = SkipModification.Insert;
			}

			public override bool ShouldSkip(object? obj, EntityDescriptor entityDescriptor, ColumnDescriptor columnDescriptor)
			{
				if (obj != null)
				{
					var value = columnDescriptor.GetProviderValue(obj);
					if (value is int i)
					{
						return i % 2 == 0;
					}
				}

				return false;
			}

			public override SkipModification Affects { get; }
		}

		[Table("PR_1598_SkipCustom_Table")]
		public class TestTable
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name")]
			public string? Name { get; set; }
			[Column("Age"), SkipCustom()]
			public int? Age { get; set; }
		}

		[Test]
		public void TestSkip([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() { Id = 1, Name = "John", Age = 15 });

					if (context.SupportsRowcount())
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.EqualTo(15));

					count = db.Insert(new TestTable() { Id = 2, Name = "Max", Age = 14 });

					if (context.SupportsRowcount())
						Assert.That(count, Is.GreaterThan(0));

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2)!;

					Assert.That(r, Is.Not.Null);
					Assert.That(r.Age, Is.Null);
				}
			}
		}
	}
}
