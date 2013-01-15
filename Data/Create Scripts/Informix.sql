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

-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID int          NOT NULL,
	Taxonomy nvarchar(50) NOT NULL
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
	ID   SERIAL       NOT NULL,

	PRIMARY KEY(ID)
)
GO
