
DROP TABLE IF EXISTS Doctor
GO
DROP TABLE IF EXISTS Patient
GO

DROP TABLE IF EXISTS InheritanceParent
GO
CREATE TABLE InheritanceParent
(
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                varchar(50)      NULL,

	 CONSTRAINT PK_InheritanceParent PRIMARY KEY CLUSTERED (InheritanceParentId)
)
GO

DROP TABLE IF EXISTS InheritanceChild
GO
CREATE TABLE InheritanceChild
(
	InheritanceChildId  int          NOT NULL,
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                varchar(50)      NULL,

	 CONSTRAINT PK_InheritanceChild PRIMARY KEY CLUSTERED (InheritanceChildId)
)
GO

-- Person Table

DROP TABLE IF EXISTS Person
GO

CREATE TABLE Person
(
	PersonID   int         AUTO_INCREMENT NOT NULL,
	FirstName  varchar(50) NOT NULL,
	LastName   varchar(50) NOT NULL,
	MiddleName varchar(50)     NULL,
	Gender     char(1)     NOT NULL,
	CONSTRAINT PK_Person PRIMARY KEY CLUSTERED (PersonID)
)
GO

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jane',   'Doe',       'F')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jürgen', 'König',     'M')
GO

CREATE OR REPLACE VIEW PersonView AS SELECT * FROM Person
GO

-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID int         NOT NULL,
	Taxonomy varchar(50) NOT NULL,
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
	PersonID  int          NOT NULL,
	Diagnosis varchar(256) NOT NULL,
	CONSTRAINT PK_Patient        PRIMARY KEY CLUSTERED (PersonID),
	CONSTRAINT FK_Patient_Person FOREIGN KEY (PersonID)
		REFERENCES Person (PersonID)
)
GO

INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
GO


-- Data Types test

DROP TABLE IF EXISTS DataTypeTest
GO

CREATE TABLE DataTypeTest
(
	DataTypeID      int              AUTO_INCREMENT NOT NULL,
	Binary_         binary(50)       NULL,
	Boolean_        bit              NOT NULL,
	Byte_           tinyint          NULL,
	Bytes_          varbinary(50)    NULL,
	Char_           char(1)          NULL,
	DateTime_       datetime         NULL,
	Decimal_        decimal(20,2)    NULL,
	Double_         float            NULL,
	Guid_           varbinary(50)    NULL,
	Int16_          smallint         NULL,
	Int32_          int              NULL,
	Int64_          bigint           NULL,
	Money_          decimal(20,4)    NULL,
	SByte_          tinyint          NULL,
	Single_         real             NULL,
	Stream_         varbinary(50)    NULL,
	String_         varchar(50)      NULL,
	UInt16_         smallint         NULL,
	UInt32_         int              NULL,
	UInt64_         bigint           NULL,
	Xml_            varchar(1000)    NULL,
	CONSTRAINT PK_DataType PRIMARY KEY CLUSTERED (DataTypeID)
)
GO

DROP TABLE IF EXISTS Parent
GO
DROP TABLE IF EXISTS Child
GO
DROP TABLE IF EXISTS GrandChild
GO

CREATE TABLE Parent     (ParentID int, Value1 int)
GO
CREATE TABLE Child      (ParentID int, ChildID int)
GO
CREATE TABLE GrandChild (ParentID int, ChildID int, GrandChildID int)
GO


DROP TABLE IF EXISTS LinqDataTypes
GO

CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  datetime
-- SKIP MySql BEGIN
	(3)
-- SKIP MySql END
	,
	DateTimeValue2 datetime NULL,
	BoolValue      boolean,
	GuidValue      char(36),
	BinaryValue    varbinary(5000) NULL,
	SmallIntValue  smallint,
	IntValue       int             NULL,
	BigIntValue    bigint          NULL,
	StringValue    varchar(50)     NULL
)
GO

DROP TABLE IF EXISTS TestIdentity
GO

CREATE TABLE TestIdentity (
	ID int AUTO_INCREMENT NOT NULL,
	CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED (ID)
)
GO


DROP TABLE IF EXISTS AllTypes
GO

CREATE TABLE AllTypes
(
	ID                  int AUTO_INCREMENT       NOT NULL,

	bigintDataType      bigint                       NULL,
	smallintDataType    smallint                     NULL,
	tinyintDataType     tinyint                      NULL,
	mediumintDataType   mediumint                    NULL,
	intDataType         int                          NULL,
	numericDataType     numeric                      NULL,
	decimalDataType     decimal                      NULL,
	doubleDataType      double                       NULL,
	floatDataType       float                        NULL,

	dateDataType        date                         NULL,
	datetimeDataType    datetime                     NULL,
	timestampDataType   timestamp                    NULL,
	timeDataType        time                         NULL,
	yearDataType        year                         NULL,
-- SKIP MySql57 BEGIN
	year2DataType       year(2)                      NULL,
-- SKIP MySql57 END
-- SKIP MySql BEGIN
-- SKIP MariaDB BEGIN
	year2DataType       year(4)                      NULL,
-- SKIP MySql END
-- SKIP MariaDB END
	year4DataType       year(4)                      NULL,

	charDataType        char(1)                      NULL,
	char20DataType      char(20)                     NULL,
	varcharDataType     varchar(20)                  NULL,
	textDataType        text                         NULL,

	binaryDataType      binary(3)                    NULL,
	varbinaryDataType   varbinary(5)                 NULL,
	blobDataType        blob                         NULL,

	bitDataType         bit(3)                       NULL,
	enumDataType        enum('Green', 'Red', 'Blue') NULL,
	setDataType         set('one', 'two')            NULL,
	intUnsignedDataType int unsigned                 NULL,
	boolDataType        bool                         NULL,

	CONSTRAINT PK_AllTypes PRIMARY KEY CLUSTERED (ID)
)
GO

INSERT INTO AllTypes
(
	bigintDataType,
	smallintDataType,
	tinyintDataType,
	mediumintDataType,
	intDataType,
	numericDataType,
	decimalDataType,
	doubleDataType,
	floatDataType,

	dateDataType,
	datetimeDataType,
	timestampDataType,
	timeDataType,
	yearDataType,
	year2DataType,
	year4DataType,

	charDataType,
	varcharDataType,
	textDataType,

	binaryDataType,
	varbinaryDataType,
	blobDataType,

	bitDataType,
	enumDataType,
	setDataType,
	boolDataType
)
SELECT
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,

	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,

	NULL,
	NULL,
	NULL,

	NULL,
	NULL,
	NULL,

	NULL,
	NULL,
	NULL,
	NULL
UNION ALL
SELECT
	1000000,
	25555,
	111,
	5555,
	7777777,
	9999999,
	8888888,
	20.31,
	16.0,

	'2012-12-12',
	'2012-12-12 12:12:12',
	'2012-12-12 12:12:12',
	'12:12:12',
	98,
	'97',
	'2012',

	'1',
	'234',
	'567',

	'abc',
	'cde',
	'def',

	B'101',
	'Green',
	'one',
	1

GO


DROP TABLE IF EXISTS TestSameName
GO

DROP TABLE IF EXISTS test_schema.TestSameName
GO

DROP SCHEMA IF EXISTS test_schema
GO

CREATE SCHEMA test_schema
GO

CREATE TABLE test_schema.TestSameName
(
	ID int NOT NULL PRIMARY KEY
)
GO

CREATE TABLE TestSameName
(
	ID int NOT NULL PRIMARY KEY
)
GO

CREATE OR REPLACE
VIEW PersonView
AS
	SELECT `Person`.`PersonID` AS `ID`
	FROM `Person`
	WHERE (`Person`.`Gender` = 'M')
GO

-- merge test tables
DROP TABLE IF EXISTS TestMerge1
GO
DROP TABLE IF EXISTS TestMerge2
GO
CREATE TABLE TestMerge1
(
	Id       int          NOT NULL,
	Field1   int              NULL,
	Field2   int              NULL,
	Field3   int              NULL,
	Field4   int              NULL,
	Field5   int              NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BIT               NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      FLOAT             NULL,
	FieldDouble     DOUBLE            NULL,
	FieldDateTime   DATETIME          NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       CHAR(36)          NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
	FieldDate       DATE              NULL,
	FieldTime       TIME              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL,

	CONSTRAINT PK_TestMerge1 PRIMARY KEY CLUSTERED (Id)
)
GO
CREATE TABLE TestMerge2
(
	Id       int          NOT NULL,
	Field1   int              NULL,
	Field2   int              NULL,
	Field3   int              NULL,
	Field4   int              NULL,
	Field5   int              NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BIT               NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      FLOAT             NULL,
	FieldDouble     DOUBLE            NULL,
	FieldDateTime   DATETIME          NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       CHAR(36)          NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
	FieldDate       DATE              NULL,
	FieldTime       TIME              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL,

	CONSTRAINT PK_TestMerge2 PRIMARY KEY CLUSTERED (Id)
)
GO
DROP PROCEDURE IF EXISTS TestProcedure
GO
DROP FUNCTION IF EXISTS TestFunction
GO
CREATE PROCEDURE TestProcedure(IN param3 INT, INOUT param2 INT, OUT param1 INT)
BEGIN
	SELECT param2 + param2 INTO param2;
	SELECT param3 + param2 INTO param1;
	SELECT * FROM Person;
END
GO
CREATE FUNCTION TestFunction(param INT)
RETURNS VARCHAR(10)
BEGIN
	RETURN 'done';
END
GO
DROP PROCEDURE IF EXISTS AddIssue792Record
GO
CREATE PROCEDURE AddIssue792Record()
BEGIN
	INSERT INTO AllTypes(char20DataType) VALUES('issue792');
END
GO
DROP PROCEDURE IF EXISTS `test_proc`
GO
CREATE PROCEDURE `test_proc`(
	IN `aInParam` VARCHAR(256),
	OUT `aOutParam` TINYINT(1)
)
BEGIN
	SELECT 1 INTO aOutParam;
END
GO
