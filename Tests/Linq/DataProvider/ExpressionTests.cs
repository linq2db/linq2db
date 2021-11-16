using System;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Common;
using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class ExpressionTests : TestBase
	{
		[Test] // SQLite returns Int64 for column instead of Int32
		public void Test1([NorthwindDataContext(true)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var dataProvider     = DataConnection.GetDataProvider(context);


			using (var conn = new DataConnection(dataProvider, connectionString))
			{
				conn.InitCommand(CommandType.Text, "SELECT 1", null, null, false);

				var rd = conn.Command.ExecuteReader();

				if (rd.Read())
				{
					var dp   = conn.DataProvider;
					var p    = Expression.Parameter(typeof(IDataReader));
					var dr   = Expression.Convert(p, dp.DataReaderType);
					var ex   = (Expression<Func<IDataReader,int,int>>)dp.GetReaderExpression(rd, 0, dr, typeof(int));
					var func = ex.CompileExpression();

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
