using System;

using LinqToDB_Temp.SqlBuilder;

using NUnit.Framework;

namespace Tests.SqlBuilder
{
	[TestFixture]
	public class SqlDataTypeTest
	{
		[Test]
		public void FromString()
		{
			Assert.IsInstanceOf<SqlDataType.Int32> (SqlDataType.FromString("int"));
			Assert.IsInstanceOf<SqlDataType.Double>(SqlDataType.FromString("double  precision (50)"));
			Assert.IsInstanceOf<SqlDataType.Xml>   (SqlDataType.FromString("xml(DOCUMENT 123)"));

			var type = SqlDataType.FromString("Decimal (30, 10)");

			Assert.IsInstanceOf<SqlDataType.Decimal>(type);
			Assert.AreEqual(30, ((SqlDataType.Decimal)type).Precision);
			Assert.AreEqual(10, ((SqlDataType.Decimal)type).Scale);
		}
	}
}
