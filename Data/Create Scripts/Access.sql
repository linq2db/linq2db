DROP TABLE Doctor
GO
DROP TABLE Patient
GO
DROP TABLE Person
GO

CREATE TABLE Person (
	PersonID                Int IDENTITY,
	FirstName               Text(50) NOT NULL,
	LastName                Text(50) NOT NULL,
	MiddleName              Text(50),
	Gender                  Text(1) NOT NULL,

	CONSTRAINT PK_Peson PRIMARY KEY (PersonID)
)
GO

CREATE TABLE Doctor (
	PersonID                Int NOT NULL,
	Taxonomy                Text(50) NOT NULL,

	CONSTRAINT OK_Doctor PRIMARY KEY (PersonID)
)
GO

CREATE TABLE Patient (
	PersonID                Int NOT NULL,
	Diagnosis               Text(255) NOT NULL,

	CONSTRAINT PK_Patient PRIMARY KEY (PersonID)
)
GO

ALTER TABLE Doctor
	ADD CONSTRAINT PersonDoctor FOREIGN KEY (PersonID) REFERENCES Person ON UPDATE CASCADE ON DELETE CASCADE;
GO

ALTER TABLE Patient
	ADD CONSTRAINT PersonPatient FOREIGN KEY (PersonID) REFERENCES Person ON UPDATE CASCADE ON DELETE CASCADE;
GO

INSERT INTO Person (FirstName, LastName, Gender) VALUES ("John",   "Pupkin",    "M")
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ("Tester", "Testerson", "M")
GO
INSERT INTO Doctor (PersonID, Taxonomy)   VALUES (1, "Psychiatry")
GO
INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, "Hallucination with Paranoid Bugs' Delirium of Persecution")
GO


DROP TABLE Parent
GO
DROP TABLE Child
GO
DROP TABLE GrandChild
GO

CREATE TABLE Parent     (ParentID int, Value1 int NULL)
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
	DateTimeValue2 datetime,
	BoolValue      bit,
	GuidValue      uniqueidentifier,
	BinaryValue    OleObject NULL,
	SmallIntValue  smallint,
	IntValue       int       NULL,
	BigIntValue    long      NULL
)
GO


DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity (
	ID Int IDENTITY,
	CONSTRAINT PK_TestIdentity PRIMARY KEY (ID)
)
GO


DROP TABLE AllTypes
GO

CREATE TABLE AllTypes
(
	ID                       counter      NOT NULL,

	bitDataType              bit              NULL,
	smallintDataType         smallint         NULL,
	decimalDataType          decimal          NULL,
	intDataType              int              NULL,
	tinyintDataType          tinyint          NULL,
	moneyDataType            money            NULL,
	floatDataType            float            NULL,
	realDataType             real             NULL,

	datetimeDataType         datetime         NULL,

	charDataType             char(1)          NULL,
	varcharDataType          varchar(20)      NULL,
	textDataType             text             NULL,
	ncharDataType            nchar(20)        NULL,
	nvarcharDataType         nvarchar(20)     NULL,
	ntextDataType            ntext            NULL,

	binaryDataType           binary(10)       NULL,
	varbinaryDataType        varbinary        NULL,
	imageDataType            image            NULL,
	oleObjectDataType        oleobject        NULL,

	uniqueidentifierDataType uniqueidentifier NULL
)
GO

INSERT INTO AllTypes (binaryDataType)
VALUES (NULL)

GO
