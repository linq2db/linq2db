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
		sealed class SampleClass
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }
		}

		[Test]
		public void Issue1925Test([IncludeDataSources(TestProvName.AllAccess, TestProvName.AllSqlServer, ProviderName.Sybase, TestProvName.AllClickHouse)]  string context)
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

				Assert.Multiple(() =>
				{
					Assert.That(table.Where(r => r.Value!.EndsWith("]")).Select(r => r.Id).Single(), Is.EqualTo(5));
					Assert.That(table.Where(r => r.Value!.StartsWith("]")).Select(r => r.Id).Single(), Is.EqualTo(6));

					Assert.That(table.Where(r => r.Value!.Contains("-")).Select(r => r.Id).Single(), Is.EqualTo(2));

					Assert.That(table.Where(r => r.Value!.Contains("[]")).ToList(), Has.Count.EqualTo(1));

					Assert.That(table.Where(r => r.Value!.Contains("[0")).ToList(), Has.Count.EqualTo(2));

					Assert.That(table.Where(r => r.Value!.Contains(asParamUnterm)).ToList(), Has.Count.EqualTo(2));

					Assert.That(table.Where(r => r.Value!.Contains("[0-9]")).ToList(), Has.Count.EqualTo(1));

					Assert.That(table.Where(r => r.Value!.Contains("6")).ToList(), Has.Count.EqualTo(1));
				});

				if (context.IsAnyOf(TestProvName.AllAccessOleDb))
				{
#pragma warning disable CA1416 // windows-specific API
					Assert.Throws<OleDbException>(() => table.Where(r => Sql.Like(r.Value, "[0")).ToList());
					Assert.Throws<OleDbException>(() => table.Where(r => Sql.Like(r.Value, asParamUnterm)).ToList());
#pragma warning disable CA1416
				}
				else if (context.IsAnyOf(TestProvName.AllAccessOdbc))
				{
					Assert.Throws<OdbcException>(() => table.Where(r => Sql.Like(r.Value, "[0")).ToList());
					Assert.Throws<OdbcException>(() => table.Where(r => Sql.Like(r.Value, asParamUnterm)).ToList());
				}
				else
				{
					table.Where(r => Sql.Like(r.Value, "[0")).ToList();
					table.Where(r => Sql.Like(r.Value, asParamUnterm)).ToList();
				}

				var expected = context.IsAnyOf(TestProvName.AllClickHouse) ? 0 : 1;
				Assert.Multiple(() =>
				{
					Assert.That(table.Where(r => Sql.Like(r.Value, "[0-9]")).ToList(), Has.Count.EqualTo(expected));
					Assert.That(table.Where(r => Sql.Like(r.Value, asParam)).ToList(), Has.Count.EqualTo(expected));
				});
			}
		}
		
	}
}
