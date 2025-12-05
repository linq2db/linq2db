using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3089Tests : TestBase
	{
		[Table(Name ="TableTime")]
		public class TableTime
		{
			[Column, NotNull    ] public int       Id   { get; set; } // integer
			[Column(DataType = DataType.DateTime),    Nullable] public TimeSpan? Time { get; set; } // time without time zone
		}

		public class TableTimeResult
		{
			public TimeSpan? Time { get; set; }
		}

		[Test]
		public void TestUnion1([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TableTime>())
			{
				var query = (from x in table
						select new TableTimeResult {Time = x.Time})
					.Union(from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = null});

				query.ToArray();
			}
		}

		[Test]
		public void TestUnion2([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TableTime>())
			{
				var query = (from x in table
					select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = x.Time})
					.Union(from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
					select new TableTimeResult {Time = null});

				query.ToArray();
			}
		}

		[ActiveIssue(3360, Configuration = TestProvName.AllPostgreSQL)]
		[Test]
		public void TestUnion3([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TableTime>())
			{
				var query = (from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = x.Time})
					.Union(from x in table
						select new TableTimeResult {Time = null});

				query.ToArray();
			}
		}

		[ActiveIssue(3360, Configuration = TestProvName.AllPostgreSQL)]
		[Test]
		public void TestUnion4([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TableTime>())
			{
				var query = (from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = null})
					.Union(from x in table
						select new TableTimeResult {Time = x.Time});

				query.ToArray();
			}
		}

	}
}
