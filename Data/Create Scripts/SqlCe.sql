DROP TABLE Patient
GO
DROP TABLE Doctor
GO
DROP TABLE Person
GO

DROP TABLE InheritanceParent
GO

CREATE TABLE InheritanceParent
(
	InheritanceParentId int          NOT NULL CONSTRAINT PK_InheritanceParent PRIMARY KEY,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL
)
GO

DROP TABLE InheritanceChild
GO

CREATE TABLE InheritanceChild
(
	InheritanceChildId  int          NOT NULL CONSTRAINT PK_InheritanceChild PRIMARY KEY,
	InheritanceParentId int          NOT NULL,
	TypeDiscriminator   int              NULL,
	Name                nvarchar(50)     NULL
)
GO

-- Person Table

CREATE TABLE Person
(
	PersonID   int          NOT NULL IDENTITY(1,1) CONSTRAINT PK_Person PRIMARY KEY,
	FirstName  nvarchar(50) NOT NULL,
	LastName   nvarchar(50) NOT NULL,
	MiddleName nvarchar(50)     NULL,
	Gender     nchar(1)     NOT NULL
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
	PersonID int          NOT NULL
		CONSTRAINT PK_Doctor        PRIMARY KEY
		CONSTRAINT FK_Doctor_Person --FOREIGN KEY
			REFERENCES Person ([PersonID])
			ON UPDATE CASCADE
			ON DELETE CASCADE,
	Taxonomy nvarchar(50) NOT NULL
)
GO

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int           NOT NULL
		CONSTRAINT PK_Patient        PRIMARY KEY
		CONSTRAINT FK_Patient_Person --FOREIGN KEY
			REFERENCES Person ([PersonID])
			ON UPDATE CASCADE
			ON DELETE CASCADE,
	Diagnosis nvarchar(256) NOT NULL
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

CREATE TABLE TestIdentity (
	ID int NOT NULL IDENTITY(1,1) CONSTRAINT PK_TestIdentity PRIMARY KEY
)
GO


DROP TABLE AllTypes
GO

CREATE TABLE AllTypes
(
	ID                       int          NOT NULL IDENTITY(1,1) CONSTRAINT PK_AllTypes PRIMARY KEY,

	bigintDataType           bigint           NULL,
	numericDataType          numeric          NULL,
	bitDataType              bit              NULL,
	smallintDataType         smallint         NULL,
	decimalDataType          decimal          NULL,
	intDataType              int              NULL,
	tinyintDataType          tinyint          NULL,
	moneyDataType            money            NULL,
	floatDataType            float            NULL,
	realDataType             real             NULL,

	datetimeDataType         datetime         NULL,

	ncharDataType            nchar(20)        NULL,
	nvarcharDataType         nvarchar(20)     NULL,
	ntextDataType            ntext            NULL,

	binaryDataType           binary           NULL,
	varbinaryDataType        varbinary        NULL,
	imageDataType            image            NULL,

	timestampDataType        timestamp        NULL,
	uniqueidentifierDataType uniqueidentifier NULL
)
GO

INSERT INTO AllTypes
(
	bigintDataType, numericDataType, bitDataType, smallintDataType, decimalDataType,
	intDataType, tinyintDataType, moneyDataType, floatDataType, realDataType, 

	datetimeDataType,

	ncharDataType, nvarcharDataType, ntextDataType,

	binaryDataType, varbinaryDataType, imageDataType,

	uniqueidentifierDataType
)
SELECT
	     NULL,      NULL,  NULL,    NULL,    NULL,   NULL, NULL,   NULL,  NULL,  NULL,
	     NULL,
	     NULL,      NULL,  NULL,
	     NULL,      NULL,  NULL,
	     NULL
UNION ALL
SELECT
	  1000000,   9999999,     1,   25555, 2222222, 7777777,  100, 100000, 20.31, 16.2,
	Cast('2012-12-12 12:12:12' as datetime),
	  '23233',    '3323', '111',
	        1,         2, Cast(3 as varbinary),
	Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)

GO

-- merge test tables
DROP TABLE testmerge1
GO
DROP TABLE testmerge2
GO

CREATE TABLE testmerge1
(
	id		int          NOT NULL CONSTRAINT PK_testmerge1 PRIMARY KEY,
	field1	int              NULL,
	field2	int              NULL,
	field3	int              NULL,
	field4	int              NULL,
	field5	int              NULL
)
GO
CREATE TABLE testmerge2
(
	id		int          NOT NULL CONSTRAINT PK_testmerge2 PRIMARY KEY,
	field1	int              NULL,
	field2	int              NULL,
	field3	int              NULL,
	field4	int              NULL,
	field5	int              NULL
)
GO
