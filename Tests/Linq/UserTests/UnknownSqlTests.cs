using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
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
			[PrimaryKey] public int  Id;

			public int? DataTypeID { get; set; }
		}

		[Test]
		public void Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CustomTableColumn>();

			var q = db.GetTable<CustomTableColumn>()
					.Select(
						x => new
						{
							DataType = Sql.AsSql(ColumnDataType.Unknown),
						});

			var sql = q.ToSqlQuery().Sql;

			Assert.That(sql, Is.Not.Contains("Unknown"));

			q.ToArray();
		}
	}
}
