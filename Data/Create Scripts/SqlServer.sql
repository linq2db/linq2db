IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Doctor') AND type in (N'U'))
BEGIN DROP TABLE Doctor END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Patient') AND type in (N'U'))
BEGIN DROP TABLE Patient END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('InheritanceParent') AND type in (N'U'))
BEGIN DROP TABLE InheritanceParent END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('InheritanceChild') AND type in (N'U'))
BEGIN DROP TABLE InheritanceChild END

CREATE TABLE InheritanceParent
(
	InheritanceParentId int          NOT NULL CONSTRAINT PK_InheritanceParent PRIMARY KEY CLUSTERED,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL
)
ON [PRIMARY]
GO

CREATE TABLE InheritanceChild
(
	InheritanceChildId  int          NOT NULL CONSTRAINT PK_InheritanceChild PRIMARY KEY CLUSTERED,
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL
)
ON [PRIMARY]
GO

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
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jane',   'Doe',       'F')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES (N'Jürgen', N'König',   'M')
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


-- Person_SelectByKey

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_SelectByKey')
BEGIN DROP Procedure Person_SelectByKey
END
GO

CREATE Procedure Person_SelectByKey
	@id int
AS

SELECT * FROM Person WHERE PersonID = @id

GO

GRANT EXEC ON Person_SelectByKey TO PUBLIC
GO

-- Person_SelectAll

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_SelectAll')
BEGIN DROP Procedure Person_SelectAll END
GO

CREATE Procedure Person_SelectAll
AS

SELECT * FROM Person

GO

GRANT EXEC ON Person_SelectAll TO PUBLIC
GO

-- Person_SelectByName

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_SelectByName')
BEGIN DROP Procedure Person_SelectByName END
GO

CREATE Procedure Person_SelectByName
	@firstName nvarchar(50),
	@lastName  nvarchar(50)
AS

SELECT
	*
FROM
	Person
WHERE
	FirstName = @firstName AND LastName = @lastName

GO

GRANT EXEC ON Person_SelectByName TO PUBLIC
GO

-- Person_SelectListByName

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_SelectListByName')
BEGIN DROP Procedure Person_SelectListByName
END
GO

CREATE Procedure Person_SelectListByName
	@firstName nvarchar(50),
	@lastName  nvarchar(50)
AS

SELECT
	*
FROM
	Person
WHERE
	FirstName like @firstName AND LastName like @lastName

GO

GRANT EXEC ON Person_SelectByName TO PUBLIC
GO

-- Person_Insert

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_Insert')
BEGIN DROP Procedure Person_Insert END
GO

CREATE Procedure Person_Insert
	@FirstName  nvarchar(50),
	@LastName   nvarchar(50),
	@MiddleName nvarchar(50),
	@Gender     char(1)
AS

INSERT INTO Person
	( LastName,  FirstName,  MiddleName,  Gender)
VALUES
	(@LastName, @FirstName, @MiddleName, @Gender)

SELECT Cast(SCOPE_IDENTITY() as int) PersonID

GO

GRANT EXEC ON Person_Insert TO PUBLIC
GO

-- Person_Insert_OutputParameter

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_Insert_OutputParameter')
BEGIN DROP Procedure Person_Insert_OutputParameter END
GO

CREATE Procedure Person_Insert_OutputParameter
	@FirstName  nvarchar(50),
	@LastName   nvarchar(50),
	@MiddleName nvarchar(50),
	@Gender     char(1),
	@PersonID   int output
AS

INSERT INTO Person
	( LastName,  FirstName,  MiddleName,  Gender)
VALUES
	(@LastName, @FirstName, @MiddleName, @Gender)

SET @PersonID = Cast(SCOPE_IDENTITY() as int)

GO

GRANT EXEC ON Person_Insert_OutputParameter TO PUBLIC
GO

-- Person_Update

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_Update')
BEGIN DROP Procedure Person_Update END
GO

CREATE Procedure Person_Update
	@PersonID   int,
	@FirstName  nvarchar(50),
	@LastName   nvarchar(50),
	@MiddleName nvarchar(50),
	@Gender     char(1)
AS

UPDATE
	Person
SET
	LastName   = @LastName,
	FirstName  = @FirstName,
	MiddleName = @MiddleName,
	Gender     = @Gender
WHERE
	PersonID = @PersonID

GO

GRANT EXEC ON Person_Update TO PUBLIC
GO

-- Person_Delete

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Person_Delete')
BEGIN DROP Procedure Person_Delete END
GO

CREATE Procedure Person_Delete
	@PersonID int
AS

DELETE FROM Person WHERE PersonID = @PersonID

GO

GRANT EXEC ON Person_Delete TO PUBLIC
GO

-- Patient_SelectAll

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Patient_SelectAll')
BEGIN DROP Procedure Patient_SelectAll END
GO

CREATE Procedure Patient_SelectAll
AS

SELECT
	Person.*, Patient.Diagnosis
FROM
	Patient, Person
WHERE
	Patient.PersonID = Person.PersonID

GO

GRANT EXEC ON Patient_SelectAll TO PUBLIC
GO

-- Patient_SelectByName

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Patient_SelectByName')
BEGIN DROP Procedure Patient_SelectByName END
GO

CREATE Procedure Patient_SelectByName
	@firstName nvarchar(50),
	@lastName  nvarchar(50)
AS

SELECT
	Person.*, Patient.Diagnosis
FROM
	Patient, Person
WHERE
	Patient.PersonID = Person.PersonID
	AND FirstName = @firstName AND LastName = @lastName

GO

GRANT EXEC ON Person_SelectByName TO PUBLIC
GO


-- OutRefTest

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'OutRefTest')
BEGIN DROP Procedure OutRefTest END
GO

CREATE Procedure OutRefTest
	@ID             int,
	@outputID       int output,
	@inputOutputID  int output,
	@str            varchar(50),
	@outputStr      varchar(50) output,
	@inputOutputStr varchar(50) output
AS

SET @outputID       = @ID
SET @inputOutputID  = @ID + @inputOutputID
SET @outputStr      = @str
SET @inputOutputStr = @str + @inputOutputStr

GO

-- OutRefEnumTest

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'OutRefEnumTest')
BEGIN DROP Procedure OutRefEnumTest END
GO

CREATE Procedure OutRefEnumTest
	@str            varchar(50),
	@outputStr      varchar(50) output,
	@inputOutputStr varchar(50) output
AS

SET @outputStr      = @str
SET @inputOutputStr = @str + @inputOutputStr

GO


-- Data Types test

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('AllTypes') AND type in (N'U'))
BEGIN DROP TABLE AllTypes END
GO

CREATE TABLE AllTypes
(
	ID                       int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_AllTypes PRIMARY KEY CLUSTERED,

	bigintDataType           bigint            NULL,
	numericDataType          numeric(18,1)     NULL,
	bitDataType              bit               NULL,
	smallintDataType         smallint          NULL,
	decimalDataType          decimal(18,1)     NULL,
	smallmoneyDataType       smallmoney        NULL,
	intDataType              int               NULL,
	tinyintDataType          tinyint           NULL,
	moneyDataType            money             NULL,
	floatDataType            float             NULL,
	realDataType             real              NULL,

	datetimeDataType         datetime          NULL,
	smalldatetimeDataType    smalldatetime     NULL,

	charDataType             char(1)           NULL,
	char20DataType           char(20)          NULL,
	varcharDataType          varchar(20)       NULL,
	-- explicit collation set for legacy text types as they doesn't support *_SC collations and this script will
	-- fail if database has such collation
	textDataType             text  COLLATE Latin1_General_CI_AS NULL,
	ncharDataType            nchar(20)         NULL,
	nvarcharDataType         nvarchar(20)      NULL,
	-- see textDataType column notes
	ntextDataType            ntext COLLATE Latin1_General_CI_AS NULL,

	binaryDataType           binary            NULL,
	varbinaryDataType        varbinary         NULL,
	imageDataType            image             NULL,

	timestampDataType        timestamp         NULL,
	uniqueidentifierDataType uniqueidentifier  NULL,
	sql_variantDataType      sql_variant       NULL,

	nvarchar_max_DataType    nvarchar(max)     NULL,
	varchar_max_DataType     varchar(max)      NULL,
	varbinary_max_DataType   varbinary(max)    NULL,

	xmlDataType              xml               NULL,

-- SKIP SqlServer.2005 BEGIN
	datetime2DataType        datetime2         NULL,
	datetimeoffsetDataType   datetimeoffset    NULL,
	datetimeoffset0DataType  datetimeoffset(0) NULL,
	datetimeoffset1DataType  datetimeoffset(1) NULL,
	datetimeoffset2DataType  datetimeoffset(2) NULL,
	datetimeoffset3DataType  datetimeoffset(3) NULL,
	datetimeoffset4DataType  datetimeoffset(4) NULL,
	datetimeoffset5DataType  datetimeoffset(5) NULL,
	datetimeoffset6DataType  datetimeoffset(6) NULL,
	datetimeoffset7DataType  datetimeoffset(7) NULL,
	dateDataType             date              NULL,
	timeDataType             time              NULL
-- SKIP SqlServer.2005 END

-- SKIP SqlServer.2008 BEGIN
-- SKIP SqlServer.2012 BEGIN
-- SKIP SqlServer.2014 BEGIN
-- SKIP SqlAzure.2012 BEGIN
	datetime2DataType        varchar(50)       NULL,
	datetimeoffsetDataType   varchar(50)       NULL,
	datetimeoffset0DataType  varchar(50)       NULL,
	datetimeoffset1DataType  varchar(50)       NULL,
	datetimeoffset2DataType  varchar(50)       NULL,
	datetimeoffset3DataType  varchar(50)       NULL,
	datetimeoffset4DataType  varchar(50)       NULL,
	datetimeoffset5DataType  varchar(50)       NULL,
	datetimeoffset6DataType  varchar(50)       NULL,
	datetimeoffset7DataType  varchar(50)       NULL,
	dateDataType             varchar(50)       NULL,
	timeDataType             varchar(50)       NULL
-- SKIP SqlServer.2008 END
-- SKIP SqlServer.2012 END
-- SKIP SqlServer.2014 END
-- SKIP SqlAzure.2012 END

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
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'IF' AND name = 'GetParentByID')
BEGIN DROP FUNCTION GetParentByID
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('ParentView') AND type in (N'V'))
BEGIN DROP VIEW ParentView END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('ParentChildView') AND type in (N'V'))
BEGIN DROP VIEW ParentChildView END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Parent') AND type in (N'U'))
BEGIN DROP TABLE Parent END
GO
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Child') AND type in (N'U'))
BEGIN DROP TABLE Child END
GO
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('GrandChild') AND type in (N'U'))
BEGIN DROP TABLE GrandChild END
GO

CREATE TABLE Parent     (ParentID int, Value1 int,  _ID INT IDENTITY PRIMARY KEY)
GO
CREATE TABLE Child      (ParentID int, ChildID int, _ID INT IDENTITY PRIMARY KEY)
GO
CREATE INDEX IX_ChildIndex ON Child (ParentID)
GO
CREATE TABLE GrandChild (ParentID int, ChildID int, GrandChildID int, _ID INT IDENTITY PRIMARY KEY)
GO

-- SKIP SqlAzure.2012 BEGIN

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'This is Parent table' , @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Parent'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'This ChildID column', @level0type=N'SCHEMA', @level0name=N'dbo',  @level1type=N'TABLE', @level1name=N'Child', @level2type=N'COLUMN', @level2name=N'ChildID'
GO
-- SKIP SqlAzure.2012 END


CREATE FUNCTION GetParentByID(@id int)
RETURNS TABLE
AS
RETURN
(
	SELECT * FROM Parent WHERE ParentID = @id
)
GO

-- ParentView

CREATE VIEW ParentView
AS
	SELECT * FROM Parent
GO

-- ParentChildView

CREATE VIEW ParentChildView
AS
	SELECT
		p.ParentID,
		p.Value1,
		ch.ChildID
	FROM Parent p
		LEFT JOIN Child ch ON p.ParentID = ch.ParentID
GO


-- LinqDataTypes

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('LinqDataTypes') AND type in (N'U'))
BEGIN DROP TABLE LinqDataTypes END
GO

-- SKIP SqlServer.2005 BEGIN
CREATE TABLE LinqDataTypes
(
	_ID            int IDENTITY PRIMARY KEY,
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  datetime,
	DateTimeValue2 datetime2,
	BoolValue      bit,
	GuidValue      uniqueidentifier,
	BinaryValue    varbinary(5000),
	SmallIntValue  smallint,
	IntValue       int NULL,
	BigIntValue    bigint NULL,
	StringValue    nvarchar(50) NULL
)
GO
-- SKIP SqlServer.2005 END

-- SKIP SqlServer.2008 BEGIN
-- SKIP SqlServer.2012 BEGIN
-- SKIP SqlServer.2014 BEGIN
-- SKIP SqlAzure.2012 BEGIN
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
	BigIntValue    bigint          NULL,
	StringValue    nvarchar(50)    NULL
)
GO
-- SKIP SqlAzure.2012 END
-- SKIP SqlServer.2012 END
-- SKIP SqlServer.2014 END
-- SKIP SqlServer.2008 END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestIdentity') AND type in (N'U'))
BEGIN DROP TABLE TestIdentity END
GO

CREATE TABLE TestIdentity (
	ID int NOT NULL IDENTITY(1,1) CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED
) ON [PRIMARY]
GO


-- IndexTable
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('IndexTable2') AND type in (N'U'))
BEGIN DROP TABLE IndexTable2 END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('IndexTable') AND type in (N'U'))
BEGIN DROP TABLE IndexTable END
GO

CREATE TABLE IndexTable
(
	PKField1    int NOT NULL,
	PKField2    int NOT NULL,
	UniqueField int NOT NULL,
	IndexField  int NOT NULL,
	CONSTRAINT PK_IndexTable PRIMARY KEY CLUSTERED (PKField2, PKField1),
	CONSTRAINT IX_IndexTable UNIQUE NONCLUSTERED (UniqueField)
)
GO

CREATE TABLE IndexTable2
(
	PKField1 int NOT NULL,
	PKField2 int NOT NULL,
	CONSTRAINT PK_IndexTable2 PRIMARY KEY CLUSTERED (PKField2, PKField1),
	CONSTRAINT FK_Patient2_IndexTable FOREIGN KEY (PKField2,PKField1)
			REFERENCES IndexTable (PKField2,PKField1)
			ON UPDATE CASCADE
			ON DELETE CASCADE
)
GO


IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'SelectImplicitColumn')
BEGIN DROP Procedure SelectImplicitColumn
END
GO

CREATE PROCEDURE SelectImplicitColumn
AS
BEGIN
	SELECT 123
END
GO


IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'DuplicateColumnNames')
BEGIN DROP Procedure DuplicateColumnNames
END
GO

CREATE PROCEDURE DuplicateColumnNames
AS
BEGIN
	SELECT 123 as id, '456' as id
END
GO


IF EXISTS (SELECT * FROM sys.objects WHERE name = 'Name.Test')
BEGIN DROP TABLE [Name.Test] END
GO

CREATE TABLE [Name.Test]
(
--	ID INT IDENTITY PRIMARY KEY CLUSTERED,
	[Name.Test] int
)
GO

IF EXISTS (SELECT * FROM sys.objects WHERE name = 'GuidID')
BEGIN DROP TABLE [GuidID] END
GO

CREATE TABLE [GuidID]
(
	ID uniqueidentifier default(NewID()) PRIMARY KEY CLUSTERED,
	Field1 int
)
GO

IF EXISTS (SELECT * FROM sys.objects WHERE name = 'GuidID2')
BEGIN DROP TABLE [GuidID2] END
GO

CREATE TABLE [GuidID2]
(
	ID uniqueidentifier default(NewID()) PRIMARY KEY CLUSTERED
)
GO

IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecimalOverflow')
BEGIN DROP TABLE [DecimalOverflow] END
GO

CREATE TABLE [DecimalOverflow]
(
	Decimal1 decimal(38,20) NOT NULL PRIMARY KEY CLUSTERED,
	Decimal2 decimal(31,2),
	Decimal3 decimal(38,36),
	Decimal4 decimal(29,0),
	Decimal5 decimal(38,38)
)
GO

INSERT INTO [DecimalOverflow]
SELECT  123456789012345.12345678901234567890,  1234567890123456789.91,  12.345678901234512345678901234567890,  1234567890123456789,  .12345678901234512345678901234567890 UNION ALL
SELECT -123456789012345.12345678901234567890, -1234567890123456789.91, -12.345678901234512345678901234567890, -1234567890123456789, -.12345678901234512345678901234567890 UNION ALL
SELECT  12345678901234.567890123456789,                          NULL,                                  NULL,                 NULL,                                  NULL UNION ALL
SELECT -12345678901234.567890123456789,                          NULL,                                  NULL,                 NULL,                                  NULL UNION ALL
SELECT  12345678901234.56789012345678,                           NULL,                                  NULL,                 NULL,                                  NULL UNION ALL
SELECT -12345678901234.56789012345678,                           NULL,                                  NULL,                 NULL,                                  NULL UNION ALL
SELECT  12345678901234.5678901234567,                            NULL,                                  NULL,                 NULL,                                  NULL UNION ALL
SELECT -12345678901234.5678901234567,                            NULL,                                  NULL,                 NULL,                                  NULL

GO

-- SKIP SqlServer.2005 BEGIN

IF EXISTS (SELECT * FROM sys.objects WHERE name = 'SqlTypes')
BEGIN DROP TABLE [SqlTypes] END
GO

CREATE TABLE [SqlTypes]
(
	ID  int NOT NULL PRIMARY KEY CLUSTERED,
	HID hierarchyid,
)
GO

INSERT INTO [SqlTypes]
SELECT 1, hierarchyid::Parse('/')      UNION ALL
SELECT 2, hierarchyid::Parse('/1/')    UNION ALL
SELECT 3, hierarchyid::Parse('/1/1/')  UNION ALL
SELECT 4, hierarchyid::Parse('/1/2/')  UNION ALL
SELECT 5, hierarchyid::Parse('/2/')    UNION ALL
SELECT 6, hierarchyid::Parse('/2/1/')  UNION ALL
SELECT 7, hierarchyid::Parse('/2/2/')  UNION ALL
SELECT 8, hierarchyid::Parse('/2/1/1/')

GO

-- SKIP SqlServer.2005 END


-- merge test tables
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestMerge1') AND type in (N'U'))
BEGIN DROP TABLE TestMerge1 END
GO
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestMerge2') AND type in (N'U'))
BEGIN DROP TABLE TestMerge2 END
GO
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestMergeIdentity') AND type in (N'U'))
BEGIN DROP TABLE TestMergeIdentity END
GO
CREATE TABLE TestMerge1
(
	Id     int NOT NULL CONSTRAINT PK_TestMerge1 PRIMARY KEY CLUSTERED,
	Field1 int NULL,
	Field2 int NULL,
	Field3 int NULL,
	Field4 int NULL,
	Field5 int NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BIT               NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      FLOAT(24)         NULL,
	FieldDouble     FLOAT(53)         NULL,
	FieldDateTime   DATETIME          NULL,
-- SKIP SqlServer.2005 BEGIN
	FieldDateTime2  DATETIMEOFFSET(7) NULL,
-- SKIP SqlServer.2005 END
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       UNIQUEIDENTIFIER  NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
-- SKIP SqlServer.2005 BEGIN
	FieldDate       DATE              NULL,
	FieldTime       TIME(7)           NULL,
-- SKIP SqlServer.2005 END
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
)
GO

CREATE TABLE TestMerge2
(
	Id     int NOT NULL CONSTRAINT PK_TestMerge2 PRIMARY KEY CLUSTERED,
	Field1 int NULL,
	Field2 int NULL,
	Field3 int NULL,
	Field4 int NULL,
	Field5 int NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BIT               NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      FLOAT(24)         NULL,
	FieldDouble     FLOAT(53)         NULL,
	FieldDateTime   DATETIME          NULL,
-- SKIP SqlServer.2005 BEGIN
	FieldDateTime2  DATETIMEOFFSET(7) NULL,
-- SKIP SqlServer.2005 END
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       UNIQUEIDENTIFIER  NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
-- SKIP SqlServer.2005 BEGIN
	FieldDate       DATE              NULL,
	FieldTime       TIME(7)           NULL,
-- SKIP SqlServer.2005 END
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
)
GO
CREATE TABLE TestMergeIdentity
(
	Id     int NOT NULL IDENTITY(1,1) CONSTRAINT PK_TestMergeIdentity PRIMARY KEY CLUSTERED,
	Field  int NULL
)
GO

-- Generate schema
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestSchemaY') AND type in (N'U'))
BEGIN DROP TABLE TestSchemaY END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestSchemaX') AND type in (N'U'))
BEGIN DROP TABLE TestSchemaX END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestSchema.TestSchemaB') AND type in (N'U'))
BEGIN DROP TABLE TestSchema.TestSchemaB END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('TestSchema.TestSchemaA') AND type in (N'U'))
BEGIN
	DROP TABLE TestSchema.TestSchemaA
	DROP SCHEMA [TestSchema]
END
GO

EXEC('CREATE SCHEMA [TestSchema] AUTHORIZATION [dbo]');

CREATE TABLE [dbo].[TestSchemaX]
(
	[TestSchemaXID] int NOT NULL CONSTRAINT [PK_TestSchemaX] PRIMARY KEY,
	[Field1]        int NOT NULL
);
GO

CREATE TABLE [dbo].[TestSchemaY]
(
	[TestSchemaXID]       INT NOT NULL,
	[ParentTestSchemaXID] INT NOT NULL,
	[OtherID]             INT NOT NULL,
	CONSTRAINT [FK_TestSchemaY_TestSchemaX]       FOREIGN KEY (TestSchemaXID)       REFERENCES [TestSchemaX] ([TestSchemaXID]),
	CONSTRAINT [FK_TestSchemaY_ParentTestSchemaX] FOREIGN KEY (ParentTestSchemaXID) REFERENCES [TestSchemaX] ([TestSchemaXID]),
	CONSTRAINT [FK_TestSchemaY_OtherID]           FOREIGN KEY (TestSchemaXID)       REFERENCES [TestSchemaX] ([TestSchemaXID])
);
GO

CREATE TABLE [TestSchema].[TestSchemaA]
(
	[TestSchemaAID] int NOT NULL CONSTRAINT [PK_TestSchema_TestSchemaA] PRIMARY KEY,
	[Field1]        int NOT NULL
);
GO

CREATE TABLE [TestSchema].[TestSchemaB]
(
	[TestSchemaBID]           INT NOT NULL,
	[OriginTestSchemaAID]     INT NOT NULL,
	[TargetTestSchemaAID]     INT NOT NULL,
	[Target_Test_Schema_A_ID] INT NOT NULL,
	CONSTRAINT [PK_TestSchema_TestSchemaB] PRIMARY KEY (TestSchemaBID),
	CONSTRAINT [FK_TestSchema_TestSchemaBY_OriginTestSchemaA]  FOREIGN KEY (OriginTestSchemaAID)       REFERENCES [TestSchema].[TestSchemaA] ([TestSchemaAID]),
	CONSTRAINT [FK_TestSchema_TestSchemaBY_TargetTestSchemaA]  FOREIGN KEY (TargetTestSchemaAID)       REFERENCES [TestSchema].[TestSchemaA] ([TestSchemaAID]),
	CONSTRAINT [FK_TestSchema_TestSchemaBY_TargetTestSchemaA2] FOREIGN KEY ([Target_Test_Schema_A_ID]) REFERENCES [TestSchema].[TestSchemaA] ([TestSchemaAID])
);
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'AddIssue792Record')
	DROP Procedure AddIssue792Record
GO
CREATE Procedure AddIssue792Record
AS
BEGIN
	INSERT INTO dbo.AllTypes(char20DataType) VALUES('issue792')
END
GO
GRANT EXEC ON AddIssue792Record TO PUBLIC
GO
