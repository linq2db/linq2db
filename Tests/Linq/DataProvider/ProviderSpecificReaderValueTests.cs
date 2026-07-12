#if !NETFRAMEWORK

extern alias MySqlConnector;
extern alias MySqlData;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

using ClickHouse.Driver.Numerics;
using DuckDB.NET.Native;
using FirebirdSql.Data.Types;
using IBM.Data.DB2Types;
using LinqToDB;
using LinqToDB.Data;

using Microsoft.Data.SqlTypes;
using Microsoft.SqlServer.Types;

using NpgsqlTypes;

using NUnit.Framework;
using Oracle.ManagedDataAccess.Types;

using Shouldly;

using MySqlConnectorDecimal = MySqlConnector::MySqlConnector.MySqlDecimal;
using MySqlDataDecimal = MySqlData::MySql.Data.Types.MySqlDecimal;

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
				AssertReadMatrix(conn, "Cast(0x010203 as varbinary(3))"                      , typeof(SqlBinary) , typeof(byte[])  , "0x010203" );
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
				AssertReadMatrix(conn, "Cast('2026-07-05' as date)"                         , typeof(DateTime), typeof(DateTime), "2026-07-05");
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

		[Test]
		public void OracleManagedProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllOracleManaged)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "hextoraw('3039')"                                      , typeof(OracleBinary)     , typeof(byte[])        , "0x3039");
				AssertReadMatrix(conn, "to_blob('3039')"                                      , typeof(OracleBlob)       , typeof(byte[])        , "0x3039");
				AssertReadMatrix(conn, "to_clob('hello, csv')"                                , typeof(OracleClob)       , typeof(string)        , "hello, csv");
				AssertReadMatrix(conn, "to_nclob(N'привет')"                                  , typeof(OracleClob)       , typeof(string)        , "привет");
				AssertReadMatrix(conn, "xmltype('<root><v>1</v></root>')"                     , typeof(OracleXmlType)    , typeof(string)        , "<root><v>1</v></root>");
				AssertReadMatrix(conn, "date '2024-01-02'"                                   , typeof(OracleDate)       , typeof(DateTime)      , "2024-01-02");
				AssertReadMatrix(conn, "timestamp '2024-01-02 03:04:05.123456'"               , typeof(OracleTimeStamp)  , typeof(DateTime)      , "2024-01-02T03:04:05.123456000", "2024-01-02T03:04:05.1234560");
				AssertReadMatrix(conn, "timestamp '2024-01-02 03:04:05.123456 -05:00'"        , typeof(OracleTimeStampTZ), typeof(DateTimeOffset), "2024-01-02T03:04:05.123456000-05:00", "2024-01-02T03:04:05.1234560-05:00");
				AssertReadMatrix(conn, "interval '1-2' year to month"                         , typeof(OracleIntervalYM) , typeof(long)          , "+01-02", "14");
				AssertReadMatrix(conn, "interval '3 04:05:06.789' day to second"              , typeof(OracleIntervalDS) , typeof(TimeSpan)      , "+03 04:05:06.789000", "3.04:05:06.7890000");
				AssertReadMatrix(conn, "bfilename('DATA_PUMP_DIR', 'missing.bin')"            , typeof(OracleBFile)      , typeof(byte[])        , "<BFILE>", providerSpecificOnly: true);
			}
		}

		[Test]
		public void DB2ProviderSpecificReadMatrix([IncludeDataSources(ProviderName.DB2)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(1000000 AS BIGINT) FROM SYSIBM.SYSDUMMY1"                       , typeof(long)          , typeof(long)    , "1000000");
				AssertReadMatrix(conn, "CAST(7777777 AS INTEGER) FROM SYSIBM.SYSDUMMY1"                      , typeof(int)           , typeof(int)     , "7777777");
				AssertReadMatrix(conn, "CAST(100 AS SMALLINT) FROM SYSIBM.SYSDUMMY1"                         , typeof(short)         , typeof(short)   , "100");
				AssertReadMatrix(conn, "CAST(9999999 AS DECIMAL(31,0)) FROM SYSIBM.SYSDUMMY1"                , typeof(decimal)       , typeof(decimal) , "9999999");
				AssertReadMatrix(conn, "CAST(8888888 AS DECFLOAT) FROM SYSIBM.SYSDUMMY1"                     , typeof(decimal)       , typeof(decimal) , "8888888");
				AssertReadMatrix(conn, "CAST(20.31 AS REAL) FROM SYSIBM.SYSDUMMY1"                           , typeof(float)         , typeof(float)   , "20.31");
				AssertReadMatrix(conn, "CAST(16.2 AS DOUBLE) FROM SYSIBM.SYSDUMMY1"                          , typeof(double)        , typeof(double)  , "16.2");
				AssertReadMatrix(conn, "CAST('text' AS VARCHAR(10)) FROM SYSIBM.SYSDUMMY1"                   , typeof(string)        , typeof(string)  , "text");
				AssertReadMatrix(conn, "CAST('2024-01-02' AS DATE) FROM SYSIBM.SYSDUMMY1"                    , typeof(DateTime)      , typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "CAST('03:04:05' AS TIME) FROM SYSIBM.SYSDUMMY1"                      , typeof(TimeSpan)      , typeof(TimeSpan), "03:04:05");
				AssertReadMatrix(conn, "CAST('2024-01-02 03:04:05.123456' AS TIMESTAMP) FROM SYSIBM.SYSDUMMY1", typeof(DateTime)      , typeof(DateTime), "2024-01-02T03:04:05.1234560");
				AssertReadMatrix(conn, "CAST(BX'3039' AS VARBINARY(2)) FROM SYSIBM.SYSDUMMY1"                , typeof(byte[])        , typeof(byte[])  , "0x3039");
				AssertReadMatrix(conn, "BLOB(BX'3039') FROM SYSIBM.SYSDUMMY1"                                , typeof(byte[])        , typeof(byte[])  , "0x3039");
				AssertReadMatrix(conn, "CLOB('hello, csv') FROM SYSIBM.SYSDUMMY1"                            , typeof(string)        , typeof(string)  , "hello, csv");
				AssertReadMatrix(conn, "XMLPARSE(DOCUMENT '<root><v>1</v></root>') FROM SYSIBM.SYSDUMMY1"    , typeof(string)        , typeof(string)  , "<root><v>1</v></root>");
			}
		}

		[Test]
		public void InformixProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(1000000 AS BIGINT) FROM systables WHERE tabid = 1"                             , typeof(long)    , typeof(long)    , "1000000");
				AssertReadMatrix(conn, "CAST(7777777 AS INTEGER) FROM systables WHERE tabid = 1"                            , typeof(int)     , typeof(int)     , "7777777");
				AssertReadMatrix(conn, "CAST(100 AS SMALLINT) FROM systables WHERE tabid = 1"                               , typeof(short)   , typeof(short)   , "100");
				AssertReadMatrix(conn, "CAST(9999999 AS DECIMAL(31,0)) FROM systables WHERE tabid = 1"                      , typeof(decimal) , typeof(decimal) , "9999999");
				AssertReadMatrix(conn, "CAST(20.31 AS REAL) FROM systables WHERE tabid = 1"                                 , typeof(float)   , typeof(float)   , "20.31");
				AssertReadMatrix(conn, "CAST(16.2 AS DOUBLE PRECISION) FROM systables WHERE tabid = 1"                      , typeof(double)  , typeof(double)  , "16.2");
				AssertReadMatrix(conn, "CAST('text' AS VARCHAR(10)) FROM systables WHERE tabid = 1"                         , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "DATE('2024-01-02') FROM systables WHERE tabid = 1"                                  , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "DATETIME(2024-01-02 03:04:05.12345) YEAR TO FRACTION(5) FROM systables WHERE tabid = 1", typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.1234500");
			}
		}

		[Test]
		public void PostgreSQLProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(true AS boolean)"                                                        , typeof(bool)                  , typeof(bool)                  , "true");
				AssertReadMatrix(conn, "CAST(123 AS integer)"                                                        , typeof(int)                   , typeof(int)                   , "123");
				AssertReadMatrix(conn, "CAST(1234567890123 AS bigint)"                                               , typeof(long)                  , typeof(long)                  , "1234567890123");
				AssertReadMatrix(conn, "CAST(123.45 AS numeric(10,2))"                                               , typeof(decimal)               , typeof(decimal)               , "123.45");
				AssertReadMatrix(conn, "CAST(1.25 AS real)"                                                         , typeof(float)                 , typeof(float)                 , "1.25");
				AssertReadMatrix(conn, "CAST(1.25 AS double precision)"                                             , typeof(double)                , typeof(double)                , "1.25");
				AssertReadMatrix(conn, "CAST('text' AS varchar(10))"                                                , typeof(string)                , typeof(string)                , "text");
				AssertReadMatrix(conn, "CAST('2024-01-02' AS date)"                                                 , typeof(DateOnly)              , typeof(DateOnly)              , "2024-01-02");
				AssertReadMatrix(conn, "CAST('03:04:05.123456' AS time)"                                            , typeof(TimeOnly)              , typeof(TimeOnly)              , "03:04:05.1234560");
				AssertReadMatrix(conn, "CAST('2024-01-02 03:04:05.123456' AS timestamp)"                           , typeof(DateTime)              , typeof(DateTime)              , "2024-01-02T03:04:05.1234560");
				AssertReadMatrix(conn, "CAST('1 day 02:03:04.123456' AS interval)"                                  , typeof(TimeSpan)              , typeof(TimeSpan)              , "1.02:03:04.1234560");
				AssertReadMatrix(conn, "CAST('\\x3039' AS bytea)"                                                   , typeof(byte[])                , typeof(byte[])                , "0x3039");
				AssertReadMatrix(conn, "CAST('01234567-89ab-cdef-0123-456789abcdef' AS uuid)"                      , typeof(Guid)                  , typeof(Guid)                  , "01234567-89ab-cdef-0123-456789abcdef");
				AssertReadMatrix(conn, "CAST('1.2.3.4' AS inet)"                                                    , typeof(System.Net.IPAddress)  , typeof(System.Net.IPAddress)  , "1.2.3.4");
				AssertReadMatrix(conn, "point(1, 2)"                                                                , typeof(NpgsqlPoint)           , typeof(NpgsqlPoint)           , "(1,2)");
				AssertReadMatrix(conn, "lseg(point(1,2), point(3,4))"                                               , typeof(NpgsqlLSeg)            , typeof(NpgsqlLSeg)            , "[(1,2),(3,4)]");
				AssertReadMatrix(conn, "box(point(1,2), point(3,4))"                                                , typeof(NpgsqlBox)             , typeof(NpgsqlBox)             , "(3,4),(1,2)");
				AssertReadMatrix(conn, "CAST('[(1,2),(3,4)]' AS path)"                                             , typeof(NpgsqlPath)            , typeof(NpgsqlPath)            , "[(1,2),(3,4)]");
				AssertReadMatrix(conn, "CAST('((1,2),(3,4),(5,6))' AS polygon)"                                    , typeof(NpgsqlPolygon)         , typeof(NpgsqlPolygon)         , "[(1,2),(3,4),(5,6)]");
				AssertReadMatrix(conn, "circle(point(1,2), 3)"                                                     , typeof(NpgsqlCircle)          , typeof(NpgsqlCircle)          , "<(1,2),3>");
				AssertReadMatrix(conn, "line(point(1,2), point(3,4))"                                              , typeof(NpgsqlLine)            , typeof(NpgsqlLine)            , "{1,-1,1}");
				AssertReadMatrix(conn, "int4range(1, 5, '[)')"                                                     , typeof(NpgsqlRange<int>)      , typeof(NpgsqlRange<int>)      , "[1,5)");
				AssertReadMatrix(conn, "numrange(1.1, 5.5, '[)')"                                                  , typeof(NpgsqlRange<decimal>)  , typeof(NpgsqlRange<decimal>)  , "[1.1,5.5)");
				AssertReadMatrix(conn, "daterange(date '2024-01-01', date '2024-02-01', '[)')"                    , typeof(NpgsqlRange<DateOnly>), typeof(NpgsqlRange<DateOnly>), "[2024-01-01,2024-02-01)", providerSpecificOnly: true);
				AssertReadMatrix(conn, "ARRAY[1,2,3]::integer[]"                                                  , typeof(int[])                 , typeof(int[])                 , "[1,2,3]");
			}
		}

		[Test]
		public void SapHanaProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSapHana)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(123 AS INTEGER) FROM dummy"                                , typeof(int)     , typeof(int)     , "123");
				AssertReadMatrix(conn, "CAST(1234567890123 AS BIGINT) FROM dummy"                      , typeof(long)    , typeof(long)    , "1234567890123");
				AssertReadMatrix(conn, "CAST(123.45 AS DECIMAL(10,2)) FROM dummy"                      , typeof(decimal) , typeof(decimal) , "123.45");
				AssertReadMatrix(conn, "CAST(1.25 AS REAL) FROM dummy"                                 , typeof(float)   , typeof(float)   , "1.25");
				AssertReadMatrix(conn, "CAST(1.25 AS DOUBLE) FROM dummy"                               , typeof(double)  , typeof(double)  , "1.25");
				AssertReadMatrix(conn, "CAST('text' AS VARCHAR(10)) FROM dummy"                        , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "CAST('2024-01-02' AS DATE) FROM dummy"                         , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "CAST('03:04:05' AS TIME) FROM dummy"                           , typeof(TimeSpan), typeof(TimeSpan), "03:04:05");
				AssertReadMatrix(conn, "CAST('2024-01-02 03:04:05.123456' AS TIMESTAMP) FROM dummy"    , typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.1234560");
				AssertReadMatrix(conn, "CAST(x'3039' AS BLOB) FROM dummy"                              , typeof(byte[])  , typeof(byte[])  , "0x3039");
			}
		}

		[Test]
		public void SybaseProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(123 AS INT)"                         , typeof(int)     , typeof(int)     , "123");
				AssertReadMatrix(conn, "CAST(1234567890123 AS BIGINT)"            , typeof(long)    , typeof(long)    , "1234567890123");
				AssertReadMatrix(conn, "CAST(123.45 AS DECIMAL(10,2))"            , typeof(decimal) , typeof(decimal) , "123.45");
				AssertReadMatrix(conn, "CAST(1.25 AS REAL)"                       , typeof(float)   , typeof(float)   , "1.25");
				AssertReadMatrix(conn, "CAST(1.25 AS FLOAT)"                      , typeof(double)  , typeof(double)  , "1.25");
				AssertReadMatrix(conn, "CAST('text' AS VARCHAR(10))"              , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "CAST('2024-01-02' AS DATE)"               , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "CAST('03:04:05' AS TIME)"                 , typeof(DateTime), typeof(DateTime), "1900-01-01T03:04:05.0000000");
				AssertReadMatrix(conn, "CAST('2024-01-02 03:04:05.123' AS DATETIME)", typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.1230000");
				AssertReadMatrix(conn, "CAST(0x3039 AS BINARY(2))"                , typeof(byte[])  , typeof(byte[])  , "0x3039");
			}
		}

		[Test]
		public void YdbProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllYdb)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(123 AS Int32)"                                      , typeof(int)     , typeof(int)     , "123");
				AssertReadMatrix(conn, "CAST(1234567890123 AS Int64)"                            , typeof(long)    , typeof(long)    , "1234567890123");
				AssertReadMatrix(conn, "CAST(1.25 AS Double)"                                    , typeof(double)  , typeof(double)  , "1.25");
				AssertReadMatrix(conn, "CAST('text' AS Utf8)"                                    , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "CAST(Date('2024-01-02') AS Date)"                        , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "CAST(Datetime('2024-01-02T03:04:05Z') AS Datetime)"      , typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.0000000");
				AssertReadMatrix(conn, "CAST(Timestamp('2024-01-02T03:04:05.123456Z') AS Timestamp)", typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.1234560");
			}
		}

		[Test]
		public void DuckDBProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllDuckDB)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(123 AS INTEGER)"                         , typeof(int)                  , typeof(int)     , "123");
				AssertReadMatrix(conn, "CAST(1234567890123 AS BIGINT)"                , typeof(long)                 , typeof(long)    , "1234567890123");
				AssertReadMatrix(conn, "CAST(123.45 AS DECIMAL(10,2))"                , typeof(decimal)              , typeof(decimal) , "123.45");
				AssertReadMatrix(conn, "CAST(1.25 AS REAL)"                           , typeof(float)                , typeof(float)   , "1.25");
				AssertReadMatrix(conn, "CAST(1.25 AS DOUBLE)"                         , typeof(double)               , typeof(double)  , "1.25");
				AssertReadMatrix(conn, "CAST('text' AS VARCHAR(10))"                  , typeof(string)               , typeof(string)  , "text");
				AssertReadMatrix(conn, "CAST('2024-01-02' AS DATE)"                   , typeof(DuckDBDateOnly)       , typeof(DateOnly), "2024-01-02");
				AssertReadMatrix(conn, "CAST('03:04:05' AS TIME)"                     , typeof(DuckDBTimeOnly)       , typeof(TimeOnly), "03:04:05.0000000");
				AssertReadMatrix(conn, "CAST('2024-01-02 03:04:05.123456' AS TIMESTAMP)", typeof(DuckDBTimestamp)    , typeof(DateTime), "2024-01-02T03:04:05.1234560");
				AssertReadMatrix(conn, "from_hex('3039')"                             , typeof(UnmanagedMemoryStream), typeof(UnmanagedMemoryStream), "0x3039");
			}
		}

		[Test]
		public void SQLiteProviderSpecificReadMatrix([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(123 AS INTEGER)"                  , typeof(long)  , typeof(long)  , "123");
				AssertReadMatrix(conn, "CAST(1234567890123 AS INTEGER)"        , typeof(long)  , typeof(long)  , "1234567890123");
				AssertReadMatrix(conn, "CAST(1.25 AS REAL)"                    , typeof(double), typeof(double), "1.25");
				AssertReadMatrix(conn, "CAST('text' AS TEXT)"                  , typeof(string), typeof(string), "text");
				AssertReadMatrix(conn, "date('2024-01-02')"                    , typeof(string), typeof(string), "2024-01-02");
				AssertReadMatrix(conn, "time('03:04:05')"                      , typeof(string), typeof(string), "03:04:05");
				AssertReadMatrix(conn, "datetime('2024-01-02 03:04:05')"       , typeof(string), typeof(string), "2024-01-02 03:04:05");
				AssertReadMatrix(conn, "x'3039'"                               , typeof(byte[]), typeof(byte[]), "0x3039");
			}
		}

		[Test]
		public void AccessOdbcProviderSpecificReadMatrix([IncludeDataSources(ProviderName.AccessAceOdbc)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CBOOL(1)"           , typeof(short)   , typeof(short)   , "-1");
				AssertReadMatrix(conn, "CINT(123)"          , typeof(short)   , typeof(short)   , "123");
				AssertReadMatrix(conn, "CLNG(1234567)"      , typeof(int)     , typeof(int)     , "1234567");
				AssertReadMatrix(conn, "CDBL(1.25)"         , typeof(double)  , typeof(double)  , "1.25");
				AssertReadMatrix(conn, "CSTR('text')"       , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "CDATE('2024-01-02')", typeof(DateTime), typeof(DateTime), "2024-01-02T00:00:00.0000000");
			}
		}

		[Test]
		public void FirebirdProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllFirebird4Plus)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(1000000 AS BIGINT) FROM rdb$database"          , typeof(long)    , typeof(long)    , "1000000");
				AssertReadMatrix(conn, "CAST(7777777 AS INTEGER) FROM rdb$database"         , typeof(int)     , typeof(int)     , "7777777");
				AssertReadMatrix(conn, "CAST(100 AS SMALLINT) FROM rdb$database"            , typeof(short)   , typeof(short)   , "100");
				AssertReadMatrix(conn, "CAST(9999999 AS DECIMAL(18,0)) FROM rdb$database"   , typeof(decimal) , typeof(decimal) , "9999999");
				AssertReadMatrix(conn, "CAST(20.31 AS FLOAT) FROM rdb$database"             , typeof(float)   , typeof(float)   , "20.31");
				AssertReadMatrix(conn, "CAST(16.2 AS DOUBLE PRECISION) FROM rdb$database"   , typeof(double)  , typeof(double)  , "16.2");
				AssertReadMatrix(conn, "CAST('text' AS VARCHAR(10)) FROM rdb$database"      , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "DATE '2024-01-02' FROM rdb$database"                , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "TIMESTAMP '2024-01-02 03:04:05.1234' FROM rdb$database", typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.1234000");
				AssertReadMatrix(conn, "CAST(x'3039' AS VARBINARY(2)) FROM rdb$database"    , typeof(byte[])  , typeof(byte[])  , "0x3039");
			}
		}

		[Test]
		public void Firebird4ProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllFirebird4Plus)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(123.45 AS DECFLOAT(16)) FROM rdb$database"              , typeof(FbDecFloat)     , typeof(FbDecFloat)     , "12345E-2");
				AssertReadMatrix(conn, "TIMESTAMP '2024-01-02 03:04:05.1234 UTC' FROM rdb$database"  , typeof(FbZonedDateTime), typeof(FbZonedDateTime), "2024-01-02T03:04:05.1234000 UTC");
				AssertReadMatrix(conn, "TIME '03:04:05.1234 UTC' FROM rdb$database"                  , typeof(FbZonedTime)    , typeof(FbZonedTime)    , "03:04:05.1234000 UTC");
			}
		}

		[Test]
		public void MySqlDataProviderSpecificReadMatrix([IncludeDataSources(ProviderName.MySql80)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(1000000 AS SIGNED)"                                      , typeof(long)    , typeof(long)    , "1000000");
				AssertReadMatrix(conn, "CAST(7777777 AS SIGNED)"                                      , typeof(long)    , typeof(long)    , "7777777");
				AssertReadMatrix(conn, "CAST(9999999 AS DECIMAL(31,0))"                               , typeof(decimal) , typeof(decimal) , "9999999");
				AssertReadMatrix(conn, "CAST(20.31 AS DOUBLE)"                                        , typeof(double)  , typeof(double)  , "20.31");
				AssertReadMatrix(conn, "CAST(16.2 AS FLOAT)"                                          , typeof(float)   , typeof(float)   , "16.2");
				AssertReadMatrix(conn, "CAST('text' AS CHAR(10))"                                     , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "DATE '2024-01-02'"                                            , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "TIMESTAMP '2024-01-02 03:04:05.123456'"                       , typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.1234560");
				AssertReadMatrix(conn, "CAST(0x3039 AS BINARY(2))"                                    , typeof(byte[])  , typeof(byte[])  , "0x3039");
				AssertReadMatrix(conn, "POINT(1, 2)"                                                  , typeof(byte[])  , typeof(byte[])  , "0x000000000101000000000000000000F03F0000000000000040");
				AssertProviderSpecificReaderMethodRequired(conn, "CAST(123456789012345678901234567890.12 AS DECIMAL(65,2))", "GetMySqlDecimal", typeof(MySqlDataDecimal), "123456789012345678901234567890.12");
			}
		}

		[Test]
		public void MySqlConnectorProviderSpecificReadMatrix([IncludeDataSources(TestProvName.MySql80Connector)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "CAST(1000000 AS SIGNED)"                                      , typeof(long)    , typeof(long)    , "1000000");
				AssertReadMatrix(conn, "CAST(7777777 AS SIGNED)"                                      , typeof(long)    , typeof(long)    , "7777777");
				AssertReadMatrix(conn, "CAST(9999999 AS DECIMAL(31,0))"                               , typeof(decimal) , typeof(decimal) , "9999999");
				AssertReadMatrix(conn, "CAST(20.31 AS DOUBLE)"                                        , typeof(double)  , typeof(double)  , "20.31");
				AssertReadMatrix(conn, "CAST(16.2 AS FLOAT)"                                          , typeof(float)   , typeof(float)   , "16.2");
				AssertReadMatrix(conn, "CAST('text' AS CHAR(10))"                                     , typeof(string)  , typeof(string)  , "text");
				AssertReadMatrix(conn, "DATE '2024-01-02'"                                            , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "TIMESTAMP '2024-01-02 03:04:05.123456'"                       , typeof(DateTime), typeof(DateTime), "2024-01-02T03:04:05.1234560");
				AssertReadMatrix(conn, "CAST(0x3039 AS BINARY(2))"                                    , typeof(byte[])  , typeof(byte[])  , "0x3039");
				AssertReadMatrix(conn, "POINT(1, 2)"                                                  , typeof(byte[])  , typeof(byte[])  , "0x000000000101000000000000000000F03F0000000000000040");
				AssertProviderSpecificReaderMethodRequired(conn, "CAST(123456789012345678901234567890.12 AS DECIMAL(65,2))", "GetMySqlDecimal", typeof(MySqlConnectorDecimal), "123456789012345678901234567890.12");
			}
		}

		[Test]
		public void ClickHouseOctonicaProviderSpecificReadMatrix([IncludeDataSources(ProviderName.ClickHouseOctonica)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "toDate('2024-01-02')"                          , typeof(DateOnly)                 , typeof(DateOnly)                 , "2024-01-02");
				AssertReadMatrix(conn, "toDateTime('2024-01-02 00:00:00')"             , typeof(DateTimeOffset)           , typeof(DateTimeOffset)           , "2024-01-02T00:00:00.0000000+00:00");
				AssertReadMatrix(conn, "[1, 2, 3]"                                     , typeof(byte[])                   , typeof(byte[])                   , "[1,2,3]");
				AssertReadMatrix(conn, "tuple(1, 'a')"                                 , typeof(Tuple<byte, string>)      , typeof(Tuple<byte, string>)      , "(1,a)");
				AssertReadMatrix(conn, "map('a', 1, 'b', 2)"                           , typeof(KeyValuePair<string, byte>[]), typeof(KeyValuePair<string, byte>[]), "{a:1,b:2}");
				AssertReadMatrix(conn, "toUUID('01234567-89ab-cdef-0123-456789abcdef')", typeof(Guid)                     , typeof(Guid)                     , "01234567-89ab-cdef-0123-456789abcdef");
			}
		}

		[Test]
		public void ClickHouseDriverProviderSpecificReadMatrix([IncludeDataSources(ProviderName.ClickHouseDriver)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "toDate('2024-01-02')"                          , typeof(DateTime)                , typeof(DateTime)                , "2024-01-02");
				AssertReadMatrix(conn, "toDateTime('2024-01-02 00:00:00')"             , typeof(DateTime)                , typeof(DateTime)                , "2024-01-02T00:00:00.0000000");
				AssertReadMatrix(conn, "[1, 2, 3]"                                     , typeof(byte[])                  , typeof(byte[])                  , "[1,2,3]");
				AssertReadMatrix(conn, "tuple(1, 'a')"                                 , typeof(Tuple<byte, byte[]>)     , typeof(Tuple<byte, byte[]>)     , "(1,0x61)");
				AssertReadMatrix(conn, "map('a', 1, 'b', 2)"                           , typeof(Dictionary<byte[], byte>), typeof(Dictionary<byte[], byte>), "{0x61:1,0x62:2}");
				AssertReadMatrix(conn, "toDecimal256('1234567890123456789012345678901234567890.12', 2)", typeof(ClickHouseDecimal), typeof(ClickHouseDecimal), "1234567890123456789012345678901234567890.12");
			}
		}

		[Test]
		public void ClickHouseMySqlProviderSpecificReadMatrix([IncludeDataSources(ProviderName.ClickHouseMySql)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "toDate('2024-01-02')"                          , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "toDateTime('2024-01-02 00:00:00')"             , typeof(DateTime), typeof(DateTime), "2024-01-02T00:00:00.0000000");
				AssertReadMatrix(conn, "[1, 2, 3]"                                     , typeof(string)  , typeof(string)  , "[1,2,3]");
				AssertReadMatrix(conn, "tuple(1, 'a')"                                 , typeof(string)  , typeof(string)  , "(1,'a')");
				AssertReadMatrix(conn, "map('a', 1, 'b', 2)"                           , typeof(string)  , typeof(string)  , "{'a':1,'b':2}");
				AssertReadMatrix(conn, "toUUID('01234567-89ab-cdef-0123-456789abcdef')", typeof(string)  , typeof(string)  , "01234567-89ab-cdef-0123-456789abcdef");
			}
		}

		void AssertReadMatrix(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, Type expectedGetValueType, string expectedString, bool providerSpecificOnly = false)
		{
			AssertReadMatrix(conn, sqlExpression, expectedProviderSpecificType, expectedGetValueType, expectedString, expectedString, providerSpecificOnly);
		}

		void AssertReadMatrix(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, Type expectedGetValueType, string expectedProviderSpecificString, string expectedGetValueString, bool providerSpecificOnly = false)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				providerSpecific.ExceptionTypeName.ShouldBeNull(sqlExpression);
				providerSpecific.Type.ShouldBe(expectedProviderSpecificType, sqlExpression);
				providerSpecific.StringValue.ShouldBe(expectedProviderSpecificString, sqlExpression);

				if (!providerSpecificOnly)
				{
					getValue.ExceptionTypeName.ShouldBeNull(sqlExpression);
					getValue.Type.ShouldBe(expectedGetValueType, sqlExpression);
					getValue.StringValue.ShouldBe(expectedGetValueString, sqlExpression);
				}
			}
		}

		void AssertProviderSpecificRequired(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, string expectedString)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				providerSpecific.ExceptionTypeName.ShouldBeNull(sqlExpression);
				providerSpecific.Type.ShouldBe(expectedProviderSpecificType, sqlExpression);
				providerSpecific.StringValue.ShouldBe(expectedString, sqlExpression);
				getValue.ExceptionTypeName.ShouldNotBeNull(sqlExpression);
			}
		}

		void AssertBothReadsFail(DataConnection conn, string sqlExpression, string expectedExceptionTypeName)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				providerSpecific.ExceptionTypeName.ShouldBe(expectedExceptionTypeName, sqlExpression);
				getValue.ExceptionTypeName.ShouldBe(expectedExceptionTypeName, sqlExpression);
			}
		}

		void AssertProviderSpecificReaderMethodRequired(DataConnection conn, string sqlExpression, string methodName, Type expectedType, string expectedString)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);
			var methodValue      = ReadProviderSpecificReaderMethodValue(conn, sqlExpression, methodName);

			using (Assert.EnterMultipleScope())
			{
				providerSpecific.ExceptionTypeName.ShouldNotBeNull(sqlExpression);
				getValue.ExceptionTypeName.ShouldNotBeNull(sqlExpression);
				methodValue.ExceptionTypeName.ShouldBeNull(sqlExpression);
				methodValue.Type.ShouldBe(expectedType, sqlExpression);
				methodValue.StringValue.ShouldBe(expectedString, sqlExpression);
			}
		}

		ReadResult ReadValue(DataConnection conn, string sqlExpression, bool providerSpecific)
		{
			using var result = conn.ExecuteReader("SELECT " + sqlExpression + (conn.DataProvider.Name.StartsWith(ProviderName.Oracle, StringComparison.Ordinal) ? " FROM DUAL" : null));
			var reader       = result.Reader ?? throw new InvalidOperationException("Reader is not available.");

			reader.Read().ShouldBeTrue();

			try
			{
				var dataTypeName = reader.GetDataTypeName(0);
				var value        = providerSpecific ? reader.GetProviderSpecificValue(0) : reader.GetValue(0);

				return new ReadResult(value.GetType(), value.GetType().FullName, ConvertValueToString(value, dataTypeName), null);
			}
			catch (Exception exception)
			{
				return new ReadResult(null, null, null, exception.GetType().FullName);
			}
		}

		ReadResult ReadProviderSpecificReaderMethodValue(DataConnection conn, string sqlExpression, string methodName)
		{
			using var result = conn.ExecuteReader("SELECT " + sqlExpression + (conn.DataProvider.Name.StartsWith(ProviderName.Oracle, StringComparison.Ordinal) ? " FROM DUAL" : null));
			var reader       = result.Reader ?? throw new InvalidOperationException("Reader is not available.");

			reader.Read().ShouldBeTrue();

			try
			{
				var dataTypeName = reader.GetDataTypeName(0);
				var method       = reader.GetType().GetMethod(methodName, [typeof(int)])
					?? throw new InvalidOperationException($"Reader method '{methodName}' is not available.");
				var value        = method.Invoke(reader, [0]) ?? throw new InvalidOperationException($"Reader method '{methodName}' returned null.");

				return new ReadResult(value.GetType(), value.GetType().FullName, ConvertValueToString(value, dataTypeName), null);
			}
			catch (Exception exception)
			{
				return new ReadResult(null, null, null, exception.GetType().FullName);
			}
		}

		static string? ConvertValueToString(object value, string dataTypeName)
		{
			return value switch
			{
				SqlBoolean sqlBoolean         => sqlBoolean.Value ? "true" : "false",
				bool boolValue                => boolValue ? "true" : "false",
				SqlSingle sqlSingle           => sqlSingle.Value.ToString("R", CultureInfo.InvariantCulture),
				float singleValue             => singleValue.ToString("R", CultureInfo.InvariantCulture),
				SqlDouble sqlDouble           => sqlDouble.Value.ToString("R", CultureInfo.InvariantCulture),
				double doubleValue            => doubleValue.ToString("R", CultureInfo.InvariantCulture),
				string stringValue            => stringValue,
				DB2Binary db2Binary           => ConvertBytesToString(db2Binary.Value),
				DB2Blob db2Blob               => ConvertBytesToString(db2Blob.Value),
				DB2Clob db2Clob               => db2Clob.Value,
				DB2Date db2Date               => FormatDate(db2Date.Value),
				DB2Time db2Time               => db2Time.Value.ToString("c", CultureInfo.InvariantCulture),
				DB2TimeStamp db2TimeStamp     => db2TimeStamp.Value.ToString("O", CultureInfo.InvariantCulture),
				DB2Xml db2Xml                 => db2Xml.GetString(),
				FbDecFloat fbDecFloat         => FormatFirebirdDecFloat(fbDecFloat),
				FbZonedDateTime zonedDateTime => FormatFirebirdZonedDateTime(zonedDateTime),
				FbZonedTime zonedTime         => FormatFirebirdZonedTime(zonedTime),
				OracleDate oracleDate         => FormatDate(oracleDate.Value),
				OracleTimeStamp timestamp     => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond),
				OracleTimeStampTZ timestamp   => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond) + timestamp.TimeZone,
				OracleTimeStampLTZ timestamp  => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond),
				SqlDateTime sqlDateTime       => sqlDateTime.Value.ToString("O", CultureInfo.InvariantCulture),
				DateOnly dateOnly             => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				TimeOnly timeOnly             => timeOnly.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
				DateTime dateTime             => IsDateDataType(dataTypeName) ? FormatDate(dateTime) : dateTime.ToString("O", CultureInfo.InvariantCulture),
				DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
				TimeSpan timeSpan             => timeSpan.ToString("c", CultureInfo.InvariantCulture),
				SqlGuid sqlGuid               => sqlGuid.Value.ToString("D"),
				Guid guid                     => guid.ToString("D"),
				SqlBinary sqlBinary           => ConvertBytesToString(sqlBinary.Value),
				DuckDBDateOnly date           => FormatDuckDBDateOnly(date),
				DuckDBTimeOnly time           => FormatDuckDBTimeOnly(time),
				DuckDBTimestamp timestamp     => FormatDuckDBTimestamp(timestamp),
				OracleBinary oracleBinary     => ConvertBytesToString(oracleBinary.Value),
				OracleBlob oracleBlob         => ConvertBytesToString(oracleBlob.Value),
				OracleClob oracleClob         => oracleClob.Value,
				OracleXmlType oracleXmlType   => oracleXmlType.Value,
				OracleBFile                   => "<BFILE>",
				Stream stream                 => ConvertStreamToString(stream),
				byte[] bytes                  => dataTypeName.StartsWith("Array(", StringComparison.OrdinalIgnoreCase) ? ConvertByteArrayToString(bytes) : ConvertBytesToString(bytes),
				SqlXml sqlXml                 => sqlXml.Value,
				SqlVector<float> vector       => ConvertVectorToString(vector.Memory.ToArray()),
				SqlVector<Half> vector        => ConvertVectorToString(vector.Memory.ToArray()),
				NpgsqlRange<int> range        => FormatNpgsqlRange(range),
				NpgsqlRange<decimal> range    => FormatNpgsqlRange(range),
				NpgsqlRange<DateOnly> range   => FormatNpgsqlRange(range),
				ITuple tuple                  => ConvertTupleToString(tuple),
				IEnumerable sequence          => ConvertSequenceToString(sequence),
				_                             => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static bool IsDateDataType(string dataTypeName)
		{
			return string.Equals(dataTypeName, "Date", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Date32", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date)", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date32)", StringComparison.OrdinalIgnoreCase);
		}

		static string FormatDate(DateTime value)
		{
			return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		static string FormatOracleTimeStamp(int year, int month, int day, int hour, int minute, int second, int nanosecond)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{year:D4}-{month:D2}-{day:D2}T{hour:D2}:{minute:D2}:{second:D2}.{nanosecond:D9}");
		}

		static string FormatFirebirdDecFloat(FbDecFloat value)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{value.Coefficient}E{value.Exponent}");
		}

		static string FormatFirebirdZonedDateTime(FbZonedDateTime value)
		{
			return value.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + " " + FormatFirebirdTimeZone(value.TimeZone, value.Offset);
		}

		static string FormatFirebirdZonedTime(FbZonedTime value)
		{
			return value.Time.ToString("c", CultureInfo.InvariantCulture) + " " + FormatFirebirdTimeZone(value.TimeZone, value.Offset);
		}

		static string FormatFirebirdTimeZone(string timeZone, TimeSpan? offset)
		{
			return offset?.ToString("c", CultureInfo.InvariantCulture) ?? timeZone;
		}

		static string FormatDuckDBDateOnly(DuckDBDateOnly value)
		{
			if (value.IsPositiveInfinity)
				return "infinity";
			if (value.IsNegativeInfinity)
				return "-infinity";

			return string.Create(CultureInfo.InvariantCulture, $"{value.Year:D4}-{value.Month:D2}-{value.Day:D2}");
		}

		static string FormatDuckDBTimeOnly(DuckDBTimeOnly value)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{value.Hour:D2}:{value.Min:D2}:{value.Sec:D2}.{value.Microsecond:D6}0");
		}

		static string FormatDuckDBTimestamp(DuckDBTimestamp value)
		{
			if (value.IsPositiveInfinity)
				return "infinity";
			if (value.IsNegativeInfinity)
				return "-infinity";

			return FormatDuckDBDateOnly(value.Date) + "T" + FormatDuckDBTimeOnly(value.Time);
		}

		static string FormatNpgsqlRange<T>(NpgsqlRange<T> value)
		{
			if (value.IsEmpty)
				return "empty";

			var output = new System.Text.StringBuilder();

			output.Append(value.LowerBoundIsInclusive ? '[' : '(');

			if (!value.LowerBoundInfinite)
				output.Append(ConvertRangeBoundToString(value.LowerBound));

			output.Append(',');

			if (!value.UpperBoundInfinite)
				output.Append(ConvertRangeBoundToString(value.UpperBound));

			output.Append(value.UpperBoundIsInclusive ? ']' : ')');
			return output.ToString();
		}

		static string? ConvertRangeBoundToString(object? value)
		{
			return value switch
			{
				null                    => null,
				DateOnly date           => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				DateTime dateTime       => dateTime.ToString("O", CultureInfo.InvariantCulture),
				DateTimeOffset dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
				TimeOnly time           => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
				TimeSpan time           => time.ToString("c", CultureInfo.InvariantCulture),
				_                       => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static string ConvertBytesToString(byte[] bytes)
		{
			return "0x" + Convert.ToHexString(bytes);
		}

		static string ConvertStreamToString(Stream stream)
		{
			if (stream.CanSeek)
				stream.Position = 0;

			using var memory = new MemoryStream();

			stream.CopyTo(memory);
			return ConvertBytesToString(memory.ToArray());
		}

		static string ConvertByteArrayToString(byte[] bytes)
		{
			var builder = new System.Text.StringBuilder();

			builder.Append('[');
			for (var i = 0; i < bytes.Length; i++)
			{
				if (i > 0)
					builder.Append(',');

				builder.Append(bytes[i].ToString(CultureInfo.InvariantCulture));
			}

			builder.Append(']');
			return builder.ToString();
		}

		static string ConvertTupleToString(ITuple tuple)
		{
			var builder = new System.Text.StringBuilder();

			builder.Append('(');
			for (var i = 0; i < tuple.Length; i++)
			{
				if (i > 0)
					builder.Append(',');

				builder.Append(ConvertNestedValueToString(tuple[i]));
			}

			builder.Append(')');
			return builder.ToString();
		}

		static string ConvertSequenceToString(IEnumerable sequence)
		{
			var builder      = new System.Text.StringBuilder();
			var first        = true;
			var map          = false;
			var openBracket  = '[';
			var closeBracket = ']';

			foreach (var item in sequence)
			{
				if (first)
				{
					map = IsKeyValuePair(item);

					if (map)
					{
						openBracket  = '{';
						closeBracket = '}';
					}

					builder.Append(openBracket);
					first = false;
				}

				if (builder.Length > 1)
					builder.Append(',');

				if (map)
					AppendKeyValuePair(builder, item);
				else
					builder.Append(ConvertNestedValueToString(item));
			}

			if (first)
				builder.Append(openBracket);

			builder.Append(closeBracket);
			return builder.ToString();
		}

		static string? ConvertNestedValueToString(object? value)
		{
			return value switch
			{
				null               => null,
				string stringValue => stringValue,
				byte[] bytes       => ConvertBytesToString(bytes),
				ITuple tuple       => ConvertTupleToString(tuple),
				IEnumerable items  => ConvertSequenceToString(items),
				_                  => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static bool IsKeyValuePair(object? value)
		{
			return value != null
				&& value.GetType().IsGenericType
				&& value.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
		}

		static void AppendKeyValuePair(System.Text.StringBuilder builder, object? value)
		{
			if (value == null)
			{
				builder.Append(':');
				return;
			}

			var type = value.GetType();
			var key  = type.GetProperty("Key")!.GetValue(value);
			var item = type.GetProperty("Value")!.GetValue(value);

			builder.Append(ConvertNestedValueToString(key));
			builder.Append(':');
			builder.Append(ConvertNestedValueToString(item));
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
