USE master
GO

DROP DATABASE TestData
GO

CREATE DATABASE TestData
	ON master = '102400K'
GO

USE TestData
GO

CREATE TABLE InheritanceParent
(
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL,

	CONSTRAINT PK_InheritanceParent PRIMARY KEY CLUSTERED (InheritanceParentId)
)
GO

CREATE TABLE InheritanceChild
(
	InheritanceChildId  int          NOT NULL,
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL,

	CONSTRAINT PK_InheritanceChild PRIMARY KEY CLUSTERED (InheritanceChildId)
)
GO

-- Person Table

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
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jane',   'Doe',       'F')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jürgen', 'König',     'M')
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


CREATE TABLE Parent      (ParentID int, Value1 int NULL)
GO
CREATE TABLE Child       (ParentID int, ChildID int)
GO
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int)
GO

CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4) NULL,
	DateTimeValue  datetime      NULL,
	DateTimeValue2 datetime      NULL,
	BoolValue      bit           default(0),
	GuidValue      char(36)      NULL,
	BinaryValue    binary(500)   NULL,
	SmallIntValue  smallint      NULL,
	IntValue       int           NULL,
	BigIntValue    bigint        NULL,
	StringValue    nvarchar(50)  NULL
)
GO


CREATE TABLE TestIdentity
(
	ID int IDENTITY CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED
)
GO

-- AllTypes

CREATE TABLE AllTypes
(
	ID                       int           IDENTITY,

	bigintDataType           bigint            NULL,
	uBigintDataType          unsigned  bigint  NULL,
	numericDataType          numeric           NULL,
	bitDataType              bit           NOT NULL,
	smallintDataType         smallint          NULL,
	uSmallintDataType        unsigned smallint NULL,
	decimalDataType          decimal           NULL,
	smallmoneyDataType       smallmoney        NULL,
	intDataType              int               NULL,
	uIntDataType             unsigned int      NULL,
	tinyintDataType          tinyint           NULL,
	moneyDataType            money             NULL,
	floatDataType            float             NULL,
	realDataType             real              NULL,

	datetimeDataType         datetime          NULL,
	smalldatetimeDataType    smalldatetime     NULL,
	dateDataType             date              NULL,
	timeDataType             time              NULL,

	charDataType             char(1)           NULL,
	char20DataType           char(20)          NULL,
	varcharDataType          varchar(20)       NULL,
	textDataType             text              NULL,
	ncharDataType            nchar(20)         NULL,
	nvarcharDataType         nvarchar(20)      NULL,
	ntextDataType            unitext           NULL,

	binaryDataType           binary            NULL,
	varbinaryDataType        varbinary         NULL,
	imageDataType            image             NULL,

	timestampDataType        timestamp         NULL
)
GO

INSERT INTO AllTypes
(
	bigintDataType, numericDataType, bitDataType, smallintDataType, decimalDataType, smallmoneyDataType,
	intDataType, tinyintDataType, moneyDataType, floatDataType, realDataType, 
	uBigintDataType, uSmallintDataType, uIntDataType, 

	datetimeDataType, smalldatetimeDataType, dateDataType, timeDataType,

	charDataType, varcharDataType, textDataType, ncharDataType, nvarcharDataType, ntextDataType,

	binaryDataType, varbinaryDataType, imageDataType
)
SELECT
	     NULL,      NULL,       0,    NULL,    NULL,   NULL,    NULL, NULL,   NULL,  NULL, NULL,
	     NULL,      NULL,    NULL,
	     NULL,      NULL,    NULL,    NULL,
	     NULL,      NULL,    NULL,    NULL,    NULL,   NULL,
	     NULL,      NULL,    NULL
UNION ALL
SELECT
	 1000000,    9999999,       1,   25555, 2222222, 100000, 7777777,  100, 100000, 20.31, 16.2,
	 2233332,      33333, 3333333,
	Cast('2012-12-12 12:12:12' as datetime),
	           Cast('2012-12-12 12:12:12' as smalldatetime),
						  Cast('2012-12-12' as date),
	                               Cast('12:12:12.010' as time),
	      '1',     '234',   '567', '23233',  '3323',  '111',
	        1,         2, Cast(3 as varbinary)

GO

-- merge test tables
CREATE TABLE TestMerge1
(
	Id     int NOT NULL,
	Field1 int NULL,
	Field2 int NULL,
	Field3 int NULL,
	Field4 int NULL,
	Field5 int NULL,

	FieldInt64      BIGINT            NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      REAL              NULL,
	FieldDouble     FLOAT             NULL,
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
	Id     int NOT NULL,
	Field1 int NULL,
	Field2 int NULL,
	Field3 int NULL,
	Field4 int NULL,
	Field5 int NULL,

	FieldInt64      BIGINT            NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      REAL              NULL,
	FieldDouble     FLOAT             NULL,
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
CREATE TABLE TestMergeIdentity
(
	Id     int IDENTITY,
	Field  int NULL,

	CONSTRAINT PK_TestMergeIdentity PRIMARY KEY CLUSTERED (Id)
)
GO

CREATE OR REPLACE PROCEDURE AddIssue792Record AS
	INSERT INTO dbo.AllTypes(char20DataType, bitDataType) VALUES('issue792', 1)
	SELECT * FROM dbo.AllTypes
RETURN
