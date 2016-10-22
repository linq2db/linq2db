using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

using NUnit.Framework;


namespace Tests.NetCore
{
	[TestFixture]
    public class SQLiteTests
    {
		/// <summary>
		/// https://github.com/aspnet/Microsoft.Data.Sqlite/issues/296
		/// </summary>
		[Test]
		public void CharTest1()
		{
			using (var connection = new SqliteConnection(@"Data Source=Database\TestData.sqlite"))
			{
				connection.Open();
				var command = connection.CreateCommand();

				command.CommandText = "SELECT Cast(@p as char)";
				var p = command.CreateParameter();
				p.Value = '1';
				p.ParameterName = "@p";

				command.Parameters.Add(p);

				var r = command.ExecuteReader();
				r.Read();

				var value = r.GetValue(0);
				Assert.AreNotEqual("49", value, $"{value.GetType().FullName}");
			}
		}

		/// <summary>
		/// https://github.com/aspnet/Microsoft.Data.Sqlite/issues/296
		/// </summary>
		[Test]
		public void CharTest2()
		{
			using (var connection = new SqliteConnection(@"Data Source=Database\TestData.sqlite"))
			{
				connection.Open();
				var command = connection.CreateCommand();

				command.CommandText = "SELECT @p";
				var p = command.CreateParameter();
				p.Value = '1';
				p.ParameterName = "@p";

				command.Parameters.Add(p);

				var r = command.ExecuteReader();
				r.Read();

				var value = r.GetValue(0);
				Assert.AreNotEqual((long)49, value, $"{value.GetType().FullName}");
			}
		}

	}
}
