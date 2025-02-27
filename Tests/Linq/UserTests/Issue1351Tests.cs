﻿using System.Linq;

using NUnit.Framework;

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

		[Test]
		public void TestSByteQuery([DataSources(false)] string context)
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
