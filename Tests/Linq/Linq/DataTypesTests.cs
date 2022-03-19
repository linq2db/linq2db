﻿using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Extensions;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Common;
	using LinqToDB.Data;
	using Model;

	// TODO: add more base types tests
	// TODO: and more type test cases
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

			TestType<GuidTable, Guid>(db, GuidTable.Data);
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

			TestType<ByteTable, byte>(db, ByteTable.Data);
		}
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
			if (context.Contains("Access") || context.Contains("Sybase"))
			{
				// for both Access and ASE BIT type cannot be NULL
				data = data.Select(r => new BooleanTable() { Id = r.Id, Column = r.Column, ColumnNullable = r.ColumnNullable ?? false }).ToArray();
			}

			TestType<BooleanTable, bool>(db, data);
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

			TestType<IntEnumTable, IntEnum>(db, IntEnumTable.Data);
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

			TestType<StringEnumTable, StringEnum>(db, StringEnumTable.Data);
		}
		#endregion

		#region Test Implementation
		[Sql.Expression("{0} = {1}", IsPredicate = true)]
		private static bool Equality(object? x, object? y) => throw new NotImplementedException();

		private void TestType<TTable, TType>(DataConnection db, TTable[] data)
			where TTable: TypeTable<TType>
			where TType: struct
		{
			using var table = db.CreateLocalTable(data);

			// test parameter
			db.InlineParameters = false;
			var record = table.Where(r => Equality(r.Column, data[1].Column) && Equality(r.ColumnNullable, data[1].ColumnNullable)).ToArray()[0];
			Assert.AreEqual(2, record.Id);
			Assert.AreEqual(data[1].Column, record.Column);
			Assert.AreEqual(data[1].ColumnNullable, record.ColumnNullable);
			Assert.AreEqual(2, db.LastParameters?.Count);

			// test literal
			db.InlineParameters = true;
			record = table.Where(r => Equality(r.Column, data[1].Column) && Equality(r.ColumnNullable, data[1].ColumnNullable)).ToArray()[0];
			Assert.AreEqual(2, record.Id);
			Assert.AreEqual(data[1].Column, record.Column);
			Assert.AreEqual(data[1].ColumnNullable, record.ColumnNullable);
			Assert.AreEqual(0, db.LastParameters?.Count);
			db.InlineParameters = false;

			// test bulk copy
			TestBulkCopy<TTable, TType>(db, data, table, BulkCopyType.RowByRow);
			TestBulkCopy<TTable, TType>(db, data, table, BulkCopyType.MultipleRows);
			TestBulkCopy<TTable, TType>(db, data, table, BulkCopyType.ProviderSpecific);
		}

		private static void TestBulkCopy<TTable, TType>(DataConnection db, TTable[] data, TempTable<TTable> table, BulkCopyType bulkCopyType)
			where TTable : TypeTable<TType>
			where TType : struct
		{
			table.Delete();
			db.BulkCopy(new BulkCopyOptions() { BulkCopyType = bulkCopyType }, data);
			var records = table.OrderBy(r => r.Id).ToArray();
			Assert.AreEqual(2, records.Length);
			Assert.AreEqual(data[0].Id, records[0].Id);
			Assert.AreEqual(data[0].Column, records[0].Column);
			Assert.AreEqual(data[0].ColumnNullable, records[0].ColumnNullable);
			Assert.AreEqual(data[1].Id, records[1].Id);
			Assert.AreEqual(data[1].Column, records[1].Column);
			Assert.AreEqual(data[1].ColumnNullable, records[1].ColumnNullable);
		}
		#endregion
	}
}
