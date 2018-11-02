using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1351Tests : TestBase
	{
		public class T1351Model
		{
			public int ID { get; set; }
			public sbyte TestField { get; set; }
			public sbyte? TestNullable { get; set; }
		}

		[ActiveIssue("CreateTable(sbyte) support missing", Configuration = ProviderName.DB2)]
		[Test, DataContextSource(false)]
		public void TestSByteQuery(string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<T1351Model>())
			{
				Assert.DoesNotThrow(() => table.Where(_ => _.TestField == 0).ToArray(), "Compare `sbyte`");
				Assert.DoesNotThrow(() => table.Where(_ => _.TestNullable != 1).ToArray(), "Compare `sbyte?` to non-null");
			}
		}
	}
}
