﻿using System.Linq;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue228Tests : TestBase
	{
		[Test, DataContextSource(false)]
		public void Test(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var cnt = db.DataProvider.SqlProviderFlags.MaxInListValuesCount;

				try
				{
					db.DataProvider.SqlProviderFlags.MaxInListValuesCount = 1;

					var ids = new[] {1, 2};
					AreEqual(Types.Where(_ => !ids.Contains(_.ID)), db.Types.Where(_ => !ids.Contains(_.ID)));

				}
				finally
				{
					db.DataProvider.SqlProviderFlags.MaxInListValuesCount = cnt;
				}
			}
		}
	}
}
