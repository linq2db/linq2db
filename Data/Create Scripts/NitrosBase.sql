DROP TABLE IF EXISTS Doctor
GO
DROP TABLE IF EXISTS Patient
GO
DROP TABLE IF EXISTS InheritanceParent
GO
DROP TABLE IF EXISTS InheritanceChild
GO
DROP TABLE IF EXISTS Person
GO
DROP TABLE IF EXISTS AllTypes
GO
DROP TABLE IF EXISTS Parent
GO
DROP TABLE IF EXISTS Child
GO
DROP TABLE IF EXISTS GrandChild
GO
DROP TABLE IF EXISTS LinqDataTypes
GO
DROP TABLE IF EXISTS TestIdentity
GO
DROP TABLE IF EXISTS TestMerge1
GO
DROP TABLE IF EXISTS TestMerge2
GO
DROP TABLE IF EXISTS TestMergeIdentity
GO
DROP TABLE IF EXISTS CollatedTable
GO

CREATE TABLE InheritanceParent
(
	InheritanceParentId INT          NOT NULL PRIMARY KEY,
	TypeDiscriminator   INT              NULL,
	Name                VARCHAR(50)      NULL
)
GO
CREATE TABLE InheritanceChild
(
	InheritanceChildId  INT          NOT NULL PRIMARY KEY,
	InheritanceParentId INT          NOT NULL,
	TypeDiscriminator   INT              NULL,
	Name                VARCHAR(50)      NULL
)
GO
CREATE TABLE Person
(
	PersonID   INT         NOT NULL IDENTITY PRIMARY KEY,
	FirstName  VARCHAR(50) NOT NULL,
	LastName   VARCHAR(50) NOT NULL,
	MiddleName VARCHAR(50)     NULL,
	Gender     CHAR(1)         NOT NULL
)
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jane',   'Doe',       'F')
GO
INSERT INTO Person (FirstName, LastName, MiddleName, Gender) VALUES (N'Jürgen', N'König', 'Ko', 'M')
GO

CREATE TABLE Doctor
(
	PersonID INT         NOT NULL PRIMARY KEY CONSTRAINT FK_Doctor_Person FOREIGN KEY REFERENCES Person (PersonID)
	Taxonomy VARCHAR(50) NOT NULL
)
GO
INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry')
GO
CREATE TABLE Patient
(
	PersonID  INT          NOT NULL PRIMARY KEY CONSTRAINT FK_Patient_Person FOREIGN KEY REFERENCES Person (PersonID)
	Diagnosis VARCHAR(256) NOT NULL
)
GO
INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
GO
CREATE TABLE AllTypes
(
	ID                       INT           NOT NULL IDENTITY PRIMARY KEY,

	bitDataType              BIT               NULL,
	intDataType              INT               NULL,
	bigintDataType           BIGINT            NULL,
	realDataType             REAL              NULL,
	dateDataType             DATE              NULL,
	datetime2DataType        DATETIME2         NULL,
	charDataType             CHAR(1)           NULL,
	char20DataType           CHAR(20)          NULL,
	varcharDataType          VARCHAR(20)       NULL,
	varbinaryDataType        VARBINARY         NULL
)
GO
INSERT INTO AllTypes
(
	bitDataType, intDataType, bigintDataType, realDataType,
	dateDataType, datetime2DataType,
	charDataType, char20DataType, varcharDataType,
	varbinaryDataType
)
SELECT
		 NULL,      NULL,  NULL,    NULL,
		 NULL,      NULL,
		 NULL,      NULL,  NULL,
		 NULL,      NULL,  NULL,
		 NULL
UNION ALL
SELECT
	1, 7777777, 1000000, 16.2,
	'2012-12-12', '2012-12-12 12:12:12.123',
	'1', '123', '234',
	0x010203
GO
CREATE TABLE Parent     (ParentID INT, Value1 INT,  _ID INT IDENTITY PRIMARY KEY)
GO
CREATE TABLE Child      (ParentID INT, ChildID INT, _ID INT IDENTITY PRIMARY KEY)
GO
CREATE TABLE GrandChild (ParentID INT, ChildID INT, GrandChildID INT, _ID INT IDENTITY PRIMARY KEY)
GO
CREATE TABLE LinqDataTypes
(
	_ID            INT IDENTITY PRIMARY KEY,
	ID             INT,
	MoneyValue     REAL,
	DateTimeValue  DATETIME2,
	DateTimeValue2 DATETIME2,
	BoolValue      BIT,
	GuidValue      VARCHAR(36),
	BinaryValue    VARBINARY(5000),
	SmallIntValue  INT,
	IntValue       INT NULL,
	BigIntValue    BIGINT NULL,
	StringValue    VARCHAR(50) NULL
)
GO
CREATE TABLE TestIdentity (
	ID INT NOT NULL IDENTITY PRIMARY KEY
)
GO
CREATE TABLE TestMerge1
(
	Id     INT NOT NULL PRIMARY KEY,
	Field1 INT NULL,
	Field2 INT NULL,
	Field3 INT NULL,
	Field4 INT NULL,
	Field5 INT NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BIT               NULL,
	FieldNString    VARCHAR(20)       NULL,
	FieldNChar      CHAR(1)           NULL,
	FieldDouble     REAL              NULL,
	FieldDateTime   DATETIME2         NULL,
	FieldDateTime2  DATETIME2(7)      NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldDate       DATE              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
)
GO

CREATE TABLE TestMerge2
(
	Id     INT NOT NULL PRIMARY KEY,
	Field1 INT NULL,
	Field2 INT NULL,
	Field3 INT NULL,
	Field4 INT NULL,
	Field5 INT NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BIT               NULL,
	FieldNString    VARCHAR(20)       NULL,
	FieldNChar      CHAR(1)           NULL,
	FieldDouble     REAL              NULL,
	FieldDateTime   DATETIME2         NULL,
	FieldDateTime2  DATETIME2(7)      NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldDate       DATE              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
)
GO
CREATE TABLE TestMergeIdentity
(
	Id     INT NOT NULL IDENTITY PRIMARY KEY,
	Field  INT NULL
)
GO
CREATE TABLE CollatedTable
(
	Id				INT         NOT NULL,
	CaseSensitive	VARCHAR(20) NOT NULL,
	CaseInsensitive	VARCHAR(20) NOT NULL
)
GO
