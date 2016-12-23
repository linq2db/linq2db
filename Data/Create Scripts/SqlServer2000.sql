-- Person Table

DROP TABLE Doctor
DROP TABLE Patient
DROP TABLE Person

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

DROP Procedure Person_SelectByKey
GO

CREATE Procedure Person_SelectByKey
	@id int
AS

SELECT * FROM Person WHERE PersonID = @id

GO

GRANT EXEC ON Person_SelectByKey TO PUBLIC
GO

-- Person_SelectAll

DROP Procedure Person_SelectAll
GO

CREATE Procedure Person_SelectAll
AS

SELECT * FROM Person

GO

GRANT EXEC ON Person_SelectAll TO PUBLIC
GO

-- Person_SelectByName

DROP Procedure Person_SelectByName
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

DROP Procedure Person_SelectListByName
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

DROP Procedure Person_Insert
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

DROP Procedure Person_Insert_OutputParameter
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

DROP Procedure Person_Update
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

DROP Procedure Person_Delete
GO

CREATE Procedure Person_Delete
	@PersonID int
AS

DELETE FROM Person WHERE PersonID = @PersonID

GO

GRANT EXEC ON Person_Delete TO PUBLIC
GO

-- Patient_SelectAll

DROP Procedure Patient_SelectAll
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

DROP Procedure Patient_SelectByName
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

DROP Procedure OutRefTest
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

DROP Procedure OutRefEnumTest
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

DROP TABLE AllTypes
GO

CREATE TABLE AllTypes
(
	ID                       int          NOT NULL IDENTITY(1,1) CONSTRAINT PK_AllTypes PRIMARY KEY CLUSTERED,

	bigintDataType           bigint           NULL,
	numericDataType          numeric(18,1)    NULL,
	bitDataType              bit              NULL,
	smallintDataType         smallint         NULL,
	decimalDataType          decimal(18,1)    NULL,
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

	nvarchar_max_DataType    nvarchar(4000)   NULL,
	varchar_max_DataType     varchar(4000)    NULL,
	varbinary_max_DataType   varbinary(4000)  NULL,

	xmlDataType              nvarchar(2000)   NULL,
	datetime2DataType        varchar(50)      NULL,
	datetimeoffsetDataType   varchar(50)      NULL,
	datetimeoffset0DataType  varchar(50)      NULL,
	datetimeoffset1DataType  varchar(50)      NULL,
	datetimeoffset2DataType  varchar(50)      NULL,
	datetimeoffset3DataType  varchar(50)      NULL,
	datetimeoffset4DataType  varchar(50)      NULL,
	datetimeoffset5DataType  varchar(50)      NULL,
	datetimeoffset6DataType  varchar(50)      NULL,
	datetimeoffset7DataType  varchar(50)      NULL,
	dateDataType             varchar(50)      NULL,
	timeDataType             varchar(50)      NULL
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

-- GetParentByID function

DROP FUNCTION GetParentByID
GO

DROP VIEW ParentView
GO

DROP VIEW ParentChildView
GO


DROP TABLE Parent
GO
DROP TABLE Child
GO
DROP TABLE GrandChild
GO

CREATE TABLE Parent      (ParentID int, Value1 int)
GO
CREATE TABLE Child       (ParentID int, ChildID int, TypeDiscriminator int NULL)
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

DROP TABLE LinqDataTypes
GO

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

DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity
(
	ID int NOT NULL IDENTITY(1,1) CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED
)
GO


-- IndexTable
DROP TABLE IndexTable2
GO

DROP TABLE IndexTable
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


DROP Procedure SelectImplicitColumn
GO

CREATE PROCEDURE SelectImplicitColumn
AS
BEGIN
	SELECT 123
END
GO


DROP Procedure DuplicateColumnNames
GO

CREATE PROCEDURE DuplicateColumnNames
AS
BEGIN
	SELECT 123 as id, '456' as id
END
GO

DROP TABLE [DecimalOverflow]
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
