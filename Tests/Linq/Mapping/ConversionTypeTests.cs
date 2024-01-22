using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.Mapping
{
	public class ConversionTypeTests : TestBase
	{
		static readonly MappingSchema _trimMappingSchema = new MappingSchema("Trim *")
			.SetConvertExpression<string,string>(s => s.Trim('*'),  conversionType: ConversionType.FromDatabase)
			.SetConvertExpression<string,string>(s => $"***{s}***", conversionType: ConversionType.ToDatabase)
			;

		class TrimTestTable
		{
			public int     ID   { get; set; }
			[Column(Length = 50)]
			public string? Data { get; set; }
		}

		[Test]
		public void InsertTest([DataSources] string context, [Values] bool inlineParameters)
		{
			using var db = GetDataContext(context, _trimMappingSchema);
			using var t  = db.CreateLocalTable<TrimTestTable>();

			db.InlineParameters = inlineParameters;

			db.Insert(new TrimTestTable { ID = 1, Data = "OOO" });

			Debug.WriteLine(t.ToDiagnosticString());

			AreEqual(
				[ new { ID = 1, Data = "OOO"} ],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

			Debug.WriteLine(db1.GetTable<TrimTestTable>().ToDiagnosticString());

			AreEqual(
				[ new { ID = 1, Data = "***OOO***"} ],
				db1.GetTable<TrimTestTable>().OrderBy(_ => _.ID).Select(r => new { r.ID, r.Data}));
		}

		[Test]
		public void InsertTest2([DataSources] string context, [Values] bool inlineParameters)
		{
			using var db = GetDataContext(context, _trimMappingSchema);
			using var t  = db.CreateLocalTable<TrimTestTable>();

			db.InlineParameters = inlineParameters;

			t
				.Value(t => t.ID,   1)
				.Value(t => t.Data, "OOO")
				.Insert();

			Debug.WriteLine(t.ToDiagnosticString());

			AreEqual(
				[ new { ID = 1, Data = "OOO"} ],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

			Debug.WriteLine(db1.GetTable<TrimTestTable>().ToDiagnosticString());

			AreEqual(
				[ new { ID = 1, Data = "***OOO***"} ],
				db1.GetTable<TrimTestTable>().OrderBy(_ => _.ID).Select(r => new { r.ID, r.Data}));
		}

		[Test]
		public void BulkCopyTest([DataSources(false, TestProvName.AllMySqlConnector)] string context, [Values] bool inlineParameters, [Values] BulkCopyType bulkCopyType)
		{
			using var db = GetDataContext(context, o => o.UseMappingSchema(_trimMappingSchema).UseBulkCopyType(bulkCopyType));
			using var t  = db.CreateLocalTable<TrimTestTable>();

			db.InlineParameters = inlineParameters;

			t.BulkCopy([new TrimTestTable { ID = 1, Data = "OOO" }]);

			Debug.WriteLine(t.ToDiagnosticString());

			AreEqual(
				[ new { ID = 1, Data = "OOO"} ],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

			Debug.WriteLine(db1.GetTable<TrimTestTable>().ToDiagnosticString());

			AreEqual(
				[ new { ID = 1, Data = "***OOO***"} ],
				db1.GetTable<TrimTestTable>().OrderBy(_ => _.ID).Select(r => new { r.ID, r.Data}));
		}

		[Test]
		public void UpdateTest([DataSources] string context, [Values] bool inlineParameters)
		{
			using var db = GetDataContext(context, _trimMappingSchema);

			db.InlineParameters = inlineParameters;

			using var t  = db.CreateLocalTable<TrimTestTable>(
				[
					new TrimTestTable { ID = 1, Data = "XXX" },
					new TrimTestTable { ID = 2, Data = "HHH" },
				]);

			t
				.Where(t => t.Data == "XXX")
				.Set(t => t.Data, "OOO")
				.Update();

			var p = "HHH";

			t
				.Where(t => t.Data == p)
				.Set(t => t.Data, "SSS")
				.Update();

			Debug.WriteLine(t.ToDiagnosticString());

			AreEqual(
				[
					new { ID = 1, Data = "OOO"},
					new { ID = 2, Data = "SSS"},
				],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

			Debug.WriteLine(db1.GetTable<TrimTestTable>().ToDiagnosticString());

			AreEqual(
				[
					new { ID = 1, Data = "***OOO***"},
					new { ID = 2, Data = "***SSS***"},
				],
				db1.GetTable<TrimTestTable>().OrderBy(_ => _.ID).Select(r => new { r.ID, r.Data}));
		}

	}
}
