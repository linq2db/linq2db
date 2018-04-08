﻿DROP Procedure AddIssue792Record
GO
DROP Procedure Person_SelectByKey
GO
DROP Procedure Person_SelectAll
GO
DROP Procedure Person_SelectByName
GO
DROP Procedure Person_SelectListByName
GO
DROP Procedure Person_Insert
GO
DROP Procedure Person_Update
GO
DROP Procedure Person_Delete
GO
DROP Procedure Patient_SelectAll
GO
DROP Procedure Patient_SelectByName
GO
DROP Procedure Scalar_DataReader
GO

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
	InheritanceParentId Int      NOT NULL CONSTRAINT PK_InheritanceParent PRIMARY KEY,
	TypeDiscriminator   Int          NULL,
	Name                Text(50)     NULL
)
GO

DROP TABLE InheritanceChild
GO

CREATE TABLE InheritanceChild
(
	InheritanceChildId  Int      NOT NULL CONSTRAINT PK_InheritanceChild PRIMARY KEY,
	InheritanceParentId Int      NOT NULL,
	TypeDiscriminator   Int          NULL,
	Name                Text(50)     NULL
)
GO

CREATE TABLE Person
(
	PersonID   Int IDENTITY,
	FirstName  Text(50) NOT NULL,
	LastName   Text(50) NOT NULL,
	MiddleName Text(50),
	Gender     Text(1) NOT NULL,

	CONSTRAINT PK_Peson PRIMARY KEY (PersonID)
)
GO

CREATE TABLE Doctor
(
	PersonID Int NOT NULL,
	Taxonomy Text(50) NOT NULL,

	CONSTRAINT PK_Doctor PRIMARY KEY (PersonID)
)
GO

CREATE TABLE Patient
(
	PersonID  Int NOT NULL,
	Diagnosis Text(255) NOT NULL,

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
INSERT INTO Person (FirstName, LastName, Gender) VALUES ("Jane",   "Doe",       "F")
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ("Jürgen", "König",     "M")
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


CREATE Procedure Person_SelectByKey(
	[@id] Long)
AS
	SELECT * FROM Person WHERE PersonID = [@id];
GO

CREATE Procedure Person_SelectAll
AS
	SELECT * FROM Person;
GO

CREATE Procedure Person_SelectByName(
	[@firstName] Text(50),
	[@lastName]  Text(50))
AS
SELECT
	*
FROM
	Person
WHERE
	FirstName = [@firstName] AND LastName = [@lastName];
GO

CREATE Procedure Person_SelectListByName(
	[@firstName] Text(50),
	[@lastName]  Text(50))
AS
SELECT
	*
FROM
	Person
WHERE
	FirstName like [@firstName] AND LastName like [@lastName];
GO

CREATE Procedure Person_Insert(
	[@FirstName]  Text(50),
	[@MiddleName] Text(50),
	[@LastName]   Text(50),
	[@Gender]     Text(1))
AS
INSERT INTO Person
	(FirstName, MiddleName, LastName, Gender)
VALUES
	([@FirstName], [@MiddleName], [@LastName], [@Gender]);
GO

CREATE Procedure Person_Update(
	[@id]         Long,
	[@PersonID]   Long,
	[@FirstName]  Text(50),
	[@MiddleName] Text(50),
	[@LastName]   Text(50),
	[@Gender]     Text(1))
AS
UPDATE
	Person
SET
	LastName   = [@LastName],
	FirstName  = [@FirstName],
	MiddleName = [@MiddleName],
	Gender     = [@Gender]
WHERE
	PersonID = [@id];
GO

CREATE Procedure Person_Delete(
	[@PersonID] Long)
AS
DELETE FROM Person WHERE PersonID = [@PersonID];
GO

CREATE Procedure Patient_SelectAll
AS
SELECT
	Person.*, Patient.Diagnosis
FROM
	Patient, Person
WHERE
	Patient.PersonID = Person.PersonID;
GO

CREATE Procedure Patient_SelectByName(
	[@firstName] Text(50),
	[@lastName]  Text(50))
AS
SELECT
	Person.*, Patient.Diagnosis
FROM
	Patient, Person
WHERE
	Patient.PersonID = Person.PersonID
	AND FirstName = [@firstName] AND LastName = [@lastName];
GO

CREATE Procedure Scalar_DataReader
AS
	SELECT 12345 AS intField, "54321" AS stringField;
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
	BigIntValue    long      NULL,
	StringValue    Text(50)  NULL
)
GO


DROP TABLE TestIdentity
GO

CREATE TABLE TestIdentity
(
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
	char20DataType           char(20)         NULL,
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

DROP TABLE TestMerge1
GO
DROP TABLE TestMerge2
GO

CREATE TABLE TestMerge1
(
	Id       Int      NOT NULL CONSTRAINT PK_TestMerge1 PRIMARY KEY,
	Field1   Int          NULL,
	Field2   Int          NULL,
	Field3   Int          NULL,
	Field4   Int          NULL,
	Field5   Int          NULL,

	FieldBoolean    BIT               NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      REAL              NULL,
	FieldDouble     FLOAT             NULL,
	FieldDateTime   DATETIME          NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       UNIQUEIDENTIFIER  NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
	FieldDate       DATE              NULL,
	FieldTime       TIME              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
)
GO
CREATE TABLE TestMerge2
(
	Id       Int      NOT NULL CONSTRAINT PK_TestMerge2 PRIMARY KEY,
	Field1   Int          NULL,
	Field2   Int          NULL,
	Field3   Int          NULL,
	Field4   Int          NULL,
	Field5   Int          NULL,

	FieldBoolean    BIT               NULL,
	FieldString     VARCHAR(20)       NULL,
	FieldNString    NVARCHAR(20)      NULL,
	FieldChar       CHAR(1)           NULL,
	FieldNChar      NCHAR(1)          NULL,
	FieldFloat      REAL              NULL,
	FieldDouble     FLOAT             NULL,
	FieldDateTime   DATETIME          NULL,
	FieldBinary     VARBINARY(20)     NULL,
	FieldGuid       UNIQUEIDENTIFIER  NULL,
	FieldDecimal    DECIMAL(24, 10)   NULL,
	FieldDate       DATE              NULL,
	FieldTime       TIME              NULL,
	FieldEnumString VARCHAR(20)       NULL,
	FieldEnumNumber INT               NULL
)
GO
CREATE Procedure AddIssue792Record
AS
	INSERT INTO AllTypes(char20DataType) VALUES('issue792')
GO
