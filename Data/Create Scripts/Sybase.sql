IF OBJECT_ID('dbo.Doctor') IS NOT NULL
BEGIN DROP TABLE Doctor END
GO

IF OBJECT_ID('dbo.Patient') IS NOT NULL
BEGIN DROP TABLE Patient END
GO

IF OBJECT_ID('dbo.InheritanceParent') IS NOT NULL
BEGIN DROP TABLE InheritanceParent END
GO

IF OBJECT_ID('dbo.InheritanceChild') IS NOT NULL
BEGIN DROP TABLE InheritanceChild END
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
	BoolValue      bit           default(0),
	GuidValue      char(36)      NULL,
	BinaryValue    binary(500)   NULL,
	SmallIntValue  smallint      NULL,
	IntValue       int           NULL,
	BigIntValue    bigint        NULL
)
GO


DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity
(
	ID int IDENTITY CONSTRAINT PK_TestIdentity PRIMARY KEY CLUSTERED
)
GO

-- AllTypes

IF OBJECT_ID('dbo.AllTypes') IS NOT NULL
BEGIN DROP TABLE AllTypes END
GO

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
IF OBJECT_ID('dbo.testmerge1') IS NOT NULL
BEGIN DROP TABLE testmerge1 END
GO
IF OBJECT_ID('dbo.testmerge2') IS NOT NULL
BEGIN DROP TABLE testmerge2 END
GO

CREATE TABLE testmerge1
(
	id		int NOT NULL,
	field1	int NULL,
	field2	int NULL,
	field3	int NULL,
	field4	int NULL,
	field5	int NULL,
	CONSTRAINT PK_testmerge1 PRIMARY KEY CLUSTERED (id)
)
GO

CREATE TABLE testmerge2
(
	id		int NOT NULL,
	field1	int NULL,
	field2	int NULL,
	field3	int NULL,
	field4	int NULL,
	field5	int NULL,
	CONSTRAINT PK_testmerge2 PRIMARY KEY CLUSTERED (id)
)
