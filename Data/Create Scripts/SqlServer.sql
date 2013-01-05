--CREATE DATABASE BLToolkitData ON PRIMARY
--(NAME=N'BLToolkitTest',     FILENAME=N'C:\Data\MSSQL.1\MSSQL\DATA\BLToolkitData.mdf',     SIZE=3072KB, FILEGROWTH=1024KB)
--LOG ON 
--(NAME=N'BLToolkitTest_log', FILENAME=N'C:\Data\MSSQL.1\MSSQL\DATA\BLToolkitData_log.ldf', SIZE=1024KB, FILEGROWTH=10%)
--GO

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

-- BinaryData Table

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('BinaryData') AND type in (N'U'))
BEGIN DROP TABLE BinaryData END

CREATE TABLE BinaryData
(
	BinaryDataID int             NOT NULL IDENTITY(1,1) CONSTRAINT PK_BinaryData PRIMARY KEY CLUSTERED,
	Stamp        timestamp       NOT NULL,
	Data         varbinary(1024) NOT NULL)
ON [PRIMARY]
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

-- ExecuteScalarTest

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Scalar_DataReader')
BEGIN DROP Procedure Scalar_DataReader END
GO

CREATE Procedure Scalar_DataReader
AS
SELECT Cast(12345 as int) AS intField, Cast('54321' as varchar(50)) AS stringField

GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Scalar_OutputParameter')
BEGIN DROP Procedure Scalar_OutputParameter END
GO

CREATE Procedure Scalar_OutputParameter
	@outputInt    int = 0 output,
	@outputString varchar(50) = '' output
AS
BEGIN
	SET @outputInt    = 12345
	SET @outputString = '54321'
END

GO

IF EXISTS (SELECT * FROM sys.objects WHERE type in (N'FN', N'IF', N'TF', N'FS', N'FT') AND name = 'Scalar_ReturnParameter')
BEGIN DROP Function Scalar_ReturnParameter END
GO

CREATE Function Scalar_ReturnParameter()
RETURNS int
AS
BEGIN
	RETURN 12345
END

GO

IF EXISTS (SELECT * FROM sys.objects WHERE type ='P' AND name = 'Scalar_ReturnParameterWithObject')
BEGIN DROP Procedure Scalar_ReturnParameterWithObject END
GO

CREATE Procedure Scalar_ReturnParameterWithObject
	@id int
AS
BEGIN
	SELECT * FROM Person WHERE PersonID = @id
	RETURN @id
END

GO

-- Data Types test

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('DataTypeTest') AND type in (N'U'))
BEGIN DROP TABLE DataTypeTest END
GO

CREATE TABLE DataTypeTest
(
	DataTypeID      int          NOT NULL IDENTITY(1,1) CONSTRAINT PK_DataType PRIMARY KEY CLUSTERED,
	Binary_         binary(50)       NULL,
	Boolean_        bit              NULL,
	Byte_           tinyint          NULL,
	Bytes_          varbinary(50)    NULL,
	Char_           char(1)          NULL,
	DateTime_       datetime         NULL,
	Decimal_        decimal(20,2)    NULL,
	Double_         float            NULL,
	Guid_           uniqueidentifier NULL,
	Int16_          smallint         NULL,
	Int32_          int              NULL,
	Int64_          bigint           NULL,
	Money_          money            NULL,
	SByte_          tinyint          NULL,
	Single_         real             NULL,
	Stream_         varbinary(50)    NULL,
	String_         nvarchar(50)     NULL,
	UInt16_         smallint         NULL,
	UInt32_         int              NULL,
	UInt64_         bigint           NULL,
	Xml_            xml              NULL
) ON [PRIMARY]
GO

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  Char_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,     Xml_)
VALUES
	(   NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL)
GO

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  Char_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,
	 Xml_)
VALUES
	(NewID(),        1,     255, NewID(),     'B', GetDate(), 12345.67,
	1234.567,  NewID(),   32767,   32768, 1000000,   12.3456,      127,
	1234.123,  NewID(), 'string',  32767,   32768, 200000000,
	'<root><element strattr="strvalue" intattr="12345"/></root>')
GO

-- SKIP SqlServer.2005 BEGIN
--
-- Arrays
--
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'ArrayTest')
BEGIN DROP PROCEDURE ArrayTest END
GO

--IF EXISTS (SELECT * FROM sys.objects WHERE type = 'T' AND name = 'IntArray')
--BEGIN
	DROP TYPE IntArray
--END
GO

CREATE TYPE IntArray AS TABLE
(
	Num int NULL
)
GO

CREATE PROCEDURE ArrayTest
	@InputIntArray IntArray READONLY
AS
BEGIN
	SELECT Num * 2 FROM @InputIntArray;
END
GO
-- SKIP SqlServer.2005 END

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
-- SKIP SqlServer.2008 END

DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity (
	ID int NOT NULL IDENTITY(1,1) CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED
)
GO
