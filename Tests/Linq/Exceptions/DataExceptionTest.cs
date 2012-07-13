using System;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.DataProvider;

using NUnit.Framework;

namespace Tests.Exceptions
{
	using Model;

	[TestFixture]
	public class DataExceptionTest
	{
		[Test]
		public void ParameterPrefixTest()
		{
			try
			{
				using (var db = new DbManager(new MySqlDataProvider(), "Server=DBHost;Port=3306;Database=nodatabase;Uid=bltoolkit;Pwd=TestPassword;"))
				{
					db.GetTable<Person>().ToList();
				}
			}
			catch (DataException ex)
			{
				var number = ex.Number;
			}
		}
	}
}
