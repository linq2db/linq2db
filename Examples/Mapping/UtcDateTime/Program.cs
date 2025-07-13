using System.Diagnostics;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;

namespace UtcDateTime
{
	internal class Program
	{
		static readonly DataOptions _options = new DataOptions()
			.UseSQLite("Data Source=db.sqlite", SQLiteProvider.Microsoft)
			.UseTracing(TraceLevel.Info, info => Console.WriteLine(info.SqlText));

		// Use specific MappingSchema for DateTime conversion or set MappingSchema.Default.
		//
		static readonly MappingSchema _utcMappingSchema = new MappingSchema("UtcConvert")
			// Converts DateTime to UTC when reading from DB.
			//
			.SetConvertExpression<DateTime,DateTime>(dt => dt.ToLocalTime(),     conversionType: ConversionType.FromDatabase)
			// Converts DateTime to UTC when writing to DB.
			//
			.SetConvertExpression<DateTime,DateTime>(dt => dt.ToUniversalTime(), conversionType: ConversionType.ToDatabase)
			;

		static void Main()
		{
			{
				using var db = new DataConnection(_options);

				db.AddMappingSchema(_utcMappingSchema);

				db.DropTable<Table>(tableOptions: TableOptions.CheckExistence);

				var t = db.CreateTable<Table>(tableOptions: TableOptions.CheckExistence);

				// Test different scenarios.
				//

				db.Insert(new Table { Date = DateTime.Now });

				t
					.Value(t => t.Date, DateTime.Now)
					.Insert();

				t.BulkCopy([new Table { Date = DateTime.Now }]);

				db.InlineParameters = true;

				db.Insert(new Table { Date = DateTime.Now });

				t
					.Value(t => t.Date, DateTime.Now)
					.Insert();

				t.BulkCopy([new Table { Date = DateTime.Now }]);

				// Check that DateTime is read as local time.
				//
				foreach (var r in db.GetTable<Table>())
				{
					Console.WriteLine(r.Date);
				}
			}

			{
				// Check that DateTime is converted to UTC when reading from DB.
				// Do not use 'UtcConvert' MappingSchema.
				//
				using var db = new DataConnection(_options);

				foreach (var r in db.GetTable<Table>())
				{
					Console.WriteLine(r.Date);
				}
			}
		}

		public class Table
		{
			[Column, DataType(DataType.DateTime)]
			public DateTime Date { get; set; }
		}
	}
}
