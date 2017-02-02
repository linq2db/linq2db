DROP TABLE Doctor
GO

DROP TABLE Patient
GO

DROP TABLE Person
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
-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID int          NOT NULL,
	Taxonomy nvarchar(50) NOT NULL,
	FOREIGN KEY (PersonID)
	REFERENCES Person (PersonID)
)
GO

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int           NOT NULL,
	Diagnosis nvarchar(100) NOT NULL
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
CREATE TABLE Child       (ParentID int, ChildID int, TypeDiscriminator int NULL)
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
	BigIntValue    bigint
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
