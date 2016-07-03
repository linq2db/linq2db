using System;
using System.Data;
using System.Linq.Expressions;

using NUnit.Framework;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

namespace Tests.DataProvider
{
	using System.Configuration;

	[TestFixture]
	public class ExpressionTests : TestBase
	{
		[Test, NorthwindDataContext]
		public void Test1(string context)
		{
			var connectionString = ConfigurationManager.ConnectionStrings["Northwind"].ConnectionString;

			using (var conn = new DataConnection(SqlServerTools.GetDataProvider(), connectionString))
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
