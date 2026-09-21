using System;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.DataProvider
{
	/*
	 * https://learn.microsoft.com/en-us/office/client-developer/access/desktop-database-reference/equivalent-ansi-sql-data-types
	 *
	 * The DDL spellings below are the ones Data/Create Scripts/Access.sql already uses for AllTypes, so
	 * every Access engine in the matrix is known to accept them.
	 *
	 * Uncovered:
	 * - COUNTER: an identity column, which TypeTestsBase cannot write
	 * - OLEOBJECT: the same LONGBINARY storage as IMAGE, covered by TestImage
	 */
	[TestFixture]
	public sealed class AccessTypeTests : TypeTestsBase
	{
		sealed class AccessDataSourcesAttribute : IncludeDataSourcesAttribute
		{
			public AccessDataSourcesAttribute()
				: base(TestProvName.AllAccess)
			{
			}
		}

		protected override string? TypeTableName => "TypeTests";

		#region Numeric types

		[Test]
		public async ValueTask TestByte([AccessDataSources] string context)
		{
			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), byte.MaxValue, byte.MinValue);
		}

		[Test]
		public async ValueTask TestSmallInt([AccessDataSources] string context)
		{
			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), default, default);
			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), short.MaxValue, short.MinValue);
		}

		[Test]
		public async ValueTask TestInteger([AccessDataSources] string context)
		{
			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), default, default);
			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), int.MaxValue, int.MinValue);
		}

		[Test]
		public async ValueTask TestSingle([AccessDataSources] string context)
		{
			await TestType<float, float?>(context, new(typeof(float), DataType.Single), default, default);
			await TestType<float, float?>(context, new(typeof(float), DataType.Single), 1.25f, -1.25f);
		}

		[Test]
		public async ValueTask TestDouble([AccessDataSources] string context)
		{
			await TestType<double, double?>(context, new(typeof(double), DataType.Double), default, default);
			await TestType<double, double?>(context, new(typeof(double), DataType.Double), 1.25d, -1.25d);
		}

		[Test]
		public async ValueTask TestCurrency([AccessDataSources] string context)
		{
			// CURRENCY is a scaled 64-bit integer with exactly four decimal places
			await TestType<decimal, decimal?>(context, new(typeof(decimal), DataType.Money), default, default);
			await TestType<decimal, decimal?>(context, new(typeof(decimal), DataType.Money), 1.2345m, -1.2345m);
		}

		[Test]
		public async ValueTask TestDecimal([AccessDataSources] string context)
		{
			await TestType<decimal, decimal?>(context, new(typeof(decimal), DataType.Decimal, null, null, 10, 4), default, default);
			await TestType<decimal, decimal?>(context, new(typeof(decimal), DataType.Decimal, null, null, 10, 4), 1.2345m, -1.2345m);
		}

		#endregion

		#region Boolean

		[Test]
		public async ValueTask TestBit([AccessDataSources] string context)
		{
			// an Access YESNO column cannot hold NULL - it stores False - so the nullable half is not testable
			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), default, default, skipNullable: true);
			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), true, default, skipNullable: true);
		}

		#endregion

		#region Date and time

		[Test]
		public async ValueTask TestDateTime([AccessDataSources] string context)
		{
			// Access DATETIME has no sub-second precision
			var value    = new DateTime(2012, 12, 12, 12, 12, 12);
			var nullable = new DateTime(2000,  1,  1,  0,  0,  0);

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), value, nullable);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), nullable, value);
		}

		#endregion

		#region Guid

		// ODBC is excluded because AccessODBCSqlBuilder.BuildValue forces every GUID to a parameter, so there
		// is no literal mode to assert - the same reason DataTypesTests passes supportLiterals: !Odbc
		[Test]
		public async ValueTask TestGuid([IncludeDataSources(TestProvName.AllAccessOleDb, TestProvName.AllAccessLibRed)] string context)
		{
			var value    = new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF");
			var nullable = new Guid("00000000-0000-0000-0000-000000000001");

			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Guid), value, nullable);
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Guid), nullable, value);
		}

		#endregion

		#region Text types

		[Test]
		public async ValueTask TestChar([AccessDataSources] string context)
		{
			// LibRed reports CLR type names from GetDataTypeName, so the fixed-width column cannot be told
			// from a VARCHAR at read time and its padding is not trimmed - see the LibRed provider notes
			var padded = context.IsAnyOf(TestProvName.AllAccessLibRed);

			await TestType<string, string?>(context, new(typeof(string), DataType.Char, null, 10), "ab", "cd",
				filterByValue           : !padded,
				filterByNullableValue   : !padded,
				getExpectedValue        : v => padded ? v.PadRight(10) : v,
				getExpectedNullableValue: v => padded ? v?.PadRight(10) : v);
		}

		[Test]
		public async ValueTask TestVarChar([AccessDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.VarChar, null, 20), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string), DataType.VarChar, null, 20), "value", "other");
		}

		[Test]
		public async ValueTask TestNVarChar([AccessDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar, null, 20), "значение", "другое");
		}

		[Test]
		public async ValueTask TestMemo([AccessDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.NText), "value", "other");
		}

		#endregion

		#region Binary types

		[Test]
		public async ValueTask TestBinary([AccessDataSources] string context)
		{
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Binary, null, 4), [1, 2, 3, 4], [5, 6, 7, 8], filterByValue: false, filterByNullableValue: false);
		}

		[Test]
		public async ValueTask TestVarBinary([AccessDataSources] string context)
		{
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.VarBinary, null, 10), [1, 2, 3, 4], [5, 6, 7, 8], filterByValue: false, filterByNullableValue: false);
		}

		[Test]
		public async ValueTask TestImage([AccessDataSources] string context)
		{
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Image), [1, 2, 3, 4], [5, 6, 7, 8], filterByValue: false, filterByNullableValue: false);
		}

		#endregion
	}
}
