using System;
using System.Linq;

using LinqToDB;

using MySql.Data.MySqlClient;

using NUnit.Framework;

namespace Tests.Exceptions
{
	using Model;

	[TestFixture]
	public class DataExceptionTest : TestBase
	{
		[Test, IncludeDataContextSource(ProviderName.MySql)]
		public void ParameterPrefixTest(string context)
		{
			try
			{
				using (var db = LinqToDB.DataProvider.MySql.MySqlTools.CreateDataConnection(
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
