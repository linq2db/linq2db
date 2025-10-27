using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;

using Microsoft.Data.SqlTypes;

using NUnit.Framework;

namespace Tests.DataProvider
{
	// TODO: add more types
	public sealed  class SqlServerTypeTests : TypeTestsBase
	{
		[Test]
		public async ValueTask TestJSONType([IncludeDataSources(TestProvName.AllSqlServer2025Plus)] string context)
		{
			// https://learn.microsoft.com/en-us/sql/t-sql/data-types/json-data-type

			// TODO: maybe add streaming testing
			// https://github.com/dotnet/SqlClient/pull/2801

			// type limitations:
			// - CAST to JSON for literals could be required (not implemented yet, as we still need to find valid case where it will help)
			// - comparisons is not supported: SqlException: The JSON data type cannot be compared or sorted, except when using the IS NULL operator
			// - BulkCopy (native) doesn't work for SQLMI
			// - SQLMI returns wrong type (varchar) for json columns

			// to support both custom types and parameters MDS v6+ required:
			// - types are defined and supported by this provider only
			// - parameter type SqlDbType.Json supported by this provider only
			// (older MDS and SDC versions could be used with JSON type in inline parameters mode)
			var sqlJsonSupported = context.IsAnyOf(TestProvName.AllSqlServerMS);

			const string json1 = /*lang=json,strict*/ "{ \"prop1\": 123 }";
			const string json2 = /*lang=json,strict*/ "{ \"prop1\": 321 }";

			// documents are normalized by server
			const string expectedEmpty = "{}";
			const string expected1 = /*lang=json,strict*/ "{\"prop1\":123}";
			const string expected2 = /*lang=json,strict*/ "{\"prop1\":321}";

			// string
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "{ }", default, filterByValue: false, getExpectedValue: _ => expectedEmpty);
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), json1, json2, filterByValue: false, filterByNullableValue: false, getExpectedValue: _ => expected1, getExpectedNullableValue: _ => expected2);

			if (sqlJsonSupported)
			{
				// JsonDocument supported only by MDS with SqlJson support
				await TestType<JsonDocument, JsonDocument?>(context, new(typeof(JsonDocument), DataType.Json), JsonDocument.Parse("{ }"), default, filterByValue: false, isExpectedValue: v => v.RootElement.GetRawText() == JsonDocument.Parse(expectedEmpty).RootElement.GetRawText());
				await TestType<JsonDocument, JsonDocument?>(context, new(typeof(JsonDocument), DataType.Json), JsonDocument.Parse(json1), JsonDocument.Parse(json2), filterByValue: false, filterByNullableValue: false, isExpectedValue: v => v.RootElement.GetRawText() == JsonDocument.Parse(expected1).RootElement.GetRawText(), isExpectedNullableValue: v => v!.RootElement.GetRawText() == JsonDocument.Parse(expected2).RootElement.GetRawText());

				await TestType<SqlJson, SqlJson?>(context, new(typeof(SqlJson)), new("{ }"), default, filterByValue: false, isExpectedValue: v => v.Value == expectedEmpty, isExpectedNullableValue: v => v?.IsNull == true);
				await TestType<SqlJson, SqlJson?>(context, new(typeof(SqlJson)), new("{ }"), SqlJson.Null, filterByValue: false, filterByNullableValue: false, isExpectedValue: v => v.Value == expectedEmpty, isExpectedNullableValue: v => v?.IsNull == true);
				await TestType<SqlJson, SqlJson?>(context, new(typeof(SqlJson)), new(json1), new(json2), filterByValue: false, filterByNullableValue: false, isExpectedValue: v => v.Value == expected1, isExpectedNullableValue: v => v?.Value == expected2);
			}
		}

		[Test]
		public async ValueTask TestVectorType([IncludeDataSources(TestProvName.AllSqlServer2025Plus)] string context)
		{
			// https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type

			// type limitations:
			// - comparison not supported

			// mappings support currently limited to SqlVector type (and MDS provider)
			// as support for other mappings require some efforts and could be done on request
			// (or will be added to newer MDS/SQL Server releases):
			// - string: requires custom parameter conversion
			// - float[], byte[]: require both parameter and reader conversions

			var msClient = context.IsAnyOf(TestProvName.AllSqlServerMS);

			//var dt = DataType.Array | DataType.Single;

			//const string asString1 = /*lang=json,strict*/ "[1.1, -1.2]";
			//const string asString2 = /*lang=json,strict*/ "[2.1, -3.2]";
			// ???
			//var asString1Expected = msClient ? /*lang=json,strict*/ "[1.1,-1.2]" : /*lang=json,strict*/ "[1.1000000e+000,-1.2000000e+000]";
			//var asString2Expected = msClient ? /*lang=json,strict*/ "[2.1,-3.2]" : /*lang=json,strict*/ "[2.1, -3.2]";
			//var asBinary1 = BitConverter.GetBytes(1.1f).Concat(BitConverter.GetBytes(-1.2f)).ToArray();
			//var asBinary2 = BitConverter.GetBytes(-7.1f).Concat(BitConverter.GetBytes(-4.2f)).ToArray();
			var asArray1 = new float[] { 1.2f, -1.1f };
			var asArray2 = new float[] { 5.2f, -3.1f };
			var asArray3 = new float[] { 11.2f, -4.1f };

			// string
			//await TestType<string, string?>(context, new(typeof(string), dt, null, length: 2), asString1, default, filterByValue: false, getExpectedValue: _ => asString1Expected);
			//await TestType<string, string?>(context, new(typeof(string), dt, null, length: 2), asString2, asString1, filterByValue: false, filterByNullableValue: false, getExpectedValue: _ => asString2Expected, getExpectedNullableValue: _ => asString1Expected);

			// byte[]
			//await TestType<byte[], byte[]?>(context, new(typeof(byte[]), dt, null, length: 2), asBinary1, default, filterByValue: false);
			//await TestType<byte[], byte[]?>(context, new(typeof(byte[]), dt, null, length: 2), asBinary2, asBinary1, filterByValue: false, filterByNullableValue: false);

			// float[]
			//await TestType<float[], float[]?>(context, new(typeof(float[]), dt, null, length: 2), asArray1, default, filterByValue: false);
			//await TestType<float[], float[]?>(context, new(typeof(float[]), dt, null, length: 2), asArray2, asArray1, filterByValue: false, filterByNullableValue: false);

			if (msClient)
			{
				var type = new DbDataType(typeof(SqlVector<float>)).WithLength(2);
				var sqlVector1 = new SqlVector<float>(asArray1.AsMemory());
				var sqlVector2 = new SqlVector<float>(asArray2.AsMemory());
				var sqlVector3 = new SqlVector<float>(asArray3.AsMemory());

				await TestType<SqlVector<float>, SqlVector<float>?>(context, type, sqlVector1, default, filterByValue: false, isExpectedValue: v => Enumerable.SequenceEqual(v.Memory.ToArray(), sqlVector1.Memory.ToArray()));

				await TestType<SqlVector<float>, SqlVector<float>?>(context, type, sqlVector2, SqlVector<float>.Null, filterByValue: false, isExpectedValue: v => Enumerable.SequenceEqual(v.Memory.ToArray(), sqlVector2.Memory.ToArray()));

				await TestType<SqlVector<float>, SqlVector<float>?>(context, type, sqlVector3, sqlVector2, filterByValue: false, filterByNullableValue: false, isExpectedValue: v => Enumerable.SequenceEqual(v.Memory.ToArray(), sqlVector3.Memory.ToArray()), isExpectedNullableValue: v => v != null && Enumerable.SequenceEqual(v.Value.Memory.ToArray(), sqlVector2.Memory.ToArray()));
			}
		}

		[ActiveIssue("Waiting for SqlClient support")]
		[Test]
		public async ValueTask TestHalfVectorType([IncludeDataSources(TestProvName.AllSqlServer2025Plus)] string context)
		{
			// https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type

			// type limitations:
			// - comparison not supported

			// mappings support currently limited to SqlVector type (and MDS provider)
			// as support for other mappings require some efforts and could be done on request
			// (or will be added to newer MDS/SQL Server releases):
			// - string: requires custom parameter conversion
			// - Half[], byte[]: require both parameter and reader conversions

			var msClient = context.IsAnyOf(TestProvName.AllSqlServerMS);

			//var dt = DataType.Array | DataType.Single;

			//const string asString1 = /*lang=json,strict*/ "[1.1, -1.2]";
			//const string asString2 = /*lang=json,strict*/ "[2.1, -3.2]";
			// ???
			//var asString1Expected = msClient ? /*lang=json,strict*/ "[1.1,-1.2]" : /*lang=json,strict*/ "[1.1000000e+000,-1.2000000e+000]";
			//var asString2Expected = msClient ? /*lang=json,strict*/ "[2.1,-3.2]" : /*lang=json,strict*/ "[2.1, -3.2]";
			//var asBinary1 = BitConverter.GetBytes(1.1f).Concat(BitConverter.GetBytes(-1.2f)).ToArray();
			//var asBinary2 = BitConverter.GetBytes(-7.1f).Concat(BitConverter.GetBytes(-4.2f)).ToArray();
#if !NETFRAMEWORK
			var asArray1 = new Half[] { (Half)1.2f, (Half)(-1.1f) };
			var asArray2 = new Half[] { (Half)5.2f, (Half)(-3.1f) };
			var asArray3 = new Half[] { (Half)11.2f, (Half)(-4.1f) };
#endif

			// string
			//await TestType<string, string?>(context, new(typeof(string), dt, null, length: 2), asString1, default, filterByValue: false, getExpectedValue: _ => asString1Expected);
			//await TestType<string, string?>(context, new(typeof(string), dt, null, length: 2), asString2, asString1, filterByValue: false, filterByNullableValue: false, getExpectedValue: _ => asString2Expected, getExpectedNullableValue: _ => asString1Expected);

			// byte[]
			//await TestType<byte[], byte[]?>(context, new(typeof(byte[]), dt, null, length: 2), asBinary1, default, filterByValue: false);
			//await TestType<byte[], byte[]?>(context, new(typeof(byte[]), dt, null, length: 2), asBinary2, asBinary1, filterByValue: false, filterByNullableValue: false);

			// Half[]
			//await TestType<Half[], Half[]?>(context, new(typeof(Half[]), dt, null, length: 2), asArray1, default, filterByValue: false);
			//await TestType<Half[], Half[]?>(context, new(typeof(Half[]), dt, null, length: 2), asArray2, asArray1, filterByValue: false, filterByNullableValue: false);

			if (msClient)
			{
#if !NETFRAMEWORK
				var type = new DbDataType(typeof(SqlVector<Half>)).WithLength(2);
				var sqlVector1 = new SqlVector<Half>(asArray1.AsMemory());
				var sqlVector2 = new SqlVector<Half>(asArray2.AsMemory());
				var sqlVector3 = new SqlVector<Half>(asArray3.AsMemory());

				await TestType<SqlVector<Half>, SqlVector<Half>?>(context, type, sqlVector1, default, filterByValue: false, isExpectedValue: v => Enumerable.SequenceEqual(v.Memory.ToArray(), sqlVector1.Memory.ToArray()));

				await TestType<SqlVector<Half>, SqlVector<Half>?>(context, type, sqlVector2, SqlVector<Half>.Null, filterByValue: false, isExpectedValue: v => Enumerable.SequenceEqual(v.Memory.ToArray(), sqlVector2.Memory.ToArray()));

				await TestType<SqlVector<Half>, SqlVector<Half>?>(context, type, sqlVector3, sqlVector2, filterByValue: false, filterByNullableValue: false, isExpectedValue: v => Enumerable.SequenceEqual(v.Memory.ToArray(), sqlVector3.Memory.ToArray()), isExpectedNullableValue: v => v != null && Enumerable.SequenceEqual(v.Value.Memory.ToArray(), sqlVector2.Memory.ToArray()));
#endif
			}
		}
	}
}
