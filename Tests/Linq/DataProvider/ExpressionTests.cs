using System;
using System.Data.Common;
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
			using (var rd = conn.ExecuteReader("SELECT 1"))
			{
				if (rd.Reader!.Read())
				{
					var dp   = conn.DataProvider;
					var p    = Expression.Parameter(typeof(DbDataReader));
					var dr   = Expression.Convert(p, dp.DataReaderType);
					var ex   = (Expression<Func<DbDataReader,int,int>>)dp.GetReaderExpression(rd.Reader, 0, dr, typeof(int));
					var func = ex.CompileExpression();

					do
					{
						var value = func(rd.Reader, 0);
						Assert.That(value, Is.EqualTo(1));
					} while (rd.Reader!.Read());
				}
				else
					Assert.Fail();
			}
		}
	}
}
