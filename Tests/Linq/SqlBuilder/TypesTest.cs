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
			var p = new SqlExpr.Parameter(new SqlDataType.Int32(typeof(SqlInt32)), "p", new SqlValue.Object(new SqlInt32(1)));
			Assert.IsTrue(p.CanBeNull);

			p = new SqlExpr.Parameter(new SqlDataType.Int32(typeof(Int32)), "p", new SqlValue.Int32(1));
			Assert.IsFalse(p.CanBeNull);
		}
	}
}
