using System;
using System.Linq;

using MySql.Data.MySqlClient;

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
				using (var db = LinqToDB.DataProvider.MySql.MySqlFactory.CreateDataConnection(
					"Server=DBHost;Port=3306;Database=nodatabase;Uid=bltoolkit;Pwd=TestPassword;"))
				{
					db.GetTable<Person>().ToList();
				}
			}
			catch (MySqlException ex)
			{
				var number = ex.Number;
			}
		}
	}
}
