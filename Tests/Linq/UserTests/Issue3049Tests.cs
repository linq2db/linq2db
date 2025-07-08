using System.Collections.Generic;
using System.Linq;

using Shouldly;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3049Tests : TestBase
	{
		sealed class AdminContext : DataContext
		{
			public AdminContext(string configuration) : base(configuration)
			{

			}

			public  Dictionary<string, object> Nesto { get; } = new();
		}

		[Table]
		sealed class SampleClass
		{
			[Column]              public int     Id    { get; set; }
			[Column(Length = 50)] public string? Value { get; set; }
		}

		[Test]
		public void TestContextProp([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values("key1", "key2")] string currentKey)
		{
			using (var db = new AdminContext(context))
			using (var table = db.CreateLocalTable(new SampleClass[]
			{
				new (){Value = "key1"},
				new (){Value = "key2"},
			}))
			{
				db.Nesto.Add(currentKey, "fake");

				AssertQuery(table.Where(t => db.Nesto.ContainsKey(t.Value!)).Select(t => t.Value));
			}
		}
	}
}
