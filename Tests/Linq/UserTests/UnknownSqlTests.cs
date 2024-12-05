using System.Linq;

using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Data;

	[TestFixture]
	public class UnknownSqlTests : TestBase
	{
		enum ColumnDataType
		{
			Unknown = 0,
			Text    = 1,
		}

		sealed class CustomTableColumn
		{
			public int? DataTypeID { get; set; }
		}

		[Test]
		public void Test()
		{
			using (var db = new DataConnection())
			{
				var q = db.GetTable<CustomTableColumn>()
					.Select(
						x => new
						{
							DataType = Sql.AsSql(ColumnDataType.Unknown),
						});

				var sql = q.ToSqlQuery().Sql;

				Assert.That(sql, Is.Not.Contains("Unknown"));
			}
		}
	}
}
