--
-- Helper table
--
DROP TABLE IF EXISTS Dual;
CREATE TABLE Dual (Dummy  VARCHAR(10));
INSERT INTO  Dual (Dummy) VALUES ('X');

--
-- Person Table
--
DROP TABLE IF EXISTS Person;
CREATE TABLE Person
(
	PersonID   integer      NOT NULL CONSTRAINT PK_Person PRIMARY KEY AUTOINCREMENT,
	FirstName  nvarchar(50) NOT NULL,
	LastName   nvarchar(50) NOT NULL,
	MiddleName nvarchar(50)     NULL,
	Gender     char(1)      NOT NULL CONSTRAINT CK_Person_Gender CHECK (Gender in ('M', 'F', 'U', 'O'))
);

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M');
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M');

--
-- Doctor Table Extension
--
DROP TABLE IF EXISTS Doctor;
CREATE TABLE Doctor
(
	PersonID integer      NOT NULL CONSTRAINT PK_Doctor        PRIMARY KEY,
	Taxonomy nvarchar(50) NOT NULL
);

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');

--
-- Patient Table Extension
--
DROP TABLE IF EXISTS Patient;
CREATE TABLE Patient
(
	PersonID  integer       NOT NULL CONSTRAINT PK_Patient        PRIMARY KEY,
	Diagnosis nvarchar(256) NOT NULL
);
INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution');

--
-- BinaryData Table
--
DROP TABLE IF EXISTS BinaryData;
CREATE TABLE BinaryData
(
	BinaryDataID integer    NOT NULL CONSTRAINT PK_BinaryData PRIMARY KEY AUTOINCREMENT,
	Stamp        timestamp  NOT NULL,
	Data         blob(1024) NOT NULL
);

--
-- Babylon test
--
DROP TABLE IF EXISTS DataTypeTest;
CREATE TABLE DataTypeTest
(
	DataTypeID      integer      NOT NULL CONSTRAINT PK_DataType PRIMARY KEY AUTOINCREMENT,
	Binary_         binary(50)       NULL,
	Boolean_        bit              NULL,
	Byte_           tinyint          NULL,
	Bytes_          varbinary(50)    NULL,
	Char_           char(1)          NULL,
	DateTime_       datetime         NULL,
	Decimal_        decimal(20,2)    NULL,
	Double_         float            NULL,
	Guid_           uniqueidentifier NULL,
	Int16_          smallint         NULL,
	Int32_          int              NULL,
	Int64_          bigint           NULL,
	Money_          money            NULL,
	SByte_          tinyint          NULL,
	Single_         real             NULL,
	Stream_         varbinary(50)    NULL,
	String_         nvarchar(50)     NULL,
	UInt16_         smallint         NULL,
	UInt32_         int              NULL,
	UInt64_         bigint           NULL,
	Xml_            text             NULL
);

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  Char_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,     Xml_)
VALUES
	(   NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL);

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  Char_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,
	 Xml_)
VALUES
	(randomblob(16),        1,     255, zeroblob(16),     'B', DATETIME('NOW'), 12345.67,
	1234.567, '{64e145a3-0077-4335-b2c6-ea19c9f464f8}',   32767,   32768, 1000000,   12.3456,      127,
	1234.123,  randomblob(64), 'string',  32767,   32768, 200000000,
	'<root><element strattr="strvalue" intattr="12345"/></root>');


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
	DateTimeValue  datetime,
	DateTimeValue2 datetime2,
	BoolValue      boolean,
	GuidValue      uniqueidentifier,
	BinaryValue    binary(5000) NULL,
	SmallIntValue  smallint,
	IntValue       int          NULL,
	BigIntValue    bigint       NULL
);
