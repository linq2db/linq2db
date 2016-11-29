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

#pragma warning disable 0649
			private DateTimeOffset? _dateTime;
#pragma warning restore 0649

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
