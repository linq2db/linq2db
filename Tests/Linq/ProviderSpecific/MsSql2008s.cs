using System;

using NUnit.Framework;

namespace Data.Linq.ProviderSpecific
{
	[TestFixture]
	public class MsSql2008 : TestBase
	{
		[Test]
		public void SqlTest()
		{
			using (var db = new TestDbManager("Sql2008"))
			using (var rd = db.SetCommand(@"
				SELECT
					DateAdd(Hour, 1, [t].[DateTimeValue]) - [t].[DateTimeValue]
				FROM
					[LinqDataTypes] [t]")
				.ExecuteReader())
			{
				if (rd.Read())
				{
					var value = rd.GetValue(0);
				}
			}
		}
	}
}
