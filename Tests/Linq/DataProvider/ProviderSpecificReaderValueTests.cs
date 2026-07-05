#if !NETFRAMEWORK

using System;
using System.Data.SqlTypes;
using System.Globalization;

using LinqToDB.Data;

using Microsoft.Data.SqlTypes;
using Microsoft.SqlServer.Types;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class ProviderSpecificReaderValueTests : DataProviderTestBase
	{
		[Test]
		public void SqlServerProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "Cast(1 as bit)"                                      , typeof(SqlBoolean), typeof(bool)    , "true" );
				AssertReadMatrix(conn, "Cast(255 as tinyint)"                                , typeof(SqlByte)   , typeof(byte)    , "255"  );
				AssertReadMatrix(conn, "Cast(32767 as smallint)"                             , typeof(SqlInt16)  , typeof(short)   , "32767");
				AssertReadMatrix(conn, "Cast(2147483647 as int)"                             , typeof(SqlInt32)  , typeof(int)     , "2147483647");
				AssertReadMatrix(conn, "Cast(9223372036854775807 as bigint)"                 , typeof(SqlInt64)  , typeof(long)    , "9223372036854775807");
				AssertReadMatrix(conn, "Cast(1.25 as real)"                                  , typeof(SqlSingle) , typeof(float)   , "1.25" );
				AssertReadMatrix(conn, "Cast(1.25 as float)"                                 , typeof(SqlDouble) , typeof(double)  , "1.25" );
				AssertReadMatrix(conn, "Cast(12.34 as money)"                                , typeof(SqlMoney)  , typeof(decimal) , "12.34", "12.3400");
				AssertReadMatrix(conn, "Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)", typeof(SqlGuid), typeof(Guid), "6f9619ff-8b86-d011-b42d-00c04fc964ff");
				AssertReadMatrix(conn, "Cast(N'text' as nvarchar(10))"                       , typeof(SqlString) , typeof(string)  , "text" );
				AssertReadMatrix(conn, "Cast(0x010203 as varbinary(3))"                      , typeof(SqlBinary) , typeof(byte[])  , "AQID" );
				AssertReadMatrix(conn, "Cast('<root />' as xml)"                             , typeof(SqlXml)    , typeof(string)  , "<root />");
				AssertReadMatrix(conn, "Cast('2026-07-05T12:34:56' as datetime)"             , typeof(SqlDateTime), typeof(DateTime), "2026-07-05T12:34:56.0000000");
				AssertProviderSpecificRequired(conn, "Cast(1.222222222222222222222222222222 as decimal(31,30))", typeof(SqlDecimal), "1.222222222222222222222222222222");
			}
		}

		[Test]
		public void SqlServerProviderSpecificReadMatrix2008Plus([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "Cast('2026-07-05' as date)"                         , typeof(DateTime), typeof(DateTime), "2026-07-05T00:00:00.0000000");
				AssertReadMatrix(conn, "Cast('2026-07-05T12:34:56.1234567' as datetime2(7))", typeof(DateTime), typeof(DateTime), "2026-07-05T12:34:56.1234567");
				AssertReadMatrix(conn, "Cast('12:34:56.1234567' as time(7))"                , typeof(TimeSpan), typeof(TimeSpan), "12:34:56.1234567");
				AssertReadMatrix(conn, "Cast('2026-07-05T12:34:56.1234567+03:00' as datetimeoffset(7))", typeof(DateTimeOffset), typeof(DateTimeOffset), "2026-07-05T12:34:56.1234567+03:00");

				if (context.IsAnyOf(TestProvName.AllSqlServerMS))
				{
					AssertBothReadsFail(conn, "Cast('/1/3/' as hierarchyid)", "Microsoft.SqlServer.Server.InvalidUdtException");
				}
				else
				{
					AssertReadMatrix(conn, "Cast('/1/3/' as hierarchyid)", typeof(SqlHierarchyId), typeof(SqlHierarchyId), "/1/3/");
				}
			}
		}

		[Test]
		public void SqlServerVectorProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "Cast('[1.0,2.0,3.0]' as vector(3))", typeof(SqlVector<float>), typeof(SqlVector<float>), "[1,2,3]");
			}
		}

		[Test]
		public void SqlServerSpatialProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (context.IsAnyOf(TestProvName.AllSqlServerMS))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "geometry::STGeomFromText('LINESTRING (100 100, 20 180, 180 180)', 0)",            typeof(SqlGeometry),  typeof(SqlGeometry) , "LINESTRING (100 100, 20 180, 180 180)");
				AssertReadMatrix(conn, "geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656)', 4326)", typeof(SqlGeography), typeof(SqlGeography), "LINESTRING (-122.36 47.656, -122.343 47.656)");
			}
		}

		void AssertReadMatrix(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, Type expectedGetValueType, string expectedString)
		{
			AssertReadMatrix(conn, sqlExpression, expectedProviderSpecificType, expectedGetValueType, expectedString, expectedString);
		}

		void AssertReadMatrix(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, Type expectedGetValueType, string expectedProviderSpecificString, string expectedGetValueString)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(providerSpecific.ExceptionTypeName, Is.Null, sqlExpression);
				Assert.That(getValue.ExceptionTypeName, Is.Null, sqlExpression);
				Assert.That(providerSpecific.Type, Is.EqualTo(expectedProviderSpecificType), sqlExpression);
				Assert.That(getValue.Type, Is.EqualTo(expectedGetValueType), sqlExpression);
				Assert.That(providerSpecific.StringValue, Is.EqualTo(expectedProviderSpecificString), sqlExpression);
				Assert.That(getValue.StringValue, Is.EqualTo(expectedGetValueString), sqlExpression);
			}
		}

		void AssertProviderSpecificRequired(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, string expectedString)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(providerSpecific.ExceptionTypeName, Is.Null, sqlExpression);
				Assert.That(providerSpecific.Type, Is.EqualTo(expectedProviderSpecificType), sqlExpression);
				Assert.That(providerSpecific.StringValue, Is.EqualTo(expectedString), sqlExpression);
				Assert.That(getValue.ExceptionTypeName, Is.Not.Null, sqlExpression);
			}
		}

		void AssertBothReadsFail(DataConnection conn, string sqlExpression, string expectedExceptionTypeName)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(providerSpecific.ExceptionTypeName, Is.EqualTo(expectedExceptionTypeName), sqlExpression);
				Assert.That(getValue.ExceptionTypeName, Is.EqualTo(expectedExceptionTypeName), sqlExpression);
			}
		}

		ReadResult ReadValue(DataConnection conn, string sqlExpression, bool providerSpecific)
		{
			using var result = conn.ExecuteReader("SELECT " + sqlExpression);
			var reader       = result.Reader ?? throw new InvalidOperationException("Reader is not available.");

			Assert.That(reader.Read(), Is.True);

			try
			{
				var value = providerSpecific ? reader.GetProviderSpecificValue(0) : reader.GetValue(0);

				return new ReadResult(value.GetType(), value.GetType().FullName, ConvertValueToString(value), null);
			}
			catch (Exception exception)
			{
				return new ReadResult(null, null, null, exception.GetType().FullName);
			}
		}

		static string? ConvertValueToString(object value)
		{
			return value switch
			{
				SqlBoolean sqlBoolean         => sqlBoolean.Value ? "true" : "false",
				bool boolValue                => boolValue ? "true" : "false",
				SqlSingle sqlSingle           => sqlSingle.Value.ToString("R", CultureInfo.InvariantCulture),
				float singleValue             => singleValue.ToString("R", CultureInfo.InvariantCulture),
				SqlDouble sqlDouble           => sqlDouble.Value.ToString("R", CultureInfo.InvariantCulture),
				double doubleValue            => doubleValue.ToString("R", CultureInfo.InvariantCulture),
				SqlDateTime sqlDateTime       => sqlDateTime.Value.ToString("O", CultureInfo.InvariantCulture),
				DateTime dateTime             => dateTime.ToString("O", CultureInfo.InvariantCulture),
				DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
				TimeSpan timeSpan             => timeSpan.ToString("c", CultureInfo.InvariantCulture),
				SqlGuid sqlGuid               => sqlGuid.Value.ToString("D"),
				Guid guid                     => guid.ToString("D"),
				SqlBinary sqlBinary           => Convert.ToBase64String(sqlBinary.Value),
				byte[] bytes                  => Convert.ToBase64String(bytes),
				SqlXml sqlXml                 => sqlXml.Value,
				SqlVector<float> vector       => ConvertVectorToString(vector.Memory.ToArray()),
				SqlVector<Half> vector        => ConvertVectorToString(vector.Memory.ToArray()),
				_                             => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static string ConvertVectorToString<T>(T[] vector)
		{
			var builder = new System.Text.StringBuilder();

			builder.Append('[');
			for (var i = 0; i < vector.Length; i++)
			{
				if (i > 0)
					builder.Append(',');

				builder.Append(Convert.ToString(vector[i], CultureInfo.InvariantCulture));
			}

			builder.Append(']');
			return builder.ToString();
		}

		sealed record ReadResult(Type? Type, string? TypeName, string? StringValue, string? ExceptionTypeName);
	}
}

#endif
