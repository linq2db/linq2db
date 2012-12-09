IF OBJECT_ID('dbo.Doctor') IS NOT NULL
BEGIN DROP TABLE Doctor END
GO

IF OBJECT_ID('dbo.Patient') IS NOT NULL
BEGIN DROP TABLE Patient END
GO

-- Person Table

IF OBJECT_ID('dbo.Person') IS NOT NULL
BEGIN DROP TABLE Person END
GO

CREATE TABLE Person
(
	PersonID   int          IDENTITY,
	FirstName  nvarchar(50) NOT NULL,
	LastName   nvarchar(50) NOT NULL,
	MiddleName nvarchar(50)     NULL,
	Gender     char(1)      NOT NULL,
	CONSTRAINT PK_Person PRIMARY KEY CLUSTERED (PersonID)
)
GO

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M')
GO

-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID int          NOT NULL,
	Taxonomy nvarchar(50) NOT NULL,
	CONSTRAINT PK_Doctor        PRIMARY KEY CLUSTERED (PersonID),
	CONSTRAINT FK_Doctor_Person FOREIGN KEY (PersonID)
		REFERENCES Person(PersonID)
)
GO

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int           NOT NULL,
	Diagnosis nvarchar(256) NOT NULL,
	CONSTRAINT PK_Patient        PRIMARY KEY CLUSTERED (PersonID),
	CONSTRAINT FK_Patient_Person FOREIGN KEY (PersonID)
		REFERENCES Person (PersonID)
)
GO

INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
GO

-- Person_SelectByKey

IF OBJECT_ID('Person_SelectByKey') IS NOT NULL
BEGIN DROP Procedure Person_SelectByKey END
GO

CREATE Procedure Person_SelectByKey
	@id int
AS

SELECT * FROM Person WHERE PersonID = @id

GO

GRANT EXEC ON Person_SelectByKey TO PUBLIC
GO

-- Person_SelectAll

IF OBJECT_ID('Person_SelectAll') IS NOT NULL
BEGIN DROP Procedure Person_SelectAll END
GO

CREATE Procedure Person_SelectAll
AS

SELECT * FROM Person

GO

GRANT EXEC ON Person_SelectAll TO PUBLIC
GO

-- Person_SelectByName

IF OBJECT_ID('Person_SelectByName') IS NOT NULL
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

IF OBJECT_ID('Person_SelectListByName') IS NOT NULL
BEGIN DROP Procedure Person_SelectListByName END
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

IF OBJECT_ID('Person_Insert') IS NOT NULL
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

SELECT Cast(@@IDENTITY as int) PersonID

GO

GRANT EXEC ON Person_Insert TO PUBLIC
GO

-- Person_Insert_OutputParameter

IF OBJECT_ID('Person_Insert_OutputParameter') IS NOT NULL
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

SET @PersonID = Cast(@@IDENTITY as int)

GO

GRANT EXEC ON Person_Insert_OutputParameter TO PUBLIC
GO

-- Person_Update

IF OBJECT_ID('Person_Update') IS NOT NULL
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

IF OBJECT_ID('Person_Delete') IS NOT NULL
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

IF OBJECT_ID('Patient_SelectAll') IS NOT NULL
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

IF OBJECT_ID('Patient_SelectByName') IS NOT NULL
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

IF OBJECT_ID('BinaryData') IS NOT NULL
BEGIN DROP TABLE BinaryData END
GO

CREATE TABLE BinaryData
(
	BinaryDataID int             IDENTITY,
	Stamp        timestamp       NOT NULL,
	Data         varbinary(1024) NOT NULL,
	CONSTRAINT PK_BinaryData PRIMARY KEY CLUSTERED (BinaryDataID)
)
GO

-- OutRefTest

IF OBJECT_ID('OutRefTest') IS NOT NULL
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

IF OBJECT_ID('OutRefEnumTest') IS NOT NULL
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

IF OBJECT_ID('Scalar_DataReader') IS NOT NULL
BEGIN DROP Procedure Scalar_DataReader END
GO

CREATE Procedure Scalar_DataReader
AS
SELECT Cast(12345 as int) AS intField, Cast('54321' as varchar(50)) AS stringField

GO

IF OBJECT_ID('Scalar_OutputParameter') IS NOT NULL
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

IF OBJECT_ID('Scalar_ReturnParameterWithObject') IS NOT NULL
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

IF OBJECT_ID('DataTypeTest') IS NOT NULL
BEGIN DROP TABLE DataTypeTest END
GO

CREATE TABLE DataTypeTest
(
	DataTypeID      int              IDENTITY,
	Binary_         binary(50)       NULL,
	Boolean_        bit              NOT NULL,
	Byte_           tinyint          NULL,
	Bytes_          varbinary(50)    NULL,
	Char_           char(1)          NULL,
	DateTime_       datetime         NULL,
	Decimal_        decimal(20,2)    NULL,
	Double_         float            NULL,
	Guid_           varbinary(16)    NULL,
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
	Xml_            nvarchar(1000)   NULL,
    CONSTRAINT PK_DataType PRIMARY KEY CLUSTERED (DataTypeID)
)
GO

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  Char_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,     Xml_)
VALUES
	(   NULL,        0,    NULL,    NULL,    NULL,      NULL,     NULL,
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



DROP TABLE Parent
GO
DROP TABLE Child
GO
DROP TABLE GrandChild
GO

CREATE TABLE Parent      (ParentID int, Value1 int NULL)
GO
CREATE TABLE Child       (ParentID int, ChildID int)
GO
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int)
GO


DROP TABLE LinqDataTypes
GO

CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4) NULL,
	DateTimeValue  datetime      NULL,
	DateTimeValue2 datetime      NULL,
	BoolValue      bit,
	GuidValue      char(36)      NULL,
	BinaryValue    binary(500)   NULL,
	SmallIntValue  smallint      NULL,
	IntValue       int           NULL,
	BigIntValue    bigint        NULL
)
GO


DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity (
	ID int IDENTITY CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED
)
GO
