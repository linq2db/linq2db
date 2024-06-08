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
			Assert.That(SqlDataType.GetDataType(DataType.Boolean).SystemType, Is.EqualTo(typeof(bool)));
		}
	}
}
