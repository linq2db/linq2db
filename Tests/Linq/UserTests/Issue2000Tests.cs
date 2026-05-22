using System.Text.Json;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2000Tests : TestBase
	{
		public interface ITest1
		{
		}

		public class Test1 : ITest1
		{
			public int A;
		}

		[Table(Name = "TestTable")]
		public class TestTable
		{
			[Column(), NotNull] public int Id { get; set; }

			[Column(DataType = DataType.VarChar), NotNull] public ITest1 F { get; set; } = null!;
		}

		[Test]
		public void TestMappingToInterface([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConverter<ITest1, string>(favs => JsonSerializer.Serialize(favs));
			ms.SetConverter<ITest1, DataParameter>(obj =>
				new DataParameter { Value = JsonSerializer.Serialize(obj), DataType = DataType.NVarChar });
			ms.SetConverter<string, ITest1>(favs => { return JsonSerializer.Deserialize<ITest1>(favs)!; });

			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable<TestTable>();
			table
				.Value(x => x.Id, 2)
				.Value(x => x.F, new Test1() { A = 5 })
				.Insert();
		}
	}
}
