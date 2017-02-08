using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue273Tests : TestBase
	{
		[Table("LinqDataTypes")]
		class ContainEnumTest
		{
			public enum TestFieldEnum
			{
				Value1,
				Value2
			}

			[PrimaryKey, Column("ID")]
			public int Id;
			[Column("BigIntValue")]
			public TestFieldEnum TestField;
		}

		[Test, IncludeDataContextSource(ProviderName.SQLite)]
		public void EnumInTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				var data = new[] { new { TestField = ContainEnumTest.TestFieldEnum.Value1 } };
				db.GetTable<ContainEnumTest>().Where(x => data.Contains(new { TestField = x.TestField })).ToList();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLite)]
		public void EnumInTest2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var data = new[] { ContainEnumTest.TestFieldEnum.Value1 };
				db.GetTable<ContainEnumTest>().Where(x => data.Contains(x.TestField)).ToList();
			}
		}
	}
}
