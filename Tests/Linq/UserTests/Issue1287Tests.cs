using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1287Tests : TestBase
	{
		[Table]
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		private class AllTypes
		{
			[Column]
			[Column("CHARDATATYPE", Configuration = ProviderName.DB2)]
			public char charDataType { get; set; }
		}

		[Table("AllTypes")]
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		private class AllTypesNullable
		{
			[Column]
			[Column("CHARDATATYPE", Configuration = ProviderName.DB2)]
			public char? charDataType { get; set; }
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void TestNullableChar(string context)
		{
			using (var db = GetDataContext(context))
			{
				var list = db.GetTable<AllTypesNullable>().Where(_ => _.charDataType == '1').ToList();

				Assert.AreEqual(1, list.Count);
				Assert.AreEqual('1', list[0].charDataType);
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void TestChar(string context)
		{
			using (var db = GetDataContext(context))
			{
				var list = db.GetTable<AllTypes>().Where(_ => _.charDataType == '1').ToList();

				Assert.AreEqual(1, list.Count);
				Assert.AreEqual('1', list[0].charDataType);
			}
		}
	}
}
