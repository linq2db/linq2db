using System;

using LinqToDB_Temp;
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
			public string Field2 = "";
		}

		[Test]
		public void Test1()
		{
			var t = new SqlTable(typeof(Table1));

			Assert.AreEqual(1, t.Fields.Length);
			Assert.AreEqual("VarChar(max)", t["Field1"].Type.ToString());
		}

		class Table2
		{
			public string Field1 = "";
			public string Field2 = "";
		}

		[Test]
		public void Test2()
		{
			var t = new SqlTable(typeof(Table2));

			Assert.AreEqual(2, t.Fields.Length);
			Assert.AreEqual("NVarChar(4000)", t["Field1"].Type.ToString());
		}
	}
}
