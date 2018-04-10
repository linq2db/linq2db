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

		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var data = new[] { new { TestField = ContainEnumTest.TestFieldEnum.Value1 }, new { TestField = ContainEnumTest.TestFieldEnum.Value2 } };
				db.GetTable<ContainEnumTest>().Where(x => data.Contains(new { TestField = x.TestField })).ToList();
			}
		}

		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var data = new[] { new {  TestField = ContainEnumTest.TestFieldEnum.Value1, Field = 10 }, new { TestField = ContainEnumTest.TestFieldEnum.Value2, Field = 10 } };
				db.GetTable<ContainEnumTest>().Where(x => data.Contains(new { TestField = x.TestField, Field = x.Id })).ToList();
			}
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var data = new[] { ContainEnumTest.TestFieldEnum.Value1 };
				db.GetTable<ContainEnumTest>().Where(x => data.Contains(x.TestField)).ToList();
			}
		}
	}
}
