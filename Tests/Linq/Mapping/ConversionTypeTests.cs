using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

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
				;

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

		class ImplicitValue<TData> : IEquatable<ImplicitValue<TData>>
		{
			public TData? Value { get; init; }

			public static implicit operator TData?(ImplicitValue<TData>? value)
			{
				return value != null ? value.Value : default;
			}

			public static implicit operator ImplicitValue<TData>(TData? value)
			{
				return new ImplicitValue<TData> { Value = value };
			}

			public bool Equals(ImplicitValue<TData>? other)
			{
				if (other is null)                return false;
				if (ReferenceEquals(this, other)) return true;

				return EqualityComparer<TData?>.Default.Equals(Value, other.Value);
			}

			public override bool Equals(object? obj)
			{
				if (obj is null)                return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;

				return Equals((ImplicitValue<TData>)obj);
			}

			public override int GetHashCode()
			{
				return Value != null ? Value.GetHashCode() : 0;
			}

			public override string ToString()
			{
				return Value == null ? "Value=null" : $"Value={Value}";
			}

			public static bool operator ==(ImplicitValue<TData>? left, ImplicitValue<TData>? right)
			{
				return Equals(left, right);
			}

			public static bool operator !=(ImplicitValue<TData>? left, ImplicitValue<TData>? right)
			{
				return !Equals(left, right);
			}
		}

		class ImplicitData
		{
			public required ImplicitValue<string?> StringData1 { get; init; }
			public required ImplicitValue<string?> StringData2 { get; init; }
			public required ImplicitValue<int?>    IntData1    { get; init; }
			public required ImplicitValue<int?>    IntData2    { get; init; }
		}

		[Test]
		public void ImplicitTest()
		{
			using var db = new TestDataConnection();

			using var t = db.CreateLocalTable(
			[
				new
				{
					StringData1 = "Test1",
					StringData2 = (string?)null,
					IntData1    = 123,
					IntData2    = (int?)null,
				}
			]);

			var result1 = t
				.Select(r => new ImplicitData
				{
					StringData1 = r.StringData1,
					StringData2 = r.StringData2,
					IntData1    = r.IntData1,
					IntData2    = r.IntData2,
				})
				.Single();

			var result = t.Single();

			var result2 = new ImplicitData
				{
					StringData1 = result.StringData1,
					StringData2 = result.StringData2,
					IntData1    = result.IntData1,
					IntData2    = result.IntData2,
				};

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result1.StringData1, Is.EqualTo(result2.StringData1));
				Assert.That(result1.StringData2, Is.EqualTo(result2.StringData2));
				Assert.That(result1.IntData1,    Is.EqualTo(result2.IntData1));
				Assert.That(result1.IntData2,    Is.EqualTo(result2.IntData2));
			}
		}
	}
}
