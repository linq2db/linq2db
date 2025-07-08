using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using Microsoft.Data.SqlTypes;

using NUnit.Framework;

namespace Tests.DataProvider
{
	// TODO: add more types
	public sealed  class SqlServerTypeTests : TypeTestsBase
	{
		[Test]
		public async ValueTask TestJSONType([IncludeDataSources(TestProvName.AllSqlAzure, TestProvName.AllSqlAzureMi)] string context)
		{
			// TODO: maybe add streaming testing
			// https://github.com/dotnet/SqlClient/pull/2801

			// type limitations:
			// - CAST to JSON for literals could be required (not implemented yet, as we still need to find valid case where it will help)
			// - comparisons is not supported: SqlException: The JSON data type cannot be compared or sorted, except when using the IS NULL operator
			// - parameters doesn't work yet (MDS 6.0.1 and servers tested on 27/01/2025)
			// - RowByRow bulk copy doesn't work yet as it uses parameters
			// - BulkCopy (native) doesn't work for SQLMI
			// - SQLMI returns wrong type (varchar) for json columns

			// to support both custom types and parameters MDS v6+ required:
			// - types are defined and supported by this provider only
			// - parameter type SqlDbType.Json supported by this provider only
			// (older MDS and SDC versions could be used with JSON type in inline parameters mode)
			var sqlJsonSupported      = context.IsAnyOf(TestProvName.SqlAzureMS, TestProvName.SqlAzureMiMS);
			var parametersSupported   = context.IsAnyOf(TestProvName.SqlAzureMS, TestProvName.SqlAzureMiMS);
			var jsonDocumentSupported = context.IsAnyOf(TestProvName.SqlAzureMS);

			// see https://github.com/dotnet/SqlClient/discussions/3136
			parametersSupported = false;

			const string json1 = /*lang=json,strict*/ "{ \"prop1\": 123 }";
			const string json2 = /*lang=json,strict*/ "{ \"prop1\": 321 }";

			// documents are normalized by server
			const string expectedEmpty = "{}";
			const string expected1 = /*lang=json,strict*/ "{\"prop1\":123}";
			const string expected2 = /*lang=json,strict*/ "{\"prop1\":321}";

			// disable RowByRow as it uses parameters
			bool testBulkCopyType(BulkCopyType bc) => !sqlJsonSupported || bc != BulkCopyType.RowByRow;

			// string
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "{ }", default, testParameters: parametersSupported, filterByValue: false, getExpectedValue: _ => expectedEmpty, testBulkCopyType: bc => testBulkCopyType(bc) && !(context.IsAnyOf(TestProvName.AllSqlAzure) && bc == BulkCopyType.ProviderSpecific));
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), json1, json2, testParameters: parametersSupported, filterByValue: false, filterByNullableValue: false, getExpectedValue: _ => expected1, getExpectedNullableValue: _ => expected2, testBulkCopyType: bc => testBulkCopyType(bc) && !(context.IsAnyOf(TestProvName.AllSqlAzure) && bc == BulkCopyType.ProviderSpecific));

			if (sqlJsonSupported)
			{
				await TestType<SqlJson, SqlJson?>(context, new(typeof(SqlJson)), new("{ }"), default, testParameters: parametersSupported, filterByValue: false, isExpectedValue: v => v.Value == expectedEmpty, isExpectedNullableValue: v => v?.IsNull == true, testBulkCopyType: bc => testBulkCopyType(bc) && !(context.IsAnyOf(TestProvName.AllSqlAzure) && bc == BulkCopyType.ProviderSpecific));
				await TestType<SqlJson, SqlJson?>(context, new(typeof(SqlJson)), new("{ }"), SqlJson.Null, testParameters: parametersSupported, filterByValue: false, filterByNullableValue: false, isExpectedValue: v => v.Value == expectedEmpty, isExpectedNullableValue: v => v?.IsNull == true, testBulkCopyType: bc => testBulkCopyType(bc) && !(context.IsAnyOf(TestProvName.AllSqlAzure) && bc == BulkCopyType.ProviderSpecific));
				await TestType<SqlJson, SqlJson?>(context, new(typeof(SqlJson)), new(json1), new(json2), testParameters: parametersSupported, filterByValue: false, filterByNullableValue: false, isExpectedValue: v => v.Value == expected1, isExpectedNullableValue: v => v?.Value == expected2, testBulkCopyType: bc => testBulkCopyType(bc) && !(context.IsAnyOf(TestProvName.AllSqlAzure) && bc == BulkCopyType.ProviderSpecific));
			}
		}
	}
}
