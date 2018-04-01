DROP TABLE Doctor
GO

DROP TABLE Patient
GO

DROP TABLE Person
GO

DROP TABLE InheritanceParent
GO

CREATE TABLE InheritanceParent
(
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL,

	PRIMARY KEY(InheritanceParentId)
)
GO

DROP TABLE InheritanceChild
GO

CREATE TABLE InheritanceChild
(
	InheritanceChildId  int          NOT NULL,
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL,

	PRIMARY KEY(InheritanceChildId)
)
GO

CREATE TABLE Person
(
	PersonID   SERIAL       NOT NULL,
	FirstName  NVARCHAR(50) NOT NULL,
	LastName   NVARCHAR(50) NOT NULL,
	MiddleName NVARCHAR(50),
	Gender     CHAR(1)      NOT NULL,

	PRIMARY KEY(PersonID)
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
-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID int          NOT NULL,
	Taxonomy nvarchar(50) NOT NULL,

	PRIMARY KEY (PersonID),
	FOREIGN KEY (PersonID) REFERENCES Person (PersonID)
)
GO

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int           NOT NULL,
	Diagnosis nvarchar(100) NOT NULL,

	PRIMARY KEY (PersonID),
	FOREIGN KEY (PersonID) REFERENCES Person (PersonID)
)
GO

INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
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


DROP TABLE LinqDataTypes
GO

CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  datetime year to fraction(3),
	DateTimeValue2 datetime year to fraction(3),
	BoolValue      boolean,
	GuidValue      char(36),
	BinaryValue    byte,
	SmallIntValue  smallint,
	IntValue       int,
	BigIntValue    bigint,
	StringValue    NVARCHAR(50)
)
GO

DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity (
	ID SERIAL NOT NULL,
	PRIMARY KEY(ID)
)
GO

DROP TABLE AllTypes
GO

CREATE TABLE AllTypes
(
	ID   SERIAL                          NOT NULL,

	bigintDataType   bigint                  NULL,
	int8DataType     Int8                    NULL,
	intDataType      int                     NULL,
	smallintDataType smallint                NULL,
	decimalDataType  decimal                 NULL,
	moneyDataType    money                   NULL,
	realDataType     real                    NULL,
	floatDataType    float                   NULL,

	boolDataType     boolean                 NULL,

	charDataType     char(1)                 NULL,
	char20DataType   char(20)                NULL,
	varcharDataType  varchar(10)             NULL,
	ncharDataType    nchar(10)               NULL,
	nvarcharDataType nvarchar(10)            NULL,
	lvarcharDataType lvarchar(10)            NULL,
	textDataType     text                    NULL,

	dateDataType     date                    NULL,
	datetimeDataType datetime year to second NULL,
	intervalDataType interval hour to second NULL,

	byteDataType     byte                    NULL,

	PRIMARY KEY(ID)
)
GO

INSERT INTO AllTypes (bigintDataType) VALUES (NULL)
GO

INSERT INTO AllTypes
(
	bigintDataType,
	int8DataType,
	intDataType,
	smallintDataType,
	decimalDataType,
	moneyDataType,
	realDataType,
	floatDataType,

	boolDataType,

	charDataType,
	varcharDataType,
	ncharDataType,
	nvarcharDataType,
	lvarcharDataType,
	textDataType,

	dateDataType,
	datetimeDataType,
	intervalDataType
)
VALUES
(
	1000000,
	1000001,
	7777777,
	100,
	9999999,
	8888888,
	20.31,
	16.2,

	't',

	'1',
	'234',
	'55645',
	'6687',
	'AAAAA',
	'BBBBB',

	datetime(2012-12-12) year to day,
	datetime(2012-12-12 12:12:12) year to second,
	interval(12:12:12) hour to second
)
GO

DROP VIEW PersonView
GO

CREATE VIEW PersonView
AS
SELECT * FROM Person
GO

DROP TABLE TestUnique
GO

CREATE TABLE TestUnique (
	ID1 INT NOT NULL,
	ID2 INT NOT NULL,
	ID3 INT NOT NULL,
	ID4 INT NOT NULL,
	PRIMARY KEY(ID1,ID2),
	UNIQUE(ID3,ID4)
)
GO

DROP TABLE TestFKUnique
GO

CREATE TABLE TestFKUnique (
	ID1 INT NOT NULL,
	ID2 INT NOT NULL,
	ID3 INT NOT NULL,
	ID4 INT NOT NULL,
	FOREIGN KEY (ID1,ID2) REFERENCES TestUnique (ID1,ID2),
	FOREIGN KEY (ID3,ID4) REFERENCES TestUnique (ID3,ID4)
)
GO


DROP TABLE TestMerge1
GO
DROP TABLE TestMerge2
GO

CREATE TABLE TestMerge1
(
	Id       int          NOT NULL,
	Field1   int              NULL,
	Field2   int              NULL,
	Field3   int              NULL,
	Field4   int              NULL,
	Field5   int              NULL,

	FieldInt64      BIGINT                       NULL,
	FieldBoolean    BOOLEAN                      NULL,
	FieldString     VARCHAR(20)                  NULL,
	FieldChar       CHAR(1)                      NULL,
	FieldFloat      REAL                         NULL,
	FieldDouble     FLOAT                        NULL,
	FieldDateTime   DATETIME YEAR TO fraction(3) NULL,
	FieldBinary     BYTE                         NULL,
	FieldDecimal    DECIMAL(24, 10)              NULL,
	FieldDate       DATE                         NULL,
	FieldTime       INTERVAL HOUR TO fraction(5) NULL,
	FieldEnumString VARCHAR(20)                  NULL,
	FieldEnumNumber INT                          NULL,

	PRIMARY KEY(Id)
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

	FieldInt64      BIGINT                       NULL,
	FieldBoolean    BOOLEAN                      NULL,
	FieldString     VARCHAR(20)                  NULL,
	FieldChar       CHAR(1)                      NULL,
	FieldFloat      REAL                         NULL,
	FieldDouble     FLOAT                        NULL,
	FieldDateTime   DATETIME YEAR TO fraction(3) NULL,
	FieldBinary     BYTE                         NULL,
	FieldDecimal    DECIMAL(24, 10)              NULL,
	FieldDate       DATE                         NULL,
	FieldTime       INTERVAL HOUR TO fraction(5) NULL,
	FieldEnumString VARCHAR(20)                  NULL,
	FieldEnumNumber INT                          NULL,

	PRIMARY KEY(Id)
)
GO

DROP PROCEDURE IF EXISTS AddIssue792Record
GO
CREATE PROCEDURE AddIssue792Record()
	INSERT INTO AllTypes(char20DataType) VALUES('issue792');
END PROCEDURE
GO
