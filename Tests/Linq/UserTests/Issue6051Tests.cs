using System;
using System.Text;

using LinqToDB;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.Oracle;

#if !NETFRAMEWORK
using LinqToDB.DataProvider.DuckDB;
using LinqToDB.DataProvider.Ydb;
#endif

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	// Special floating-point values through the provider code paths rewritten for #6051.
	// The JIT fault itself is guarded by Tests/Tests.FpuJit; these pin the produced values.
	[TestFixture]
	public class Issue6051Tests : TestBase
	{
		// version-pinned factories: the unversioned ones auto-detect by opening a connection
		static IDataProvider GetProvider(string provider) => provider switch
		{
			"Oracle"   => OracleTools.GetDataProvider(OracleVersion.v12, OracleProvider.Managed),
			"Firebird" => FirebirdTools.GetDataProvider(FirebirdVersion.v5),
#if !NETFRAMEWORK
			"DuckDB"   => DuckDBTools.GetDataProvider(),
			"Ydb"      => YdbTools.GetDataProvider(),
#endif
			_          => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
		};

		static string Literal(IDataProvider provider, object value)
		{
			var sb = new StringBuilder();

			provider.MappingSchema.ValueToSqlConverter.Convert(sb, provider.MappingSchema, new DataOptions(), value);

			return sb.ToString();
		}

		[TestCase("Oracle",   "BINARY_DOUBLE_NAN", "BINARY_DOUBLE_INFINITY", "-BINARY_DOUBLE_INFINITY", "1.5D")]
		[TestCase("Firebird", "LOG(1, 1)",         "LOG(1, 2)",              "LOG(1, 0.5)",             "1.5")]
#if !NETFRAMEWORK
		[TestCase("DuckDB",   "'NaN'::DOUBLE",     "'Infinity'::DOUBLE",     "'-Infinity'::DOUBLE",     "1.5")]
		[TestCase("Ydb",      "Double('NaN')",     "Double('inf')",          "Double('-inf')",          "Double('1.5')")]
#endif
		public void DoubleLiterals(string provider, string nan, string positiveInfinity, string negativeInfinity, string finite)
		{
			var dataProvider = GetProvider(provider);

			Literal(dataProvider, double.NaN).ShouldBe(nan);
			Literal(dataProvider, double.PositiveInfinity).ShouldBe(positiveInfinity);
			Literal(dataProvider, double.NegativeInfinity).ShouldBe(negativeInfinity);
			Literal(dataProvider, 1.5d).ShouldBe(finite);
		}

		[TestCase("Oracle",   "BINARY_FLOAT_NAN",         "BINARY_FLOAT_INFINITY",    "-BINARY_FLOAT_INFINITY",     "1.5")]
		[TestCase("Firebird", "CAST(LOG(1, 1) AS FLOAT)", "CAST(LOG(1, 2) AS FLOAT)", "CAST(LOG(1, 0.5) AS FLOAT)", "1.5")]
#if !NETFRAMEWORK
		[TestCase("DuckDB",   "'NaN'::FLOAT",             "'Infinity'::FLOAT",        "'-Infinity'::FLOAT",         "'1.5'::FLOAT")]
		[TestCase("Ydb",      "Float('NaN')",             "Float('inf')",             "Float('-inf')",              "Float('1.5')")]
#endif
		public void FloatLiterals(string provider, string nan, string positiveInfinity, string negativeInfinity, string finite)
		{
			var dataProvider = GetProvider(provider);

			Literal(dataProvider, float.NaN).ShouldBe(nan);
			Literal(dataProvider, float.PositiveInfinity).ShouldBe(positiveInfinity);
			Literal(dataProvider, float.NegativeInfinity).ShouldBe(negativeInfinity);
			Literal(dataProvider, 1.5f).ShouldBe(finite);
		}

		[Test]
		public void ClickHouseMySqlStringToDouble()
		{
			var mappingSchema = ClickHouseTools.GetDataProvider(ClickHouseProvider.MySqlConnector).MappingSchema;
			var convert       = mappingSchema.GetConvertExpression<string, double>(checkNull: false, createDefault: false)!.Compile();

			BitConverter.DoubleToInt64Bits(convert("nan")).ShouldBe(BitConverter.DoubleToInt64Bits(double.NaN));
			convert("inf").ShouldBe(double.PositiveInfinity);
			convert("-inf").ShouldBe(double.NegativeInfinity);
			convert("1.5").ShouldBe(1.5d);
		}

		[Test]
		public void ClickHouseMySqlStringToFloat()
		{
			var mappingSchema = ClickHouseTools.GetDataProvider(ClickHouseProvider.MySqlConnector).MappingSchema;
			var convert       = mappingSchema.GetConvertExpression<string, float>(checkNull: false, createDefault: false)!.Compile();

			BitConverter.GetBytes(convert("nan")).ShouldBe(BitConverter.GetBytes(float.NaN));
			convert("inf").ShouldBe(float.PositiveInfinity);
			convert("-inf").ShouldBe(float.NegativeInfinity);
			convert("1.5").ShouldBe(1.5f);
		}
	}
}
