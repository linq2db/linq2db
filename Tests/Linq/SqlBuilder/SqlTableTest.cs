using System;

using LinqToDB_Temp;
using LinqToDB_Temp.Mapping;
using LinqToDB_Temp.SqlBuilder;

using NUnit.Framework;

namespace Tests.SqlBuilder
{
	[TestFixture]
	public class SqlTableTest
	{
		[Table]
		class Table1
		{
			[Column(DbType = "varchar(max)")] public string Field1 = "";
		}

		[Test]
		public void Test1()
		{
			var t = new SqlTable(MappingSchema.Default, typeof(Table1));

			Assert.AreEqual("VarChar(max)", t["Field1"].Type.ToString());
		}
	}
}
