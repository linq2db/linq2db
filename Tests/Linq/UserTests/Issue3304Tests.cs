using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3304Tests : TestBase
	{
		[Table]
		public class Table
		{
			[Column    ] public VersionClass Version   { get; set; } = null!;
			[Column    ] public DateTime?    UpdatedOn { get; set; }
			[PrimaryKey] public Guid         UserId    { get; set; }
			[Column    ] public Guid?        Value     { get; set; }

			public class VersionClass
			{
				public int Version { get; set; }
			}
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConverter<Table.VersionClass, int>(c => c?.Version ?? 0);
			ms.SetConverter<Table.VersionClass, DataParameter>(version => new DataParameter()
			{
				DataType = DataType.Int32,
				Value    = version?.Version ?? 0
			});
			ms.SetConverter<int, Table.VersionClass>(v => new Table.VersionClass() { Version = v });

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Table>();
			var data = new Table()
			{
				UserId = TestData.Guid1
			};

			var query = tb.Merge()
					.Using(new[] {data })
					.OnTargetKey()
					.InsertWhenNotMatched();

			query.Merge();
		}
	}
}
