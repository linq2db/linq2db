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

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('ParentView') AND type in (N'V'))
BEGIN DROP VIEW ParentView END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('ParentChildView') AND type in (N'V'))
BEGIN DROP VIEW ParentChildView END
GO


DROP TABLE Parent
GO
DROP TABLE Child
GO
DROP TABLE GrandChild
GO

CREATE TABLE Parent      (ParentID int, Value1 int, _ID INT IDENTITY PRIMARY KEY)
GO
CREATE TABLE Child       (ParentID int, ChildID int, _ID INT IDENTITY PRIMARY KEY)
GO
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int, _ID INT IDENTITY PRIMARY KEY)
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
	_ID            int IDENTITY  PRIMARY KEY,
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
	BigIntValue    bigint          NULL
)
GO
-- SKIP SqlAzure.2012 END
-- SKIP SqlServer.2012 END
-- SKIP SqlServer.2008 END

DROP TABLE TestIdentity
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
	PKField1    int NOT NULL,
	PKField2    int NOT NULL,
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

