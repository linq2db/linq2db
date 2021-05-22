using LinqToDB;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.AST
{
	[TestFixture]
	public class SqlDataTypeTests : TestBase
	{

		[Test]
		public void TestSystemType()
		{
			Assert.AreEqual(typeof(bool), SqlDataType.GetDataType(DataType.Boolean).SystemType);
		}
	}
}
