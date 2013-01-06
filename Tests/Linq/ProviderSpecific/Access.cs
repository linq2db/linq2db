using System;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.ProviderSpecific
{
	[TestFixture]
	public class Access : TestBase
	{
		[Test]
		public void SqlTest([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.Execute(@"
					UPDATE
						[Child] [c]
							LEFT JOIN [Parent] [t1] ON [c].[ParentID] = [t1].[ParentID]
					SET
						[ChildID] = @id
					WHERE
						[c].[ChildID] = @id1 AND [t1].[Value1] = 1",
					new { id1 = 1001, id = 1002 });
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
