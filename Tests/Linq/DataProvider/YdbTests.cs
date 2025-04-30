using System;
using System.Globalization;
using System.Reflection;
using System.Text;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class YdbTests : DataProviderTestBase

	{
		#region MappingSchemaTests
		//------------------------------------------------------------------
		//  ADAPTER
		//------------------------------------------------------------------
		[Test]
		public void GetInstance_ShouldCreateAdapterSuccessfully()
		{
			// Act
			var adapter = YdbProviderAdapter.GetInstance();

			// Assert
			Assert.That(adapter, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(adapter.ConnectionType, Is.Not.Null);
				Assert.That(adapter.DataReaderType, Is.Not.Null);
				Assert.That(adapter.ParameterType, Is.Not.Null);
				Assert.That(adapter.CommandType, Is.Not.Null);
				Assert.That(adapter.MappingSchema, Is.Not.Null);
			});
		}
		#endregion

		#region MappingSchemaTests
		//------------------------------------------------------------------
		//  SCALAR-TYPES
		//------------------------------------------------------------------
		[Test]
		public void MappingSchema_ShouldMapBasicDotNetTypes()
		{
			var schema = YdbMappingSchema.Instance;
			Assert.That(schema, Is.Not.Null);

			Assert.Multiple(() =>
			{
				Assert.That(schema.GetDataType(typeof(string)).Type.DataType, Is.EqualTo(DataType.VarChar));
				Assert.That(schema.GetDataType(typeof(bool)).Type.DataType, Is.EqualTo(DataType.Boolean));
				Assert.That(schema.GetDataType(typeof(Guid)).Type.DataType, Is.EqualTo(DataType.Guid));
				Assert.That(schema.GetDataType(typeof(byte[])).Type.DataType, Is.EqualTo(DataType.VarBinary));
				Assert.That(schema.GetDataType(typeof(TimeSpan)).Type.DataType, Is.EqualTo(DataType.Interval));
			});
		}

		//------------------------------------------------------------------
		//  DECIMAL-LITERAL
		//------------------------------------------------------------------
		[Test]
		public void DecimalLiteralBuilder_ShouldRespectSqlDataTypeDefaults()
		{
			var mi = typeof(YdbMappingSchema)
					.GetMethod("BuildDecimalLiteral", BindingFlags.Static | BindingFlags.NonPublic)!;

			var sb  = new StringBuilder();
			var val = 123.45m;

			// SqlDataType by default Precision=29, Scale=10.
			var sqlType = new SqlDataType(DataType.Decimal, typeof(decimal));

			mi.Invoke(null, new object[] { sb, val, sqlType });

			var expected = $"Decimal(\"{val.ToString(CultureInfo.InvariantCulture)}\", {sqlType.Type.Precision}, {sqlType.Type.Scale})";

			Assert.That(sb.ToString(), Is.EqualTo(expected));
		}
		#endregion
	}
}
