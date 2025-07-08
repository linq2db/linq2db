using System.Collections.Generic;
using System.Linq;

using Shouldly;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3014Tests : TestBase
	{
		[Table(Name = "Table2_3014")]
		public partial class Table2
		{
			[Column, NotNull    ] public int   Id       { get; set; } // integer
			[Column, NotNull    ] public int   ParentId { get; set; } // integer
			[Column,    Nullable] public bool? IsTrue   { get; set; } // boolean
		}

		[Table(Name = "TableStatus_3014")]
		public partial class TableStatus
		{
			[PrimaryKey, NotNull] public int    StatusId   { get; set; } // integer
			[Column,     NotNull] public string StatusName { get; set; } = null!; // character varying
		}
		public class TestClass
		{
			public int       Id         { get; set; }
			public int       Status     { get; set; }
			public string    Text       { get; set; } = null!;
			public List<int> StatusData { get; set; } = null!;
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Table2>(new []{new Table2{Id = 1, IsTrue = true}}))
			using (db.CreateLocalTable<TableStatus>(new []{new TableStatus{StatusId = 1, StatusName = "Sample"}}))
			{
				var query = from t in db.GetTable<Table2>()
					select new TestClass
					{
						Id         = t.Id,
						Status     = 1,
						Text       = "0",
						StatusData = db.GetTable<TableStatus>().Select(x => 1).ToList(),
					};
				query.ToList().Count().ShouldBe(1);
				query.Where(x => x.Id == 0).ShouldBeEmpty();
				query.Where(x => x.Status == 0).ShouldBeEmpty();
				query.Where(e => e.Text == "0").ToList().Count().ShouldBe(1);

			}
		}
	}
}
