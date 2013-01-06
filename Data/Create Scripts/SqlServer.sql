IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Doctor') AND type in (N'U'))
BEGIN DROP TABLE Doctor END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Patient') AND type in (N'U'))
BEGIN DROP TABLE Patient END

-- Person Table

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Person') AND type in (N'U'))
BEGIN DROP TABLE Person END

CREATE TABLE Person
(
	PersonID   int          NOT NULL IDENTITY(1,1) CONSTRAINT PK_Person PRIMARY KEY CLUSTERED,
	FirstName  nvarchar(50) NOT NULL,
	LastName   nvarchar(50) NOT NULL,
	MiddleName nvarchar(50)     NULL,
	Gender     char(1)      NOT NULL CONSTRAINT CK_Person_Gender CHECK (Gender in ('M', 'F', 'U', 'O'))
)
ON [PRIMARY]
GO

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M')
GO

-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID int          NOT NULL
		CONSTRAINT PK_Doctor        PRIMARY KEY CLUSTERED
		CONSTRAINT FK_Doctor_Person FOREIGN KEY
			REFERENCES Person ([PersonID])
			ON UPDATE CASCADE
			ON DELETE CASCADE,
	Taxonomy nvarchar(50) NOT NULL
)
ON [PRIMARY]
GO

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int           NOT NULL
		CONSTRAINT PK_Patient        PRIMARY KEY CLUSTERED
		CONSTRAINT FK_Patient_Person FOREIGN KEY
			REFERENCES Person ([PersonID])
			ON UPDATE CASCADE
			ON DELETE CASCADE,
	Diagnosis nvarchar(256) NOT NULL
)
ON [PRIMARY]
GO

INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
GO

-- Data Types test

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('AllTypes') AND type in (N'U'))
BEGIN DROP TABLE AllTypes END
GO

CREATE TABLE AllTypes
(
	ID                       int          NOT NULL IDENTITY(1,1) CONSTRAINT PK_AllTypes PRIMARY KEY CLUSTERED,

	bigintDataType           bigint           NULL,
	numericDataType          numeric          NULL,
	bitDataType              bit              NULL,
	smallintDataType         smallint         NULL,
	decimalDataType          decimal          NULL,
	smallmoneyDataType       smallmoney       NULL,
	intDataType              int              NULL,
	tinyintDataType          tinyint          NULL,
	moneyDataType            money            NULL,
	floatDataType            float            NULL,
	realDataType             real             NULL,

	datetimeDataType         datetime         NULL,
	smalldatetimeDataType    smalldatetime    NULL,

	charDataType             char(1)          NULL,
	varcharDataType          varchar(20)      NULL,
	textDataType             text             NULL,
	ncharDataType            nchar(20)        NULL,
	nvarcharDataType         nvarchar(20)     NULL,
	ntextDataType            ntext            NULL,

	binaryDataType           binary           NULL,
	varbinaryDataType        varbinary        NULL,
	imageDataType            image            NULL,

	timestampDataType        timestamp        NULL,
	uniqueidentifierDataType uniqueidentifier NULL,
	sql_variantDataType      sql_variant      NULL,

	nvarchar_max_DataType    nvarchar(max)    NULL,
	varchar_max_DataType     varchar(max)     NULL,
	varbinary_max_DataType   varbinary(max)   NULL,

	xmlDataType              xml              NULL
) ON [PRIMARY]
GO

INSERT INTO AllTypes
(
	bigintDataType, numericDataType, bitDataType, smallintDataType, decimalDataType, smallmoneyDataType,
	intDataType, tinyintDataType, moneyDataType, floatDataType, realDataType, 

	datetimeDataType, smalldatetimeDataType,

	charDataType, varcharDataType, textDataType, ncharDataType, nvarcharDataType, ntextDataType,

	binaryDataType, varbinaryDataType, imageDataType,

	uniqueidentifierDataType, sql_variantDataType,

	nvarchar_max_DataType, varchar_max_DataType, varbinary_max_DataType,

	xmlDataType
)
SELECT
	     NULL,      NULL,  NULL,    NULL,    NULL,   NULL,    NULL, NULL,   NULL,  NULL,  NULL,
	     NULL,      NULL,
	     NULL,      NULL,  NULL,    NULL,    NULL,   NULL,
	     NULL,      NULL,  NULL,
	     NULL,      NULL,
	     NULL,      NULL,  NULL,
	     NULL
UNION ALL
SELECT
	 1000000,    9999999,     1,   25555, 2222222, 100000, 7777777,  100, 100000, 20.31, 16.2,
	Cast('2012-12-12 12:12:12' as datetime),
	           Cast('2012-12-12 12:12:12' as smalldatetime),
	      '1',     '234', '567', '23233',  '3323',  '111',
	        1,         2, Cast(3 as varbinary),
	Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier),
	                  10,
	  '22322',    '3333',  2345,
	'<root><element strattr="strvalue" intattr="12345"/></root>'

GO

-- SKIP SqlServer.2005 BEGIN
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('AllTypes2') AND type in (N'U'))
BEGIN DROP TABLE AllTypes2 END
GO

CREATE TABLE AllTypes2
(
	ID                     int        NOT NULL IDENTITY(1,1) CONSTRAINT PK_AllTypes2 PRIMARY KEY CLUSTERED,

	dateDataType           date           NULL,
	datetimeoffsetDataType datetimeoffset NULL,
	datetime2DataType      datetime2      NULL,
	timeDataType           time           NULL,
	hierarchyidDataType    hierarchyid    NULL,
	geographyDataType      geography      NULL,
	geometryDataType       geometry       NULL

) ON [PRIMARY]
GO

INSERT INTO AllTypes2
SELECT
	NULL, NULL, NULL, NULL, NULL, NULL, NULL
UNION ALL
SELECT
	Cast('2012-12-12'                    as date),
	Cast('2012-12-12 12:12:12.012 +5:00' as datetimeoffset),
	Cast('2012-12-12 12:12:12.012'       as datetime2),
	Cast('12:12:12.012'                  as time),
	Cast('/1/3/'                         as hierarchyid),
	Cast(geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656)', 4326) as geography),
	Cast(geometry::STGeomFromText('LINESTRING (100 100, 20 180, 180 180)', 0) as geometry)

GO
-- SKIP SqlServer.2005 END


-- GetParentByID function

DROP FUNCTION GetParentByID
GO

DROP TABLE Parent
GO
DROP TABLE Child
GO
DROP TABLE GrandChild
GO

CREATE TABLE Parent      (ParentID int, Value1 int)
GO
CREATE TABLE Child       (ParentID int, ChildID int)
GO
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int)
GO

CREATE FUNCTION GetParentByID(@id int)
RETURNS TABLE
AS
RETURN 
(
	SELECT * FROM Parent WHERE ParentID = @id
)
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('LinqDataTypes') AND type in (N'U'))
BEGIN DROP TABLE LinqDataTypes END
GO


-- SKIP SqlServer.2005 BEGIN
CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  datetime,
	DateTimeValue2 datetime2,
	BoolValue      bit,
	GuidValue      uniqueidentifier,
	BinaryValue    varbinary(5000),
	SmallIntValue  smallint,
	IntValue       int NULL,
	BigIntValue    bigint NULL
)
GO
-- SKIP SqlServer.2005 END

-- SKIP SqlServer.2008 BEGIN
-- SKIP SqlServer.2012 BEGIN
CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  datetime,
	DateTimeValue2 datetime,
	BoolValue      bit,
	GuidValue      uniqueidentifier,
	BinaryValue    varbinary(5000) NULL,
	SmallIntValue  smallint,
	IntValue       int             NULL,
	BigIntValue    bigint          NULL
)
GO
-- SKIP SqlServer.2012 END
-- SKIP SqlServer.2008 END

DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity (
	ID int NOT NULL IDENTITY(1,1) CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED
)
GO
