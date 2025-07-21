using System;
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
			[Column] public int    Id             { get; set; }
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
				new DateOnlyTable() { Id = 1, Column = new DateOnly(1950, 1, 1), ColumnNullable = null },
				new DateOnlyTable() { Id = 2, Column = new DateOnly(2020, 2, 29), ColumnNullable = new DateOnly(2200, 1, 1) },
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
		[Sql.Expression("{0} = {1}", IsPredicate = true)]
		private static bool Equality(object? x, object? y) => throw new NotImplementedException();

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

		#region Issue 1918
		[ActiveIssue(Configurations = [
			// cannot create table, DataType.Blob not mapped
			TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllPostgreSQL, ProviderName.SqlCe,
			TestProvName.AllSqlServer, TestProvName.AllSybase,
			// fails on insert
			TestProvName.AllSQLite, TestProvName.AllSapHana, TestProvName.AllOracle, TestProvName.AllInformix, TestProvName.AllFirebird,
			// fails on select
			TestProvName.AllMySql, ProviderName.DB2
			])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1918")]
		public void Issue1918Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue1918Table>();

			using (var stream = new MemoryStream())
			{
				var entity = new Issue1918Table()
				{
					Id = 1,
					Blob = stream
				};

				db.Insert(entity);
				stream.WriteByte(1);
				stream.Flush();
				stream.WriteByte(2);
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

		[Table("Issue4163Table")]
		sealed class Issue1918Table
		{
			[Column] public int Id { get; set; }
			[Column(DataType = DataType.Blob)] public Stream? Blob { get; set; }
		}
		#endregion
	}
}
