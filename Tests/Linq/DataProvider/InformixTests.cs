using System;
using System.Linq;
using System.Data.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Informix;
using LinqToDB.Mapping;

#if NETFRAMEWORK
using IBM.Data.Informix;
#endif
using NUnit.Framework;

namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class InformixTests : DataProviderTestBase
	{
		const string CurrentProvider = TestProvName.AllInformix;

		protected override string? PassNullSql(DataConnection dc, out int paramCount)
		{
			paramCount = 1;
			return null;
		}
		protected override string  PassValueSql(DataConnection dc) => "SELECT ID FROM {1} WHERE {0} = ?";

		[Test]
		public void TestDataTypes([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				// TimeSpan cannot be passed as parameter if it is not IfxTimeSpan
				// for Linq queries we handle it by converting parameters to literals, but Execute uses parameters
				var isIDSProvider = ((InformixDataProvider)conn.DataProvider).Adapter.IsIDSProvider;

				Assert.Multiple(() =>
				{
					Assert.That(TestType<long?>(conn, "bigintDataType", DataType.Int64), Is.EqualTo(1000000L));
					Assert.That(TestType<long?>(conn, "int8DataType", DataType.Int64), Is.EqualTo(1000001L));
					Assert.That(TestType<int?>(conn, "intDataType", DataType.Int32), Is.EqualTo(7777777));
					Assert.That(TestType<short?>(conn, "smallintDataType", DataType.Int16), Is.EqualTo(100));
					Assert.That(TestType<decimal?>(conn, "decimalDataType", DataType.Decimal), Is.EqualTo(9999999m));
					Assert.That(TestType<decimal?>(conn, "moneyDataType", DataType.Money), Is.EqualTo(8888888m));
					Assert.That(TestType<float?>(conn, "realDataType", DataType.Single), Is.EqualTo(20.31f));
					Assert.That(TestType<double?>(conn, "floatDataType", DataType.Double), Is.EqualTo(16.2d));

					Assert.That(TestType<bool?>(conn, "boolDataType", DataType.Boolean), Is.EqualTo(true));

					Assert.That(TestType<string>(conn, "charDataType", DataType.Char), Is.EqualTo("1"));
					Assert.That(TestType<string>(conn, "varcharDataType", DataType.VarChar), Is.EqualTo("234"));
					Assert.That(TestType<string>(conn, "ncharDataType", DataType.NChar), Is.EqualTo("55645"));
					Assert.That(TestType<string>(conn, "nvarcharDataType", DataType.NVarChar), Is.EqualTo("6687"));
					Assert.That(TestType<string>(conn, "lvarcharDataType", DataType.NVarChar), Is.EqualTo("AAAAA"));

					Assert.That(TestType<DateTime?>(conn, "dateDataType", DataType.Date), Is.EqualTo(new DateTime(2012, 12, 12)));
					Assert.That(TestType<DateTime?>(conn, "datetimeDataType", DataType.DateTime2), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				});
				if (!isIDSProvider)
					Assert.That(TestType<TimeSpan?>   (conn, "intervalDataType", DataType.Time),      Is.EqualTo(new TimeSpan(12, 12, 12)));

				Assert.Multiple(() =>
				{
					Assert.That(TestType<string>(conn, "textDataType", DataType.Text, skipPass: true), Is.EqualTo("BBBBB"));
					Assert.That(TestType<string>(conn, "textDataType", DataType.NText, skipPass: true), Is.EqualTo("BBBBB"));
					Assert.That(TestType<byte[]>(conn, "byteDataType", DataType.Binary, skipPass: true), Is.EqualTo(new byte[] { 1, 2 }));
					Assert.That(TestType<byte[]>(conn, "byteDataType", DataType.VarBinary, skipPass: true), Is.EqualTo(new byte[] { 1, 2 }));
				});

#if NETFRAMEWORK
				if (context == ProviderName.Informix)
				{
					Assert.That(TestType<IfxDateTime?>(conn, "datetimeDataType", DataType.DateTime), Is.EqualTo(new IfxDateTime(new DateTime(2012, 12, 12, 12, 12, 12))));
					if (!isIDSProvider)
					{
						Assert.Multiple(() =>
						{
							Assert.That(TestType<IfxDecimal?>(conn, "decimalDataType", DataType.Decimal), Is.EqualTo(new IfxDecimal(9999999m)));
							Assert.That(TestType<IfxTimeSpan?>(conn, "intervalDataType", DataType.Time), Is.EqualTo(new IfxTimeSpan(new TimeSpan(12, 12, 12))));
						});
					}
				}
#endif
			}
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(CurrentProvider)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
					try
					{
						db.BulkCopy(
							new BulkCopyOptions { BulkCopyType = bulkCopyType },
							Enumerable.Range(0, 10).Select(n =>
								new LinqDataTypes
								{
									ID            = 4000 + n,
									MoneyValue    = 1000m + n,
									DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
									BoolValue     = true,
									GuidValue     = TestData.SequentialGuid(n),
									SmallIntValue = (short)n
								}
							));
					}
					finally
					{
						db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
					}
				}
			}
		}

		[Test]
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(CurrentProvider)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
					try
					{
						await db.BulkCopyAsync(
							new BulkCopyOptions { BulkCopyType = bulkCopyType },
							Enumerable.Range(0, 10).Select(n =>
								new LinqDataTypes
								{
									ID            = 4000 + n,
									MoneyValue    = 1000m + n,
									DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
									BoolValue     = true,
									GuidValue     = TestData.SequentialGuid(n),
									SmallIntValue = (short)n
								}
							));
					}
					finally
					{
						await db.GetTable<LinqDataTypes>().DeleteAsync(p => p.ID >= 4000);
					}
				}
			}
		}

		#region BulkCopy
		[Table("AllTypes")]
		public partial class AllType
		{
			[PrimaryKey] public int ID { get; set; }

			[Column] public long?     bigintDataType   { get; set; }
			[Column] public long?     int8DataType     { get; set; }
			[Column] public int?      intDataType      { get; set; }
			[Column] public short?    smallintDataType { get; set; }
			[Column] public decimal?  decimalDataType  { get; set; }
			[Column] public decimal?  moneyDataType    { get; set; }
			[Column] public float?    realDataType     { get; set; }
			[Column] public double?   floatDataType    { get; set; }
			[Column] public bool?     boolDataType     { get; set; }
			[Column] public char?     charDataType     { get; set; }
			[Column] public string?   char20DataType   { get; set; }
			[Column] public string?   varcharDataType  { get; set; }
			[Column] public string?   ncharDataType    { get; set; }
			[Column] public string?   nvarcharDataType { get; set; }
			[Column] public string?   lvarcharDataType { get; set; }
			[Column] public string?   textDataType     { get; set; }
			[Column] public DateTime? dateDataType     { get; set; }
			[Column] public DateTime? datetimeDataType { get; set; }
			[Column] public TimeSpan? intervalDataType { get; set; }
			[Column] public byte[]?   byteDataType     { get; set; }
		}

		static readonly AllType[] _allTypeses =
		{
#region data
			new AllType
			{
				ID                       = 700,
				bigintDataType           = 1,
				int8DataType             = 2,
				intDataType              = 1,
				smallintDataType         = 1,
				decimalDataType          = 1.1m,
				moneyDataType            = 1.2m,
				realDataType             = 1.5f,
				floatDataType            = 1.4d,
				boolDataType             = true,
				charDataType             = 'E',
				char20DataType           = "Eboi",
				varcharDataType          = "E",
				ncharDataType            = "Ё",
				nvarcharDataType         = "ы",
				lvarcharDataType         = "Й",
				textDataType             = "E",
				dateDataType             = new DateTime(2014, 12, 17),
				datetimeDataType         = new DateTime(2014, 12, 17, 21, 2, 58),
				intervalDataType         = new TimeSpan(0, 10, 11, 12),
				byteDataType             = new byte[] { 1, 2, 3 },
			},
			new AllType
			{
				ID                       = 701,
			},
#endregion
		};

		[Table("LinqDataTypes")]
		sealed class DataTypes
		{
			[Column] public int       ID;
			[Column] public decimal?  MoneyValue;
			[Column] public DateTime? DateTimeValue;
			[Column] public DateTime? DateTimeValue2;
			[Column] public bool?     BoolValue;
			[Column] public Guid?     GuidValue;
			[Column] public Binary?   BinaryValue;
			[Column] public short?    SmallIntValue;
			[Column] public int?      IntValue;
			[Column] public long?     BigIntValue;
			[Column] public string?   StringValue;
		}

		[Test]
		public void BulkCopyLinqTypesMultipleRows([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					db.BulkCopy(
						new BulkCopyOptions
						{
							BulkCopyType       = BulkCopyType.MultipleRows,
							RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					db.GetTable<DataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

		[Test]
		public async Task BulkCopyLinqTypesMultipleRowsAsync([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions
						{
							BulkCopyType       = BulkCopyType.MultipleRows,
							RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					await db.GetTable<DataTypes>().DeleteAsync(p => p.ID >= 4000);
				}
			}
		}

		[Test]
		public void BulkCopyLinqTypesProviderSpecific([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					db.BulkCopy(
						new BulkCopyOptions
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					db.GetTable<DataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

		[Test]
		public async Task BulkCopyLinqTypesProviderSpecificAsync([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					await db.GetTable<DataTypes>().DeleteAsync(p => p.ID >= 4000);
				}
			}
		}

		void BulkCopyAllTypes(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				db.CommandTimeout = 60;

				db.GetTable<AllType>().Delete(p => p.ID >= _allTypeses[0].ID);

				var keepIdentity = bulkCopyType == BulkCopyType.ProviderSpecific
					&& ((InformixDataProvider)db.DataProvider).Adapter.IsIDSProvider;

				try
				{
					db.BulkCopy(
						new BulkCopyOptions
						{
							BulkCopyType       = bulkCopyType,
							RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied),
							KeepIdentity       = keepIdentity
						},
						_allTypeses);

					var ids = _allTypeses.Select(at => at.ID).ToArray();

					var list = db.GetTable<AllType>().Where(t => ids.Contains(t.ID)).OrderBy(t => t.ID).ToList();

					Assert.That(list, Has.Count.EqualTo(_allTypeses.Length));

					for (var i = 0; i < list.Count; i++)
						CompareObject(db.MappingSchema, list[i], _allTypeses[i]);
				}
				finally
				{
					db.GetTable<AllType>().Delete(p => p.ID >= _allTypeses[0].ID);
				}
			}
		}

		async Task BulkCopyAllTypesAsync(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				db.CommandTimeout = 60;

				await db.GetTable<AllType>().DeleteAsync(p => p.ID >= _allTypeses[0].ID);

				var keepIdentity = bulkCopyType == BulkCopyType.ProviderSpecific
					&& ((InformixDataProvider)db.DataProvider).Adapter.IsIDSProvider;

				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions
						{
							BulkCopyType = bulkCopyType,
							RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied),
							KeepIdentity = keepIdentity
						},
						_allTypeses);

					var ids = _allTypeses.Select(at => at.ID).ToArray();

					var list = await db.GetTable<AllType>().Where(t => ids.Contains(t.ID)).OrderBy(t => t.ID).ToListAsync();

					Assert.That(list, Has.Count.EqualTo(_allTypeses.Length));

					for (var i = 0; i < list.Count; i++)
						CompareObject(db.MappingSchema, list[i], _allTypeses[i]);
				}
				finally
				{
					await db.GetTable<AllType>().DeleteAsync(p => p.ID >= _allTypeses[0].ID);
				}
			}
		}

		void CompareObject<T>(MappingSchema mappingSchema, T actual, T test)
			where T: notnull
		{
			var ed = mappingSchema.GetEntityDescriptor(typeof(T));

			foreach (var column in ed.Columns)
			{
				var actualValue = column.GetProviderValue(actual);
				var testValue   = column.GetProviderValue(test);

				Assert.That(actualValue, Is.EqualTo(testValue),
					actualValue is DateTimeOffset
						? $"Column  : {column.MemberName} {actualValue:yyyy-MM-dd HH:mm:ss.fffffff zzz} {testValue:yyyy-MM-dd HH:mm:ss.fffffff zzz}"
						: $"Column  : {column.MemberName}");
			}
		}

		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public void BulkCopyAllTypesMultipleRows([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			BulkCopyAllTypes(context, BulkCopyType.MultipleRows);
		}

		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public void BulkCopyAllTypesProviderSpecific([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			BulkCopyAllTypes(context, BulkCopyType.ProviderSpecific);
		}

		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public async Task BulkCopyAllTypesMultipleRowsAsync([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			await BulkCopyAllTypesAsync(context, BulkCopyType.MultipleRows);
		}

		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public async Task BulkCopyAllTypesProviderSpecificAsync([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			await BulkCopyAllTypesAsync(context, BulkCopyType.ProviderSpecific);
		}

		[Test]
		public void CreateAllTypes([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var ms = new MappingSchema();

				new FluentMappingBuilder(ms)
					.Entity<AllType>()
						.HasTableName("AllTypeCreateTest")
					.Build();

				db.AddMappingSchema(ms);

				try
				{
					db.DropTable<AllType>();
				}
				catch
				{
				}

				var table = db.CreateTable<AllType>();

				table.ToList();

				db.DropTable<AllType>();
			}
		}
		#endregion
	}
}
