using System;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class AccessTest : TestBase
	{
		static void TestType<T>(DataConnection connection, string dataTypeName, T value, string tableName = "AllTypes", bool convertToString = false)
		{
			connection.Command.Parameters.Clear();
			Assert.That(connection.Execute<T>(string.Format("SELECT {0} FROM {1} WHERE ID = 1", dataTypeName, tableName)),
				Is.EqualTo(connection.MappingSchema.GetDefaultValue(typeof(T))));

			connection.Command.Parameters.Clear();

			object actualValue   = connection.Execute<T>(string.Format("SELECT {0} FROM {1} WHERE ID = 2", dataTypeName, tableName));
			object expectedValue = value;

			if (convertToString)
			{
				actualValue   = actualValue.  ToString();
				expectedValue = expectedValue.ToString();
			}

			Assert.That(actualValue, Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestDataTypes([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
//				TestType(conn, "bigintDataType",           1000000L);
//				TestType(conn, "numericDataType",          9999999m);
//				TestType(conn, "bitDataType",              true);
//				TestType(conn, "smallintDataType",         (short)25555);
//				TestType(conn, "decimalDataType",          2222222m);
//				TestType(conn, "smallmoneyDataType",       100000m);
//				TestType(conn, "intDataType",              7777777);
//				TestType(conn, "tinyintDataType",          (sbyte)100);
//				TestType(conn, "moneyDataType",            100000m);
//				TestType(conn, "floatDataType",            20.31d);
//				TestType(conn, "realDataType",             16.2f);
//
//				TestType(conn, "datetimeDataType",         new DateTime(2012, 12, 12, 12, 12, 12));
//				TestType(conn, "smalldatetimeDataType",    new DateTime(2012, 12, 12, 12, 12, 00));
//
//				TestType(conn, "charDataType",             '1');
//				TestType(conn, "varcharDataType",          "234");
//				TestType(conn, "textDataType",             "567");
//				TestType(conn, "ncharDataType",            "23233");
//				TestType(conn, "nvarcharDataType",         "3323");
//				TestType(conn, "ntextDataType",            "111");

				TestType(conn, "binaryDataType",           new byte[] { 1, 2, 3, 4, 0, 0, 0, 0, 0, 0 });
				TestType(conn, "varbinaryDataType",        new byte[] { 1, 2, 3, 5 });
				TestType(conn, "imageDataType",            new byte[] { 3, 4, 5, 6 });

//				TestType(conn, "uniqueidentifierDataType", new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"));
//				TestType(conn, "sql_variantDataType",      (object)10);
//
//				TestType(conn, "nvarchar_max_DataType",    "22322");
//				TestType(conn, "varchar_max_DataType",     "3333");
//				TestType(conn, "varbinary_max_DataType",   new byte[] { 0, 0, 9, 41 });
//
//				TestType(conn, "xmlDataType",              "<root><element strattr=\"strvalue\" intattr=\"12345\" /></root>");
//
//				conn.Command.Parameters.Clear();
//				Assert.That(conn.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 1").Length, Is.EqualTo(8));
			}
		}



		/*
CREATE TABLE AllTypes
(
	ID                       Counter          NOT NULL,

--	bigintDataType           bigint           NULL,
--	numericDataType          numeric          NULL,
--	bitDataType              bit              NULL,
--	smallintDataType         smallint         NULL,
--	decimalDataType          decimal          NULL,
--	smallmoneyDataType       smallmoney       NULL,
--	intDataType              int              NULL,
--	tinyintDataType          tinyint          NULL,
--	moneyDataType            money            NULL,
--	floatDataType            float            NULL,
--	realDataType             real             NULL,
--
--	datetimeDataType         datetime         NULL,
--	smalldatetimeDataType    smalldatetime    NULL,
--
--	charDataType             char(1)          NULL,
--	varcharDataType          varchar(20)      NULL,
--	textDataType             text             NULL,
--	ncharDataType            nchar(20)        NULL,
--	nvarcharDataType         nvarchar(20)     NULL,
--	ntextDataType            ntext            NULL,
--
	binaryDataType           binary           NULL,
	imageDataType            image            NULL
--
--	timestampDataType        timestamp        NULL,
--	uniqueidentifierDataType uniqueidentifier NULL,
--	sql_variantDataType      sql_variant      NULL,
--
--	nvarchar_max_DataType    nvarchar(max)    NULL,
--	varchar_max_DataType     varchar(max)     NULL,
--	varbinary_max_DataType   varbinary(max)   NULL,
--
--	xmlDataType              xml              NULL
)
GO

/*
	DataTypeID              AutoIncrement,
	Binary_                 Image,
	Boolean_                Long,
	Byte_                   Byte DEFAULT 0,
	Bytes_                  Image,
	Char_                   Text(1),
	DateTime_               DateTime,
	Decimal_                Currency DEFAULT 0,
	Double_                 Double DEFAULT 0,
	Guid_                   Uniqueidentifier,
	Int16_                  SmallInt DEFAULT 0,
	Int32_                  Long DEFAULT 0,
	Int64_                  Long DEFAULT 0,
	Money_                  Currency DEFAULT 0,
	SByte_                  Byte DEFAULT 0,
	Single_                 Single DEFAULT 0,
	Stream_                 Image,
	String_                 Text(50) WITH COMP,
	UInt16_                 SmallInt DEFAULT 0,
	UInt32_                 Long DEFAULT 0,
	UInt64_                 Long DEFAULT 0,    
	Xml_                    Text WITH COMP,

INSERT INTO AllTypes
(
--	bigintDataType, numericDataType, bitDataType, smallintDataType, decimalDataType, smallmoneyDataType,
--	intDataType, tinyintDataType, moneyDataType, floatDataType, realDataType, 
--
--	datetimeDataType, smalldatetimeDataType,
--
--	charDataType, varcharDataType, textDataType, ncharDataType, nvarcharDataType, ntextDataType,
--
	binaryDataType, imageDataType
--
--	uniqueidentifierDataType, sql_variantDataType,
--
--	nvarchar_max_DataType, varchar_max_DataType, varbinary_max_DataType,
--
--	xmlDataType
)
SELECT
--	     NULL,      NULL,  NULL,    NULL,    NULL,   NULL,    NULL, NULL,   NULL,  NULL,  NULL,
--	     NULL,      NULL,
--	     NULL,      NULL,  NULL,    NULL,    NULL,   NULL,
	     NULL,      NULL
--	     NULL,      NULL,
--	     NULL,      NULL,  NULL,
--	     NULL
UNION ALL
SELECT
--	 1000000,    9999999,     1,   25555, 2222222, 100000, 7777777,  100, 100000, 20.31, 16.2,
--	Cast('2012-12-12 12:12:12' as datetime),
--	           Cast('2012-12-12 12:12:12' as smalldatetime),
--	      '1',     '234', '567', '23233',  '3323',  '111',
	        1, Cast(3 as varbinary)
--	Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier),
--	                  10,
--	  '22322',    '3333',  2345,
--	'<root><element strattr="strvalue" intattr="12345"/></root>'

GO
		 */
	}
}
