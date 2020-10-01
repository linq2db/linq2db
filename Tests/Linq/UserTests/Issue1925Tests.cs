using System.Data.Odbc;
using System.Data.OleDb;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1925Tests : TestBase
	{

		[Table]
		class SampleClass
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }
		}

		[Test]
		public void Issue1925Test([IncludeDataSources(TestProvName.AllAccess, TestProvName.AllSqlServer, ProviderName.Sybase)]  string context)
		{
			var data = new[]
			{
				new SampleClass() { Id = 1, Value = "6" }, 
				new SampleClass() { Id = 2, Value = "x[0-9]x" },
				new SampleClass() { Id = 3, Value = "x[0x" },
				new SampleClass() { Id = 4, Value = "x[]x" },
				new SampleClass() { Id = 5, Value = "x]" },
				new SampleClass() { Id = 6, Value = "]x" },
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var asParam = "[0-9]";
				var asParamUnterm = "[0";
				

				Assert.AreEqual(5, table.Where(r => r.Value!.EndsWith("]")).Select(r => r.Id).Single());
				Assert.AreEqual(6, table.Where(r => r.Value!.StartsWith("]")).Select(r => r.Id).Single());

				Assert.AreEqual(2, table.Where(r => r.Value!.Contains("-")).Select(r => r.Id).Single());

				Assert.AreEqual(1, table.Where(r => r.Value!.Contains("[]")).ToList().Count());

				Assert.AreEqual(2, table.Where(r => r.Value!.Contains("[0")).ToList().Count());
				
				Assert.AreEqual(2, table.Where(r => r.Value!.Contains(asParamUnterm)).ToList().Count());

				Assert.AreEqual(1, table.Where(r => r.Value!.Contains("[0-9]")).ToList().Count());

				Assert.AreEqual(1, table.Where(r => r.Value!.Contains("6")).ToList().Count());

				if (context == ProviderName.Access)
				{
					Assert.Throws<OleDbException>(() => table.Where(r => Sql.Like(r.Value, "[0")).ToList().Count());
					Assert.Throws<OleDbException>(() => table.Where(r => Sql.Like(r.Value, asParamUnterm)).ToList().Count());
				}
				else if (context == ProviderName.AccessOdbc)
				{
					Assert.Throws<OdbcException>(() => table.Where(r => Sql.Like(r.Value, "[0")).ToList().Count());
					Assert.Throws<OdbcException>(() => table.Where(r => Sql.Like(r.Value, asParamUnterm)).ToList().Count());
				}
				else
				{
					table.Where(r => Sql.Like(r.Value, "[0")).ToList().Count();
					table.Where(r => Sql.Like(r.Value, asParamUnterm)).ToList().Count();
				}

				Assert.AreEqual(1, table.Where(r => Sql.Like(r.Value, "[0-9]")).ToList().Count());

				Assert.AreEqual(1, table.Where(r => Sql.Like(r.Value, asParam)).ToList().Count());

			}
		}
		
	}
}
