DROP SCHEMA IF EXISTS `{DBNAME}`
GO
CREATE SCHEMA `{DBNAME}`
GO
ALTER DATABASE `{DBNAME}` CHARACTER SET utf8 COLLATE utf8_general_ci
GO
USE `{DBNAME}`
GO
SET GLOBAL local_infile=ON;
GO
SET @@global.sql_mode=(SELECT REPLACE(@@global.sql_mode, 'ONLY_FULL_GROUP_BY', ''))
GO

CREATE TABLE InheritanceParent
(
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                varchar(50)      NULL,

	 CONSTRAINT PK_InheritanceParent PRIMARY KEY CLUSTERED (InheritanceParentId)
)
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
INSERT INTO Person (FirstName, LastName, MiddleName, Gender) VALUES ('Jürgen', 'König', 'Ko', 'M')
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

CREATE TABLE Parent     (ParentID int, Value1 int)
GO
CREATE TABLE Child      (ParentID int, ChildID int)
GO
CREATE INDEX IX_ChildIndex ON Child (ParentID)
GO
CREATE INDEX IX_ChildIndex2 ON Child (ParentID DESC)
GO
CREATE TABLE GrandChild (ParentID int, ChildID int, GrandChildID int)
GO

CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  datetime(3),
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

CREATE TABLE TestIdentity (
	ID int AUTO_INCREMENT NOT NULL,
	CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED (ID)
)
GO

CREATE TABLE `AllTypes`
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

INSERT INTO `AllTypes`
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
	1998,

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

CREATE TABLE `AllTypesNoYear`
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
	FieldDateTime   DATETIME(6)       NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       CHAR(36)          NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
	FieldDate       DATE              NULL,
-- SKIP MySql.5.7 BEGIN
-- SKIP MySql.8.0 BEGIN
-- SKIP MySqlConnector.5.7 BEGIN
-- SKIP MySqlConnector.8.0 BEGIN
	FieldTime       TIME(6)           NULL,
-- SKIP MySql.5.7 END
-- SKIP MySql.8.0 END
-- SKIP MySqlConnector.5.7 END
-- SKIP MySqlConnector.8.0 END
-- SKIP MariaDB.11 BEGIN
	FieldTime       TIME              NULL,
-- SKIP MariaDB.11 END
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
	FieldDateTime   DATETIME(6)       NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       CHAR(36)          NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
	FieldDate       DATE              NULL,
-- SKIP MySql.5.7 BEGIN
-- SKIP MySql.8.0 BEGIN
-- SKIP MySqlConnector.5.7 BEGIN
-- SKIP MySqlConnector.8.0 BEGIN
	FieldTime       TIME(6)           NULL,
-- SKIP MySql.5.7 END
-- SKIP MySql.8.0 END
-- SKIP MySqlConnector.5.7 END
-- SKIP MySqlConnector.8.0 END
-- SKIP MariaDB.11 BEGIN
	FieldTime       TIME              NULL,
-- SKIP MariaDB.11 END
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL,

	CONSTRAINT PK_TestMerge2 PRIMARY KEY CLUSTERED (Id)
)
GO

CREATE PROCEDURE TestProcedure(IN param3 INT, INOUT param2 INT, OUT param1 INT)
BEGIN
	SELECT param2 + param2 INTO param2;
	SELECT param3 + param2 INTO param1;
	SELECT * FROM Person;
END
GO
SET GLOBAL log_bin_trust_function_creators = 1;
GO
CREATE FUNCTION TestFunction(param INT)
RETURNS VARCHAR(10)
BEGIN
	RETURN 'done';
END
GO
CREATE PROCEDURE AddIssue792Record()
BEGIN
	INSERT INTO `AllTypes`(char20DataType) VALUES('issue792');
END
GO
CREATE PROCEDURE `TestOutputParametersWithoutTableProcedure`(
	IN `aInParam` VARCHAR(256),
	OUT `aOutParam` TINYINT(1)
)
BEGIN
	SELECT 123 INTO aOutParam;
END
GO

CREATE TABLE FullTextIndexTest (
	id int UNSIGNED AUTO_INCREMENT NOT NULL PRIMARY KEY,
	TestField1 TEXT(100),
	TestField2 TEXT(200),
	FULLTEXT idx_all (TestField1, TestField2),
	FULLTEXT idx_field1 (TestField1),
	FULLTEXT idx_field2 (TestField2)
)
GO
INSERT INTO FullTextIndexTest(TestField1, TestField2) VALUES('this is text1', 'this is text2');
INSERT INTO FullTextIndexTest(TestField1, TestField2) VALUES('looking for something?', 'found it!');
INSERT INTO FullTextIndexTest(TestField1, TestField2) VALUES('record not found', 'empty');
GO
CREATE TABLE Issue1993 (
	id			INTEGER UNSIGNED	NOT NULL   AUTO_INCREMENT,
	description	VARCHAR(100)		NULL,
PRIMARY KEY(id));
GO
CREATE PROCEDURE `Issue2313Parameters`(
	IN `VarCharDefault` VARCHAR(255),
	IN `VarChar1` VARCHAR(1),
	IN `Char255` CHAR(255),
	IN `Char1` CHAR(1),
	IN `VarBinary255` VARBINARY(255),
	IN `Binary255` BINARY(255),
	IN `TinyBlob` TINYBLOB,
	IN `Blob` BLOB,
	IN `MediumBlob` MEDIUMBLOB,
	IN `LongBlob` LONGBLOB,
	IN `TinyText` TINYTEXT,
	IN `Text` TEXT,
	IN `MediumText` MEDIUMTEXT,
	IN `LongText` LONGTEXT,
	IN `Date` DATE,
	IN `DateTime` DATETIME,
	IN `TimeStamp` TIMESTAMP,
	IN `Time` TIME,
	IN `Json` JSON,
	IN `TinyInt` TINYINT,
	IN `TinyIntUnsigned` TINYINT UNSIGNED,
	IN `SmallInt` SMALLINT,
	IN `SmallIntUnsigned` SMALLINT UNSIGNED,
	IN `MediumInt` MEDIUMINT,
	IN `MediumIntUnsigned` MEDIUMINT UNSIGNED,
	IN `Int` INT,
	IN `IntUnsigned` INT UNSIGNED,
	IN `BigInt` BIGINT,
	IN `BigIntUnsigned` BIGINT UNSIGNED,
	IN `Decimal` DECIMAL,
	IN `Float` FLOAT,
	IN `Double` DOUBLE,
	IN `Boolean` BOOLEAN,
	IN `Bit1` BIT,
	IN `Bit8` BIT(8),
	IN `Bit10` BIT(10),
	IN `Bit16` BIT(16),
	IN `Bit32` BIT(32),
	IN `Bit64` BIT(64),
	IN `Enum` ENUM('one', 'two'),
	IN `Set` ENUM('one', 'two'),
	IN `Year` YEAR,
	IN `Geometry` GEOMETRY,
	IN `Point` POINT,
	IN `LineString` LINESTRING,
	IN `Polygon` POLYGON,
	IN `MultiPoint` MULTIPOINT,
	IN `MultiLineString` MULTILINESTRING,
	IN `MultiPolygon` MULTIPOLYGON,
	IN `GeometryCollection` GEOMETRYCOLLECTION
)
BEGIN
	SELECT
	`VarCharDefault`,
	`VarChar1`,
	`Char255`,
	`Char1`,
	`VarBinary255`,
	`Binary255`,
	`TinyBlob`,
	`Blob`,
	`MediumBlob`,
	`LongBlob`,
	`TinyText`,
	`Text`,
	`MediumText`,
	`LongText`,
	`Date`,
	`DateTime`,
	`TimeStamp`,
	`Time`,
	`Json`,
	`TinyInt`,
	`TinyIntUnsigned`,
	`SmallInt`,
	`SmallIntUnsigned`,
	`MediumInt`,
	`MediumIntUnsigned`,
	`Int`,
	`IntUnsigned`,
	`BigInt`,
	`BigIntUnsigned`,
	`Decimal`,
	`Float`,
	`Double`,
	`Boolean`,
	`Bit1`,
	`Bit8`,
	`Bit10`,
	`Bit16`,
	`Bit32`,
	`Bit64`,
	`Enum`,
	`Set`,
	`Year`,
	`Geometry`,
	`Point`,
	`LineString`,
	`Polygon`,
	`MultiPoint`,
	`MultiLineString`,
	`MultiPolygon`,
	`GeometryCollection`
	FROM Person;
END
GO
CREATE PROCEDURE `Issue2313Results`(
	IN `VarCharDefault` VARCHAR(4000),
	IN `VarChar1` VARCHAR(1),
	IN `Char255` CHAR(255),
	IN `Char1` CHAR(1),
	IN `VarBinary255` VARBINARY(255),
	IN `Binary255` BINARY(255),
	IN `TinyBlob` TINYBLOB,
	IN `Blob` BLOB,
	IN `MediumBlob` MEDIUMBLOB,
	IN `LongBlob` LONGBLOB,
	IN `TinyText` TINYTEXT,
	IN `Text` TEXT,
	IN `MediumText` MEDIUMTEXT,
	IN `LongText` LONGTEXT,
	IN `Date` DATE,
	IN `DateTime` DATETIME,
	IN `TimeStamp` TIMESTAMP,
	IN `Time` TIME,
	IN `TinyInt` TINYINT,
	IN `TinyIntUnsigned` TINYINT UNSIGNED,
	IN `SmallInt` SMALLINT,
	IN `SmallIntUnsigned` SMALLINT UNSIGNED,
	IN `MediumInt` MEDIUMINT,
	IN `MediumIntUnsigned` MEDIUMINT UNSIGNED,
	IN `Int` INT,
	IN `IntUnsigned` INT UNSIGNED,
	IN `BigInt` BIGINT,
	IN `BigIntUnsigned` BIGINT UNSIGNED,
	IN `Decimal` DECIMAL,
	IN `Float` FLOAT,
	IN `Double` DOUBLE,
	IN `Boolean` BOOLEAN,
	IN `Bit1` BIT,
	IN `Bit8` BIT(8),
	IN `Bit10` BIT(10),
	IN `Bit16` BIT(16),
	IN `Bit32` BIT(32),
	IN `Bit64` BIT(64),
	IN `Enum` ENUM('one', 'two'),
	IN `Set` ENUM('one', 'two'),

-- SKIP MySql.5.7 BEGIN
-- SKIP MySql.8.0 BEGIN
	IN `Json` JSON,
	IN `Geometry` GEOMETRY,
	IN `Point` POINT,
	IN `LineString` LINESTRING,
	IN `Polygon` POLYGON,
	IN `MultiPoint` MULTIPOINT,
	IN `MultiLineString` MULTILINESTRING,
	IN `MultiPolygon` MULTIPOLYGON,
	IN `GeometryCollection` GEOMETRYCOLLECTION,
-- SKIP MySql.8.0 END
-- SKIP MySql.5.7 END

	IN `Year` YEAR
)
BEGIN
	SELECT
	`VarCharDefault`,
	`VarChar1`,
	`Char255`,
	`Char1`,
	`VarBinary255`,
	`Binary255`,
	`TinyBlob`,
	`Blob`,
	`MediumBlob`,
	`LongBlob`,
	`TinyText`,
	`Text`,
	`MediumText`,
	`LongText`,
	`Date`,
	`DateTime`,
	`TimeStamp`,
	`Time`,
	`TinyInt`,
	`TinyIntUnsigned`,
	`SmallInt`,
	`SmallIntUnsigned`,
	`MediumInt`,
	`MediumIntUnsigned`,
	`Int`,
	`IntUnsigned`,
	`BigInt`,
	`BigIntUnsigned`,
	`Decimal`,
	`Float`,
	`Double`,
	`Boolean`,
	`Bit1`,
	`Bit8`,
	`Bit10`,
	`Bit16`,
	`Bit32`,
	`Bit64`,
	`Enum`,
	`Set`,
	`Year`

-- SKIP MySql.5.7 BEGIN
-- SKIP MySql.8.0 BEGIN
	,`Json`,
	`Geometry`,
	`Point`,
	`LineString`,
	`Polygon`,
	`MultiPoint`,
	`MultiLineString`,
	`MultiPolygon`,
	`GeometryCollection`
-- SKIP MySql.8.0 END
-- SKIP MySql.5.7 END

	FROM Person;
END
GO

DROP TABLE `CollatedTable`
GO
CREATE TABLE `CollatedTable`
(
	`Id`				INT NOT NULL,
	`CaseSensitive`		VARCHAR(20) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL,
	`CaseInsensitive`	VARCHAR(20) CHARACTER SET utf8 COLLATE utf8_unicode_ci NOT NULL
)
GO

-- SKIP MySql.8.0 BEGIN
-- SKIP MySqlConnector.8.0 BEGIN
-- SKIP MySql.5.7 BEGIN
-- SKIP MySqlConnector.5.7 BEGIN

CREATE OR REPLACE FUNCTION TEST_FUNCTION(i INT) RETURNS INT RETURN i + 3

GO

CREATE OR REPLACE PROCEDURE TEST_PROCEDURE (IN i INT)
SELECT i + 3;
GO

SET SQL_MODE='ORACLE';
GO

CREATE OR REPLACE PACKAGE TEST_PACKAGE1 AS
	FUNCTION TEST_FUNCTION (i INT) RETURN INT;
	PROCEDURE TEST_PROCEDURE (i INT);
END;
GO

CREATE OR REPLACE PACKAGE BODY TEST_PACKAGE1 AS
	FUNCTION TEST_FUNCTION (i INT) RETURN INT AS
	BEGIN 
		RETURN i + 1;
	END TEST_FUNCTION;
	PROCEDURE TEST_PROCEDURE (i INT) AS
	BEGIN 
		SELECT i + 1;
	END TEST_PROCEDURE;
END TEST_PACKAGE1;
GO

CREATE OR REPLACE PACKAGE TEST_PACKAGE2 AS
	FUNCTION TEST_FUNCTION (i INT) RETURN INT;
	PROCEDURE TEST_PROCEDURE (i INT);
END;
GO

CREATE OR REPLACE PACKAGE BODY TEST_PACKAGE2 AS
	FUNCTION TEST_FUNCTION (i INT) RETURN INT AS
	BEGIN 
		RETURN i + 2;
	END TEST_FUNCTION;
	PROCEDURE TEST_PROCEDURE (i INT) AS
	BEGIN 
		SELECT i + 2;
	END TEST_PROCEDURE;
END TEST_PACKAGE2;
GO

set session sql_mode=default
GO

-- SKIP MySqlConnector.5.7 END
-- SKIP MySql.5.7 END
-- SKIP MySqlConnector.8.0 END
-- SKIP MySql.8.0 END
