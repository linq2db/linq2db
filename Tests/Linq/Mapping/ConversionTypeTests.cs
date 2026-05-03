using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

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
			[PrimaryKey]
			public int     ID   { get; set; }
			[Column(Length = 50)]
			public required string Data { get; set; }
		}

		[Test]
		public void InsertTest([DataSources] string context, [Values] bool inlineParameters)
		{
			using var db = GetDataContext(context, _trimMappingSchema);
			using var t  = db.CreateLocalTable<TrimTestTable>();

			db.InlineParameters = inlineParameters;

			db.Insert(new TrimTestTable { ID = 1, Data = "OOO" });

			t
				.Value(t => t.ID,   2)
				.Value(t => t.Data, "HHH")
				.Insert();

			t.Insert(() => new TrimTestTable { ID = 3, Data = "VVV" });

			AreEqual(
				[
					new { ID = 1, Data = "OOO" },
					new { ID = 2, Data = "HHH" },
					new { ID = 3, Data = "VVV" }
				],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data }));

			using var db1 = GetDataContext(context);

			AreEqual(
				[
					new { ID = 1, Data = "***OOO***"},
					new { ID = 2, Data = "***HHH***"},
					new { ID = 3, Data = "***VVV***"}
				],
				db1.GetTable<TrimTestTable>().OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));
		}

		[Test]
		public void InsertWithOutputTest([DataSources([
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSybase,
			])] string context,
			[Values] bool inlineParameters)
		{
			using var db = GetDataContext(context, _trimMappingSchema);
			using var t  = db.CreateLocalTable<TrimTestTable>(
			[
				new TrimTestTable { ID = 1, Data = "OOO" }
			]);

			db.InlineParameters = inlineParameters;

			var o = t
				.InsertWithOutput(
					t,
					r => new TrimTestTable { ID = r.ID + 1, Data = "HHH" },
					inserted => new { inserted.ID, inserted.Data })
				.ToList();

			AreEqual([new { ID = 2, Data = "HHH" }], o.OrderBy(r => r.ID));

			AreEqual(
				[
					new { ID = 1, Data = "OOO" },
					new { ID = 2, Data = "HHH" }
				],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

			AreEqual(
				[
					new { ID = 1, Data = "***OOO***"},
					new { ID = 2, Data = "***HHH***"}
				],
				db1.GetTable<TrimTestTable>().OrderBy(_ => _.ID).Select(r => new { r.ID, r.Data}));
		}

		[Test]
		public void BulkCopyTest([DataSources(false, TestProvName.AllMySqlConnector)] string context, [Values] bool inlineParameters, [Values] BulkCopyType bulkCopyType)
		{
			using var db = GetDataContext(context, o => o.UseMappingSchema(_trimMappingSchema).UseBulkCopyType(bulkCopyType));
			using var t  = db.CreateLocalTable<TrimTestTable>();

			db.InlineParameters = inlineParameters;

			t.BulkCopy([new TrimTestTable { ID = 1, Data = "OOO" }]);

			AreEqual(
				[ new { ID = 1, Data = "OOO"} ],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

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
					new TrimTestTable { ID = 3, Data = "VVV" },
				]);

			db.Update(new TrimTestTable { ID = 3, Data = "III" });

			t
				.Where(t => t.Data == "XXX")
				.Set(t => t.Data, "OOO")
				.Update();

			var p = "HHH";

			t
				.Where(t => t.Data == p)
				.Set(t => t.Data, "SSS")
				.Update();

			AreEqual(
				[
					new { ID = 1, Data = "OOO"},
					new { ID = 2, Data = "SSS"},
					new { ID = 3, Data = "III"},
				],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

			AreEqual(
				[
					new { ID = 1, Data = "***OOO***"},
					new { ID = 2, Data = "***SSS***"},
					new { ID = 3, Data = "***III***"},
				],
				db1.GetTable<TrimTestTable>().OrderBy(_ => _.ID).Select(r => new { r.ID, r.Data}));
		}

		[Test]
		public void MergeTest([MergeDataContextSource] string context, [Values] bool inlineParameters)
		{
			using var db = GetDataContext(context, _trimMappingSchema);

			db.InlineParameters = inlineParameters;

			using var t  = db.CreateLocalTable<TrimTestTable>(
				[
					new TrimTestTable { ID = 1, Data = "XXX" },
					new TrimTestTable { ID = 3, Data = "VVV" },
				]);

			t
				.Merge()
				.Using(
				[
					new TrimTestTable { ID = 1, Data = "OOO" },
					new TrimTestTable { ID = 2, Data = "SSS" },
				])
				.OnTargetKey()
				.UpdateWhenMatched()
				.InsertWhenNotMatched()
				.Merge();

#pragma warning disable CS0618 // Type or member is obsolete
			t.Merge(
				[
					new TrimTestTable { ID = 3, Data = "III" }
				]);
#pragma warning restore CS0618 // Type or member is obsolete

			AreEqual(
				[
					new { ID = 1, Data = "OOO"},
					new { ID = 2, Data = "SSS"},
					new { ID = 3, Data = "III"},
				],
				t.OrderBy(r => r.ID).Select(r => new { r.ID, r.Data}));

			using var db1 = GetDataContext(context);

			AreEqual(
				[
					new { ID = 1, Data = "***OOO***"},
					new { ID = 2, Data = "***SSS***"},
					new { ID = 3, Data = "***III***"},
				],
				db1.GetTable<TrimTestTable>().OrderBy(_ => _.ID).Select(r => new { r.ID, r.Data}));
		}
	}
}
