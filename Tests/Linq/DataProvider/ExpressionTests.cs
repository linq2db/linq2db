using System;
using System.Data;
using System.Linq.Expressions;

using NUnit.Framework;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.SQLite;

namespace Tests.DataProvider
{
#if !NETSTANDARD
	using System.Configuration;
#endif
	[TestFixture]
	public class ExpressionTests : TestBase
	{
		[Test, NorthwindDataContext]
		public void Test1(string context)
		{
#if !NETSTANDARD
			var connectionString = ConfigurationManager.ConnectionStrings[context].ConnectionString;
#else
			var connectionString = "TODO";
#endif

			IDataProvider provider;
			if (context == "NorthwindSqlite")
			{
				provider = new SQLiteDataProvider();
			}
			else
			{
				provider = SqlServerTools.GetDataProvider();
			}

			using (var conn = new DataConnection(provider, connectionString))
			{
				conn.InitCommand(CommandType.Text, "SELECT 1", null, null);

				var rd = conn.Command.ExecuteReader();

				if (rd.Read())
				{
					var dp   = conn.DataProvider;
					var p    = Expression.Parameter(typeof(IDataReader));
					var dr   = Expression.Convert(p, dp.DataReaderType);
					var ex   = (Expression<Func<IDataReader,int,int>>)dp.GetReaderExpression(conn.MappingSchema, rd, 0, dr, typeof(int));
					var func = ex.Compile();

					do
					{
						var value = func(rd, 0);
						Assert.AreEqual(1, value);
					} while (rd.Read());
				}
				else
				{
					Assert.Fail();
				}
			}
		}
	}
}
