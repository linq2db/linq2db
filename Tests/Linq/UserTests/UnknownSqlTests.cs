using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using Model;

	[TestFixture]
	public class UnknownSqlTests : TestBase
	{
		enum ColumnDataType
		{
			Unknown = 0,
			Text    = 1,
		}

		class CustomTableColumn
		{
			public int? DataTypeID { get; set; }
		}

		[Test]
		public void Test()
		{
			using (var db = new TestDataConnection())
			{
				var q = db.GetTable<CustomTableColumn>()
					.Select(
						x => new
						{
							DataType = Sql.AsSql(ColumnDataType.Unknown),
						});

				var sql = q.ToString();

				Assert.That(sql, Is.Not.Contains("Unknown"));
			}
		}
	}
}
