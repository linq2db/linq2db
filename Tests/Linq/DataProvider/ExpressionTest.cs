using System;
using System.Data;
using System.Linq.Expressions;

using NUnit.Framework;

using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace Tests.DataProvider
{
	[TestFixture]
	public class ExpressionTest : TestBase
	{
		[Test]
		public void Test1([IncludeDataContexts("Northwind")] string context)
		{
			using (var conn = new DataConnection(new SqlServerDataProvider(), "Server=.;Database=Northwind;Integrated Security=SSPI"))
			{
				conn.Command.CommandText = "SELECT 1";

				var rd = conn.Command.ExecuteReader();

				if (rd.Read())
				{
					var dp = conn.DataProvider;
					var p  = Expression.Parameter(typeof(IDataReader));
					var dr = dp.ConvertDataReader(p);

					var ex = dp.GetReaderExpression(conn.MappingSchema, rd, 0, dr, typeof(int));

					var expr = Expression.Lambda<Func<IDataReader,int>>(ex, p);
					var func = expr.Compile();

					do
					{
						var value = func(rd);
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
