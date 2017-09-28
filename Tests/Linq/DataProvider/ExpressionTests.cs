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
#if !NETSTANDARD1_6
	using System.Configuration;
#endif
	[TestFixture]
	public class ExpressionTests : TestBase
	{
		[Test, NorthwindDataContext(true)] // SQLite returns Int64 for column instead of Int32
		public void Test1(string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var dataProvider     = DataConnection.GetDataProvider(context);


			using (var conn = new DataConnection(dataProvider, connectionString))
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
