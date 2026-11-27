using System;
using System.Data.Linq;
using System.IO;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	// TODO: delete this test when we implement type tests for all databases similar to CLickHouse tests
	[TestFixture]
	public class DataTypesTests : TestBase
	{
		public abstract class TypeTable<TType>
			where TType : struct
		{
			[PrimaryKey] public int    Id         { get; set; }
			[Column] public TType  Column         { get; set; }
			[Column] public TType? ColumnNullable { get; set; }
		}

		#region Guid
		[Table]
		public class GuidTable : TypeTable<Guid>
		{
			public static GuidTable[] Data = new[]
			{
				new GuidTable() { Id = 1, Column = TestData.Guid1, ColumnNullable = null },
				new GuidTable() { Id = 2, Column = TestData.Guid2, ColumnNullable = TestData.Guid3 },
			};
		}

		[Test]
		public void TestGuid([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			TestType<GuidTable, Guid>(db, GuidTable.Data, context, supportLiterals: !context.IsAnyOf(TestProvName.AllAccessOdbc));
		}
		#endregion

		#region Byte
		[Table]
		public class ByteTable : TypeTable<byte>
		{
			public static ByteTable[] Data = new[]
			{
				new ByteTable() { Id = 1, Column = 1, ColumnNullable = null },
				new ByteTable() { Id = 2, Column = 255, ColumnNullable = 2 },
			};
		}

		[Test]
		public void TestByte([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			TestType<ByteTable, byte>(db, ByteTable.Data, context);
		}
		#endregion

		#region DateOnly
#if SUPPORTS_DATEONLY
		[Table]
		public class DateOnlyTable : TypeTable<DateOnly>
		{
			public static DateOnlyTable[] Data = new[]
			{
				new DateOnlyTable() { Id = 1, Column = new DateOnly(1980, 1, 1), ColumnNullable = null },
				new DateOnlyTable() { Id = 2, Column = new DateOnly(2020, 2, 29), ColumnNullable = new DateOnly(2020, 1, 1) },
			};
		}

		[Test]
		public void TestDateOnly([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			TestType<DateOnlyTable, DateOnly>(db, DateOnlyTable.Data, context);
		}
#endif
		#endregion

		#region Boolean
		[Table]
		public class BooleanTable : TypeTable<bool>
		{
			public static BooleanTable[] Data = new[]
			{
				new BooleanTable() { Id = 1, Column = true, ColumnNullable = null },
				new BooleanTable() { Id = 2, Column = false, ColumnNullable = true },
			};
		}

		[Test]
		public void TestBoolean([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var data = BooleanTable.Data;
			if (context.IsAnyOf(TestProvName.AllAccess, TestProvName.AllSybase))
			{
				// for both Access and ASE BIT type cannot be NULL
				data = data.Select(r => new BooleanTable() { Id = r.Id, Column = r.Column, ColumnNullable = r.ColumnNullable ?? false }).ToArray();
			}

			TestType<BooleanTable, bool>(db, data, context);
		}
		#endregion

		#region Enum (int)
		public enum IntEnum
		{
			Value1 = 1,
			Value2 = 2,
			Value3 = 3,
		}
		[Table]
		public class IntEnumTable : TypeTable<IntEnum>
		{
			public static IntEnumTable[] Data = new[]
			{
				new IntEnumTable() { Id = 1, Column = IntEnum.Value1, ColumnNullable = null },
				new IntEnumTable() { Id = 2, Column = IntEnum.Value2, ColumnNullable = IntEnum.Value3 },
			};
		}

		[Test]
		public void TestIntEnum([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			TestType<IntEnumTable, IntEnum>(db, IntEnumTable.Data, context);
		}
		#endregion

		#region Enum (string)
		public enum StringEnum
		{
			[MapValue("val=1")   ] Value1 = 1,
			[MapValue("value=2") ] Value2 = 2,
			[MapValue("value=33")] Value3 = 33,
		}
		[Table]
		public class StringEnumTable : TypeTable<StringEnum>
		{
			public static StringEnumTable[] Data = new[]
			{
				new StringEnumTable() { Id = 1, Column = StringEnum.Value1, ColumnNullable = null },
				new StringEnumTable() { Id = 2, Column = StringEnum.Value2, ColumnNullable = StringEnum.Value3 },
			};
		}

		[Test]
		public void TestStringEnum([DataSources(false)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			TestType<StringEnumTable, StringEnum>(db, StringEnumTable.Data, context);
		}
		#endregion

		#region Test Implementation
		[Sql.Expression("{0} = {1}", IsPredicate = true, ServerSideOnly = true)]
		private static bool Equality(object? x, object? y) => throw new ServerSideOnlyException(nameof(Equality));

		private void TestType<TTable, TType>(DataConnection db, TTable[] data, string context, bool supportLiterals = true)
			where TTable: TypeTable<TType>
			where TType: struct
		{
			var supportsParameters = !context.IsAnyOf(TestProvName.AllClickHouse);

			using var table = db.CreateLocalTable(data);

			// test parameter
			db.InlineParameters = false;
			db.OnNextCommandInitialized((_, cmd) =>
			{
				Assert.That(cmd.Parameters, Has.Count.EqualTo(supportsParameters ? 2 : 0));
				return cmd;
			});

			var records = table.Where(r => Equality(r.Column, data[1].Column) && Equality(r.ColumnNullable, data[1].ColumnNullable)).ToArray();
			Assert.That(records, Has.Length.EqualTo(1));

			var record = records[0];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Id, Is.EqualTo(2));
				Assert.That(record.Column, Is.EqualTo(data[1].Column));
				Assert.That(record.ColumnNullable, Is.EqualTo(data[1].ColumnNullable));
			}

			// test literal
			db.InlineParameters = true;
			db.OnNextCommandInitialized((_, cmd) =>
			{
				if (supportLiterals)
					Assert.That(cmd.Parameters, Is.Empty);
				else
					Assert.That(cmd.Parameters, Has.Count.EqualTo(2));
				return cmd;
			});

			records = table.Where(r => Equality(r.Column, data[1].Column) && Equality(r.ColumnNullable, data[1].ColumnNullable)).ToArray();
			Assert.That(records, Has.Length.EqualTo(1));

			record = records[0];
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Id, Is.EqualTo(2));
				Assert.That(record.Column, Is.EqualTo(data[1].Column));
				Assert.That(record.ColumnNullable, Is.EqualTo(data[1].ColumnNullable));
			}

			db.InlineParameters = false;

			// test bulk copy
			TestBulkCopy<TTable, TType>(db, context, data, table, BulkCopyType.RowByRow);
			TestBulkCopy<TTable, TType>(db, context, data, table, BulkCopyType.MultipleRows);
			TestBulkCopy<TTable, TType>(db, context, data, table, BulkCopyType.ProviderSpecific);
		}

		private void TestBulkCopy<TTable, TType>(DataConnection db, string context, TTable[] data, TempTable<TTable> table, BulkCopyType bulkCopyType)
			where TTable : TypeTable<TType>
			where TType : struct
		{
			table.Delete();

			var options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = bulkCopyType };

			db.BulkCopy(options, data);
			var records = table.OrderBy(r => r.Id).ToArray();
			Assert.That(records, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(records[0].Id, Is.EqualTo(data[0].Id));
				Assert.That(records[0].Column, Is.EqualTo(data[0].Column));
				Assert.That(records[0].ColumnNullable, Is.EqualTo(data[0].ColumnNullable));
				Assert.That(records[1].Id, Is.EqualTo(data[1].Id));
				Assert.That(records[1].Column, Is.EqualTo(data[1].Column));
				Assert.That(records[1].ColumnNullable, Is.EqualTo(data[1].ColumnNullable));
			}
		}
		#endregion

		// Stream columns are unimplemented everywhere, so each provider dies at whichever stage it first meets
		// the unsupported type. Grouped by that stage rather than by wording.
		[ActiveIssue(1918, Configurations = [TestProvName.AllSqlServer, ProviderName.SqlCe, TestProvName.AllPostgreSQL],
			Details = "no-declaration: the emitted DDL carries the literal type name Blob and each server rejects it in its own words - 'Cannot find data type Blob', 'The specified data type is not valid', '42704: type \"blob\" does not exist'.")]
		[ActiveIssue(1918, Configuration = TestProvName.AllClickHouse, ErrorTypeName = "LinqToDB.LinqToDBException",
			ErrorMessage = "Cannot infer type name from (System.IO.Stream, Blob). Specify DataType or DbType explicitly",
			Details = "linq2db refuses to infer the column type before any DDL reaches the server, so all three ClickHouse drivers give one message.")]
		[ActiveIssue(1918, Configurations = [TestProvName.AllSQLite, TestProvName.AllDuckDB], SkipForLinqService = true,
			Details = "no-declaration: the table is created, and the insert then cannot convert the Stream to the driver's native type - an InvalidCastException to Byte[] on SQLite.Classic/MPU/MPM and DuckDB, an InvalidOperationException about a missing mapping on SQLite.MS.")]
		[ActiveIssue(1918, Configuration = TestProvName.AllYdb, SkipForLinqService = true, ErrorTypeName = "LinqToDB.Common.LinqToDBConvertException",
			ErrorMessage = "Cannot convert value 'System.Byte[]: System.Byte[]' to type 'System.IO.Stream'",
			Details = "YDB gets furthest of any provider - it stores the value and only fails converting it back on select.")]
		[ActiveIssue(1918, Configurations = [TestProvName.AllSQLite, TestProvName.AllDuckDB, TestProvName.AllYdb], SkipForNonLinqService = true,
			ErrorTypeName = "LinqToDB.Common.LinqToDBConvertException", ErrorMessage = "Cannot convert value 'System.IO.MemoryStream: System.String' to type 'System.IO.MemoryStream'",
			Details = "Remote fails earlier than direct and identically across these three: serializing the Stream parameter for the wire is what breaks, before any provider-specific handling runs.")]
		[ActiveIssue(1918, Configurations = [TestProvName.AllAccess, TestProvName.AllSybase, TestProvName.AllSapHana, TestProvName.AllOracle,
			TestProvName.AllInformix, TestProvName.AllFirebird, TestProvName.AllMySql, ProviderName.DB2],
			Details = "no-declaration: unvalidated: not reachable from this workstation - no container for most, and Informix/DB2 evidence is inadmissible here because the IBM CLI driver replaces the server text with a codepage-conversion message.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1918")]
		public void StreamBlobRoundTrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<StreamBlobTable>();

			using (var stream = new MemoryStream())
			{
				stream.WriteByte(1);
				stream.Flush();
				stream.WriteByte(2);
				stream.Position = 0;

				var entity = new StreamBlobTable()
				{
					Id = 1,
					Blob = stream
				};

				db.Insert(entity);
			}

			var record = tb.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Id, Is.EqualTo(1));
				Assert.That(record.Blob, Is.Not.Null);
			}

			using var ms = new MemoryStream();
			record.Blob!.CopyTo(ms);
			var data = ms.ToArray();
			Assert.That(data, Is.EqualTo(new byte[] { 1, 2 }));
		}

		[Table("StreamBlobTable")]
		sealed class StreamBlobTable
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(DataType = DataType.Blob)] public Stream? Blob { get; set; }
		}

		[Table]
		public class BinaryColumnRecord
		{
			[PrimaryKey]
			public int    ID     { get; set; }
			[Column(Length = 10)]
			public byte[]? Array  { get; set; }
			[Column(Length = 10)]
			public Binary? Binary { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1303")]
		public void BinaryAndByteArrayColumnRoundTrip([DataSources] string context, [Values] bool inlineParameters)
		{
			using var db = GetDataContext(context);
			using var tbl = db.CreateLocalTable<BinaryColumnRecord>();
			// Informix: apply inlining to insert to test binary parameters
			if (context.IsAnyOf(TestProvName.AllInformix))
				db.InlineParameters = inlineParameters;

			tbl.Insert(() => new BinaryColumnRecord()
			{
				ID = 1,
				Array = new byte[] { 1, 2, 3 },
				Binary = new Binary(new byte[] { 4, 5 })
			});

			db.InlineParameters = inlineParameters;

			var byId     = tbl.Where(_ => _.ID == 1).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(byId.ID, Is.EqualTo(1));
				Assert.That(new byte[] { 1, 2, 3 }.SequenceEqual(byId.Array!), Is.True);
				Assert.That(new byte[] { 4, 5 }.SequenceEqual(byId.Binary!.ToArray()), Is.True);
			}

			// Informix: doesn't support blobs in conditions
			if (!context.IsAnyOf(TestProvName.AllInformix))
			{
				var byArray  = tbl.Where(_ => _.Array  == new byte[] { 1, 2, 3 })         .Single();
				var byBinary = tbl.Where(_ => _.Binary == new Binary(new byte[] { 4, 5 })).Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(byArray.ID, Is.EqualTo(1));
					Assert.That(new byte[] { 1, 2, 3 }.SequenceEqual(byArray.Array!), Is.True);
					Assert.That(new byte[] { 4, 5 }.SequenceEqual(byArray.Binary!.ToArray()), Is.True);

					Assert.That(byBinary.ID, Is.EqualTo(1));
					Assert.That(new byte[] { 1, 2, 3 }.SequenceEqual(byBinary.Array!), Is.True);
					Assert.That(new byte[] { 4, 5 }.SequenceEqual(byBinary.Binary!.ToArray()), Is.True);
				}
			}
		}

		public class SByteColumnsRecord
		{
			[PrimaryKey] public int ID { get; set; }
			public sbyte TestField { get; set; }
			public sbyte? TestNullable { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1351")]
		public void CompareSByteAndNullableSByteColumns([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SByteColumnsRecord>();
			table.Where(_ => _.TestField == 0).ToArray(); // Compare `sbyte`
			table.Where(_ => _.TestNullable != 1).ToArray(); // Compare `sbyte?` to non-null
		}

		public class BlobValueEntity
		{
			[Column, PrimaryKey]
			public int Id;

			[Column(Length = 100)]
			[Column(DataType = DataType.Blob,      Configuration = ProviderName.DB2)]
			[Column(DataType = DataType.Blob,      Configuration = ProviderName.Firebird)]
			[Column(DataType = DataType.Blob,      Configuration = ProviderName.Oracle)]
			[Column(DataType = DataType.VarBinary, Configuration = ProviderName.ClickHouse)]
			[Column(DataType = DataType.Blob,      Configuration = ProviderName.PostgreSQL, DbType = "bytea")]
			[Column(                               Configuration = ProviderName.Informix,   DbType = "byte")]
			public byte[]? BlobValue;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/421")]
		public void BlobInsertAndUpdateWithParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<BlobValueEntity>())
			{
				db.Into(db.GetTable<BlobValueEntity>())
					.Value(p => p.Id,        1)
					.Value(p => p.BlobValue, new byte[] { 1, 2, 3 })
					.Insert();

				var v = db.GetTable<BlobValueEntity>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue!);

				db.GetTable<BlobValueEntity>()
					.Where(_ => _.Id == 1)
					.Set(_ => _.BlobValue, new byte[] {3, 2, 1})
					.Update();

				v = db.GetTable<BlobValueEntity>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue!);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/421")]
		public void BlobInsertAndUpdateWithInlinedParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<BlobValueEntity>())
			{
				db.InlineParameters = true;

				db.Into(db.GetTable<BlobValueEntity>())
					.Value(p => p.Id,        1)
					.Value(p => p.BlobValue, new byte[] { 1, 2, 3 })
					.Insert();

				var v = db.GetTable<BlobValueEntity>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue!);

				db.GetTable<BlobValueEntity>()
					.Where(_ => _.Id == 1)
					.Set(_ => _.BlobValue, new byte[] {3, 2, 1})
					.Update();

				v = db.GetTable<BlobValueEntity>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue!);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/421")]
		public void BlobEntityInsert([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<BlobValueEntity>())
			{
				var e = new BlobValueEntity() {Id = 1, BlobValue = new byte[] {1, 2, 3}};

				db.Insert(e);

				var v = db.GetTable<BlobValueEntity>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue!);

				e.BlobValue = new byte[] {3, 2, 1};

				v = db.GetTable<BlobValueEntity>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue!);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/421")]
		public void BlobEntityInsertIntoNamedTableWithInlinedParameters([DataSources] string context)
		{
			var tableName = nameof(BlobValueEntity);
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<BlobValueEntity>(tableName);
			db.InlineParameters = true;

			var e = new BlobValueEntity() { Id = 1, BlobValue = new byte[] { 1, 2, 3 } };

			db.Insert(e, tableName);

			var v = table.First(_ => _.Id == 1);

			AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue!);

			e.BlobValue = new byte[] { 3, 2, 1 };

			v = table.First(_ => _.Id == 1);

			AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue!);
		}
	}
}
