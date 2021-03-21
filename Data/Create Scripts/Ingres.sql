/*
-- Helper table
*/
DROP TABLE IF EXISTS Dual;
CREATE TABLE Dual (Dummy  VARCHAR(10));
INSERT INTO  Dual (Dummy) VALUES ('X');

DROP TABLE IF EXISTS InheritanceParent;
CREATE TABLE InheritanceParent
(
	InheritanceParentId integer      NOT NULL CONSTRAINT PK_InheritanceParent PRIMARY KEY,
	TypeDiscriminator   integer          NULL,
	Name                nvarchar(50)     NULL
);

DROP TABLE IF EXISTS InheritanceChild;
CREATE TABLE InheritanceChild
(
	InheritanceChildId  integer      NOT NULL CONSTRAINT PK_InheritanceChild PRIMARY KEY,
	InheritanceParentId integer      NOT NULL,
	TypeDiscriminator   integer          NULL,
	Name                nvarchar(50)     NULL
);

/*
-- Person Table
*/
DROP TABLE IF EXISTS Doctor;
DROP TABLE IF EXISTS Patient;
DROP TABLE IF EXISTS Person;
DROP SEQUENCE IF EXISTS PersonSequence;

CREATE SEQUENCE PersonSequence;
CREATE TABLE Person
(
	PersonID   integer      NOT NULL CONSTRAINT PK_Person PRIMARY KEY WITH DEFAULT NEXT VALUE FOR PersonSequence,
	FirstName  nvarchar(50) NOT NULL,
	LastName   nvarchar(50) NOT NULL,
	MiddleName nvarchar(50)     NULL,
	Gender     char(1)      NOT NULL CONSTRAINT CK_Person_Gender CHECK (Gender in ('M', 'F', 'U', 'O'))
);

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M');
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M');
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jane',   'Doe',       'F');
INSERT INTO Person (FirstName, LastName, MiddleName, Gender) VALUES ('Jürgen', 'König', 'Ko', 'M');

/*
-- Doctor Table Extension
*/
CREATE TABLE Doctor
(
	PersonID integer      NOT NULL CONSTRAINT PK_Doctor PRIMARY KEY,
	Taxonomy nvarchar(50) NOT NULL,
	CONSTRAINT FK_Doctor_Person FOREIGN KEY(PersonID) REFERENCES Person(PersonID)
);

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');

/*
-- Patient Table Extension
*/
CREATE TABLE Patient
(
	PersonID  integer       NOT NULL CONSTRAINT PK_Patient PRIMARY KEY,
	Diagnosis nvarchar(256) NOT NULL,

	CONSTRAINT FK_Patient_Person FOREIGN KEY(PersonID) REFERENCES Person(PersonID)
);
INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution');

/*-
-- Babylon test
-*/
DROP TABLE IF EXISTS Parent;
DROP TABLE IF EXISTS Child;
DROP TABLE IF EXISTS GrandChild;

CREATE TABLE Parent      (ParentID int, Value1 int);
CREATE TABLE Child       (ParentID int, ChildID int);
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int);

DROP TABLE IF EXISTS LinqDataTypes;
CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  date,
	DateTimeValue2 date,
	BoolValue      boolean,
	GuidValue      byte(16),
	BinaryValue    binary(5000) NULL,
	SmallIntValue  smallint,
	IntValue       int          NULL,
	BigIntValue    bigint       NULL,
	StringValue    nvarchar(50) NULL
);


DROP TABLE IF EXISTS TestIdentity;
DROP SEQUENCE IF EXISTS TestIdentitySequence;

CREATE SEQUENCE TestIdentitySequence;
CREATE TABLE TestIdentity (
	ID integer NOT NULL CONSTRAINT PK_TestIdentity PRIMARY KEY WITH DEFAULT NEXT VALUE FOR TestIdentitySequence
);

/*
DROP TABLE IF EXISTS AllTypes;
DROP SEQUENCE IF EXISTS AllTypesSequence;

CREATE SEQUENCE AllTypesSequence;
CREATE TABLE AllTypes
(
	ID                       integer          NOT NULL CONSTRAINT PK_AllTypes PRIMARY KEY WITH DEFAULT NEXT VALUE FOR AllTypesSequence,

	bigintDataType           bigint           NULL,
	numericDataType          numeric          NULL,
	bitDataType              byte(1)          NULL,
	smallintDataType         smallint         NULL,
	decimalDataType          decimal          NULL,
	intDataType              int              NULL,
	tinyintDataType          tinyint          NULL,
	moneyDataType            money            NULL,
	floatDataType            float            NULL,
	realDataType             real             NULL,

	datetimeDataType         date             NULL,

	charDataType             char(1)          NULL,
	char20DataType           char(20)         NULL,
	varcharDataType          varchar(20)      NULL,
	textDataType             text             NULL,
	ncharDataType            nchar(20)        NULL,
	nvarcharDataType         nvarchar(20)     NULL,
	ntextDataType            text            NULL,

	binaryDataType           binary           NULL,
	varbinaryDataType        varbinary        NULL,
	imageDataType            byte             NULL,

	uniqueidentifierDataType byte(16)         NULL,
	objectDataType           byte             NULL
);

INSERT INTO AllTypes
(
	bigintDataType, numericDataType, bitDataType, smallintDataType, decimalDataType,
	intDataType, tinyintDataType, moneyDataType, floatDataType, realDataType,
	datetimeDataType,
	charDataType, varcharDataType, textDataType, ncharDataType, nvarcharDataType, ntextDataType,
	objectDataType
)
SELECT
		 NULL,      NULL,  NULL,    NULL,    NULL,   NULL,  NULL,   NULL,  NULL, NULL,
		 NULL,
		 NULL,      NULL,  NULL,    NULL,    NULL,   NULL,
		 NULL
UNION ALL
SELECT
	 1000000,    9999999,     1,   25555, 2222222, 7777777,  100, 100000, 20.31, 16.2,
	'2012-12-12 12:12:12',
		  '1',     '234', '567', '23233',  '3323',  '111',
		   10;*/

/* merge test tables */
DROP TABLE IF EXISTS TestMerge1;
DROP TABLE IF EXISTS TestMerge2;
CREATE TABLE TestMerge1
(
	Id              INTEGER       NOT NULL CONSTRAINT PK_TestMerge1 PRIMARY KEY,
	Field1          INTEGER           NULL,
	Field2          INTEGER           NULL,
	Field3          INTEGER           NULL,
	Field4          INTEGER           NULL,
	Field5          INTEGER           NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BYTE(1)           NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      FLOAT(24)         NULL,
	FieldDouble     FLOAT(53)         NULL,
	FieldDateTime   DATE              NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       BYTE(16)          NULL,
	FieldDate       DATE              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
);
CREATE TABLE TestMerge2
(
	Id              INTEGER       NOT NULL CONSTRAINT PK_TestMerge2 PRIMARY KEY,
	Field1          INTEGER           NULL,
	Field2          INTEGER           NULL,
	Field3          INTEGER           NULL,
	Field4          INTEGER           NULL,
	Field5          INTEGER           NULL,

	FieldInt64      BIGINT            NULL,
	FieldBoolean    BYTE(1)           NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      FLOAT(24)         NULL,
	FieldDouble     FLOAT(53)         NULL,
	FieldDateTime   DATE              NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       BYTE(16)          NULL,
	FieldDate       DATE              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
);

DROP TABLE IF EXISTS TEST_T4_CASING;
CREATE TABLE TEST_T4_CASING
(
	ALL_CAPS              INT    NOT NULL,
	CAPS                  INT    NOT NULL,
	PascalCase            INT    NOT NULL,
	Pascal_Snake_Case     INT    NOT NULL,
	PascalCase_Snake_Case INT    NOT NULL,
	snake_case            INT    NOT NULL,
	camelCase             INT    NOT NULL
);

