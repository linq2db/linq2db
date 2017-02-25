using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue357Tests : TestBase
	{
		[Table(Database="TestData", Name="AllTypes2")]
		class AllTypes2
		{
			[Column(DbType="int"), PrimaryKey, Identity]
			public int ID { get; set; }

			private DateTimeOffset? _dateTime;

			[Column("datetimeoffsetDataType", DbType="datetimeoffset(7)", Storage = "_dateTime"), Nullable]
			public DateTime? DateTime
			{
				get
				{
					return _dateTime == null ? (DateTime?)null : _dateTime.Value.DateTime;
				}
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var dt = db.GetTable<AllTypes2>().First(t => t.ID == 2);
				Assert.IsNotNull(dt);
				Assert.IsNotNull(dt.DateTime);
			}
		}
	}
}
