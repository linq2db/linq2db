using System;
using System.Data.SqlTypes;

using LinqToDB_Temp.SqlBuilder;

using NUnit.Framework;

namespace Tests.SqlBuilder
{
	public class TypesTest
	{
		[Test]
		public void SqlNullableTest()
		{
			var p = new SqlExpr.Parameter(new SqlDataType.Int32(typeof(SqlInt32)), "p");
			Assert.IsTrue(p.CanBeNull);

			p = new SqlExpr.Parameter(new SqlDataType.Int32(typeof(Int32)), "p");
			Assert.IsFalse(p.CanBeNull);
		}
	}
}
