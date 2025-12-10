using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2052Tests : TestBase
	{
		[Table("Entity", IsColumnAttributeRequired = true)]
		public class Entity
		{
			private DataPack _Data;

			public ref DataPack Data => ref _Data;

			[Column]
			public string? Str;
		}

		public struct DataPack
		{
		}

		[Test]
		public void TestRefTypeDoNotThrow([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Entity>();
			var result = table.ToArray();
		}
	}
}
