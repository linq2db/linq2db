
DROP TABLE Doctor
GO
DROP TABLE Patient
GO

-- Person Table

DROP TABLE Person
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

DROP TABLE DataTypeTest
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

DROP TABLE Parent
GO
DROP TABLE Child
GO
DROP TABLE GrandChild
GO

CREATE TABLE Parent     (ParentID int, Value1 int)
GO
CREATE TABLE Child      (ParentID int, ChildID int)
GO
CREATE TABLE GrandChild (ParentID int, ChildID int, GrandChildID int)
GO


DROP TABLE LinqDataTypes
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
	BigIntValue    bigint          NULL
)
GO

DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity (
	ID int AUTO_INCREMENT NOT NULL,
	CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED (ID)
)
GO


DROP TABLE AllTypes
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
	year2DataType       year(2)                      NULL,
	year4DataType       year(4)                      NULL,

	charDataType        char(1)                      NULL,
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


DROP TABLE TestSameName
GO

DROP TABLE test_schema.TestSameName
GO

DROP SCHEMA test_schema
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
