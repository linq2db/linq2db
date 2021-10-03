using System.Linq;
using MySqlConnector;
using NUnit.Framework;

namespace Tests.Exceptions
{
	using Model;

	[TestFixture]
	public class DataExceptionTests : TestBase
	{
		// TODO: what we even test here? remove?
		[Test]
		public void ParameterPrefixTest([IncludeDataSources(TestProvName.AllMySql)] string context)
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
