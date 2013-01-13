
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
	DateTimeValue  datetime,
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

	bigintDataType      bigint                   NULL,
--	numericDataType     numeric                  NULL,
	smallintDataType    smallint                 NULL,
	intDataType         int                      NULL,
	tinyintDataType     tinyint                  NULL,
	mediumintDataType   mediumint                NULL,
	intDataType         int                      NULL,
--	moneyDataType       money                    NULL,
--	doubleDataType      double precision         NULL,
--	realDataType        real                     NULL,
--
--	timestampDataType   timestamp                NULL,
--	timestampTZDataType timestamp with time zone NULL,
--	dateDataType        date                     NULL,
--	timeDataType        time                     NULL,
--	timeTZDataType      time with time zone      NULL,
--	intervalDataType    interval                 NULL,
--
--	charDataType        char(1)                  NULL,
--	varcharDataType     varchar(20)              NULL,
--	textDataType        text                     NULL,
--
--	binaryDataType      bytea                    NULL,
--
--	uuidDataType        uuid                     NULL,
--	bitDataType         bit(3)                   NULL,
--	booleanDataType     boolean                  NULL,
--	colorDataType       color                    NULL,
--
--	pointDataType       point                    NULL,
--	lsegDataType        lseg                     NULL,
--	boxDataType         box                      NULL,
--	pathDataType        path                     NULL,
--	polygonDataType     polygon                  NULL,
--	circleDataType      circle                   NULL,
--
--	inetDataType        inet                     NULL,
--	macaddrDataType     macaddr                  NULL,

	xmlDataType         xml                      NULL
)
GO

INSERT INTO AllTypes
(
	bigintDataType,
--	numericDataType,
	smallintDataType,
	tinyintDataType,
	mediumintDataType,
	intDataType,
--	moneyDataType,
--	doubleDataType,
--	realDataType,
--
--	timestampDataType,
--	timestampTZDataType,
--	dateDataType,
--	timeDataType,
--	timeTZDataType,
--	intervalDataType,
--
--	charDataType,
--	varcharDataType,
--	textDataType,
--
--	binaryDataType,
--
--	uuidDataType,
--	bitDataType,
--	booleanDataType,
--	colorDataType,
--
--	pointDataType,
--	lsegDataType,
--	boxDataType,
--	pathDataType,
--	polygonDataType,
--	circleDataType,
--
--	inetDataType,
--	macaddrDataType,

	xmlDataType
)
SELECT
	NULL,
--	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
--	NULL,
--	NULL,
--	NULL,
--
--	NULL,
--	NULL,
--	NULL,
--	NULL,
--	NULL,
--	NULL,
--
--	NULL,
--	NULL,
--	NULL,
--
--	NULL,
--
--	NULL,
--	NULL,
--	NULL,
--	NULL,
--
--	NULL,
--	NULL,
--	NULL,
--	NULL,
--	NULL,
--	NULL,
--
--	NULL,
--	NULL,

	NULL
UNION ALL
SELECT
	1000000,
--	9999999,
	25555,
	111,
	5555,
	7777777,
--	100000,
--	20.31,
--	16.2,
--
--	Cast('2012-12-12 12:12:12' as timestamp),
--	Cast('2012-12-12 12:12:12-04' as timestamp with time zone),
--	Cast('2012-12-12 12:12:12' as date),
--	Cast('2012-12-12 12:12:12' as time),
--	Cast('12:12:12' as time with time zone),
--	Cast('1 3:05:20' as interval),
--
--	'1',
--	'234',
--	'567',
--
--	E'\\052'::bytea,
--
--	Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid),
--	B'101',
--	true,
--	'Green'::color,
--
--	'(1,2)'::point,
--	'((1,2),(3,4))'::lseg,
--	'((1,2),(3,4))'::box,
--	'((1,2),(3,4))'::path,
--	'((1,2),(3,4))'::polygon,
--	'((1,2),3)'::circle,
--
--	'192.168.1.1'::inet,
--	'01:02:03:04:05:06'::macaddr,

	'<root><element strattr="strvalue" intattr="12345"/></root>'

GO
