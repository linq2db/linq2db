using System;
using System.Data;
using System.Linq.Expressions;

using NUnit.Framework;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

namespace Tests.DataProvider
{
	[TestFixture]
	public class ExpressionTest : TestBase
	{
		[Test]
		public void Test1([IncludeDataContexts("Northwind")] string context)
		{
			using (var conn = new DataConnection(SqlServerFactory.GetDataProvider(), "Server=.;Database=Northwind;Integrated Security=SSPI"))
			{
				conn.SetCommand("SELECT 1");

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
