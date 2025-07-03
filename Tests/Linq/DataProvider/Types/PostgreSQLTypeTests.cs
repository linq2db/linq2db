using System;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;

using Npgsql;

using NUnit.Framework;

namespace Tests.DataProvider
{
	// TODO: add more types
	/*
	 * NOTES:
	 * 1. for JSON and JSONB mappings we don't have default mapping (when DataType is not set) as we cannot prefer JSON over JSONB
	 * 2. not all supported by Npgsql types for JSON(B) are tested currently, e.g. no Newtowsoft.Json types and Utf8JsonReader tests
	 */
	public sealed  class PostgreSQLTypeTests : TypeTestsBase
	{
		[Test]
		public async ValueTask TestJsonTypes([IncludeDataSources(false, TestProvName.AllPostgreSQL95Plus)] string context, [Values(DataType.Json, DataType.BinaryJson)] DataType dataType)
		{
			// https://www.postgresql.org/docs/current/datatype-json.html
			// https://www.npgsql.org/doc/types/json.html
			const string json1 = /*lang=json,strict*/ "{\"x\": 1, \"y\": {\"a\": null, \"b\": \"тест\", \"w\": [1, null, \"qqq\", true], \"z\": true}}";
			const string json2 = /*lang=json,strict*/ "{\"4454\": {\"\": true, \"b\": \"тест\", \"w\": [-1, false, \"qqdfg q\", true], \"null\": null}, \"тест\": 1}";

			var doubleValue = "-12e34";
			var doubleValueJsonB = "-120000000000000000000000000000000000";

			// mapping to string
			await Test<string>(
				v => v,
				v => v == doubleValue && dataType == DataType.BinaryJson ? doubleValueJsonB : v,
				v => v);

			// TODO: postpone till type system refactoring
			// For mappings below we need to register reader to be able to read data into mapping
			// For now we skip it and allow users to register their own converter as workaround
			// proper fix should be around provider's GetReaderExpression, but we don't have db type information from mapping there currently

			//// mapping to JsonDocument
			//await Test<JsonDocument>(
			//	v => v == null ? null : JsonDocument.Parse(v),
			//	v => v,
			//	v => v);

			//// mapping to JsonElement
			//await TestStruct<JsonElement>(v => v == null ? null : JsonDocument.Parse(v).RootElement);

			async ValueTask Test<TType>(
				Func<string?, TType?> prepareValue,
				Func<TType, TType> getExpectedValue,
				Func<TType?, TType?> getExpectedNullableValue)
				where TType: class
			{
				var dbType = new DbDataType(typeof(TType), dataType);

				// primitives: null
				await TestType<TType, TType?>(context, dbType, prepareValue("null")!, prepareValue("null"));

				// primitives: boolean
				await TestType<TType, TType?>(context, dbType, prepareValue("true")!, prepareValue("false"));

				// primitives: number
				await TestType<TType, TType?>(context, dbType, prepareValue("12")!, prepareValue("-34"));

				// primitives: number with fraction/exponent
				await TestType<TType, TType?>(context, dbType, prepareValue(doubleValue)!, prepareValue("34.12"), getExpectedValue: getExpectedValue);

				// primitives: string
				await TestType<TType, TType?>(context, dbType, prepareValue("\"тест\"")!, prepareValue("\"\""));

				// empty document
				await TestType<TType, TType?>(context, dbType, prepareValue("null")!, prepareValue(null));

				// array (unified)
				await TestType<TType, TType?>(context, dbType, prepareValue("[1, 2, 3]")!, prepareValue("[true, false]"));

				// object with all types and mixed arrays
				await TestType<TType, TType?>(context, dbType, prepareValue(json1)!, prepareValue(json2));
			}

			//async ValueTask TestStruct<TType>(Func<string?, TType?> prepareValue)
			//	where TType : struct
			//{
			//	var dbType = new DbDataType(typeof(TType), dataType);

			//	// primitives: null
			//	await TestType<TType, TType?>(context, dbType, prepareValue("null")!.Value, prepareValue("null"));

			//	// primitives: boolean
			//	await TestType<TType, TType?>(context, dbType, prepareValue("true")!.Value, prepareValue("false"));

			//	// primitives: number
			//	await TestType<TType, TType?>(context, dbType, prepareValue("12")!.Value, prepareValue("-34"));

			//	// primitives: number with fraction/exponent
			//	await TestType<TType, TType?>(context, dbType, prepareValue(doubleValue)!.Value, prepareValue("34.12"));

			//	// primitives: string
			//	await TestType<TType, TType?>(context, dbType, prepareValue("\"тест\"")!.Value, prepareValue("\"\""));

			//	// empty document
			//	await TestType<TType, TType?>(context, dbType, prepareValue("null")!.Value, prepareValue(null));

			//	// array (unified)
			//	await TestType<TType, TType?>(context, dbType, prepareValue("[1, 2, 3]")!.Value, prepareValue("[true, false]"));

			//	// object with all types and mixed arrays
			//	await TestType<TType, TType?>(context, dbType, prepareValue(json1)!.Value, prepareValue(json2));
			//}
		}

		sealed class Poco
		{
			public int? X { get; set; }
			public bool? Ы { get; set; }
			public string? String { get; set; }
			public double? Double { get; set; }
			public float? Float { get; set; }
			public decimal? Decimal { get; set; }
			public string?[]? Array { get; set; }
		}

		[ActiveIssue("Lack of reader support, see comment in TestJSONTypes")]
		[Test]
		public async ValueTask TestJsonPocoType([IncludeDataSources(false, TestProvName.AllPostgreSQL)] string context, [Values(DataType.Json, DataType.BinaryJson)] DataType dataType)
		{
			var pocoNull = new Poco();

			var poco1 = new Poco()
			{
				X = -123,
				Ы = true,
				String = "dfgsfg",
				Double = -123e42,
				Float = 32e-3F,
				Decimal = -12.34M,
				Array = ["one", null, "three"]
			};

			var poco2 = new Poco()
			{
				X = int.MaxValue,
				Ы = false,
				String = "варув",
				Double = 123e-42,
				Float = -1.122e-3F,
				Decimal = 0.12345M,
				Array = ["вапвыа", null, "w49fguw4-"]
			};

			var dbType = new DbDataType(typeof(Poco), dataType);

			var builder = new NpgsqlDataSourceBuilder(GetConnectionString(context));
			// all for this line
			builder.EnableDynamicJson();
			var dataSource = builder.Build();

			await TestType<Poco, Poco?>(context, dbType, pocoNull, null, optionsBuilder: OptionsBuilder);

			await TestType<Poco, Poco?>(context, dbType, poco1, poco2, optionsBuilder: OptionsBuilder);

			DataOptions OptionsBuilder(DataOptions o) => o.UseConnectionFactory(GetDataProvider(context), _ => dataSource.CreateConnection());
		}
	}
}
