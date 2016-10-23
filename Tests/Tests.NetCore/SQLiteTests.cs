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

		[Test]
		public void UInt32Test()
		{
			using (var connection = new SqliteConnection(@"Data Source=Database\TestData.sqlite"))
			{
				connection.Open();
				var command = connection.CreateCommand();

				command.CommandText = "SELECT @p";
				var p = command.CreateParameter();
				p.Value = UInt32.MaxValue;
				p.DbType = System.Data.DbType.UInt32;
				p.ParameterName = "@p";

				command.Parameters.Add(p);

				var r = command.ExecuteReader();
				r.Read();

				var value = r.GetValue(0);
				Assert.AreEqual(UInt32.MaxValue, value, $"{value.GetType().FullName} {value}");
			}
		}

		/// <summary>
		/// https://github.com/aspnet/Microsoft.Data.Sqlite/issues/298
		/// </summary>
		[Test]
		public void UInt64Test()
		{
			using (var connection = new SqliteConnection(@"Data Source=Database\TestData.sqlite"))
			{
				connection.Open();
				var command = connection.CreateCommand();

				command.CommandText = "SELECT @p";
				var p = command.CreateParameter();
				p.Value = UInt64.MaxValue;
				p.DbType = System.Data.DbType.UInt64;
				p.ParameterName = "@p";

				command.Parameters.Add(p);

				var r = command.ExecuteReader();
				r.Read();

				var value = r.GetValue(0);
				Assert.AreEqual(UInt64.MaxValue, value, $"{value.GetType().FullName} {value}");
			}
		}

		/// <summary>
		/// https://github.com/aspnet/Microsoft.Data.Sqlite/issues/300
		/// </summary>
		[Test]
	    public void DecimalTest()
	    {
			using (var connection = new SqliteConnection(@"Data Source=Database\TestData.sqlite"))
			{
				connection.Open();
				var command = connection.CreateCommand();

				command.CommandText = "SELECT MoneyValue FROM LinqDataTypes";

				var r = command.ExecuteReader();
				while (r.Read())
				{
					var value = r.GetValue(0);
					Console.WriteLine($"{value.GetType().FullName} {value}, {r.GetFieldType(0)}");
					Assert.AreNotEqual(typeof(long), r.GetFieldType(0), $"{value.GetType().FullName} {value}");
				}
			}
		}
	}
}
