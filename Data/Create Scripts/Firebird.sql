DROP PROCEDURE Person_SelectByKey;            COMMIT;
DROP PROCEDURE Person_SelectAll;              COMMIT;
DROP PROCEDURE Person_SelectByName;           COMMIT;
DROP PROCEDURE Person_Insert;                 COMMIT;
DROP PROCEDURE Person_Insert_OutputParameter; COMMIT;
DROP PROCEDURE Person_Update;                 COMMIT;
DROP PROCEDURE Person_Delete;                 COMMIT;
DROP PROCEDURE Patient_SelectAll;             COMMIT;
DROP PROCEDURE Patient_SelectByName;          COMMIT;
DROP PROCEDURE OutRefTest;                    COMMIT;
DROP PROCEDURE OutRefEnumTest;                COMMIT;
DROP PROCEDURE Scalar_DataReader;             COMMIT;
DROP PROCEDURE Scalar_OutputParameter;        COMMIT;
DROP PROCEDURE Scalar_ReturnParameter;        COMMIT;

DROP TRIGGER CREATE_BinaryDataID;             COMMIT;
DROP TRIGGER CHANGE_BinaryData;               COMMIT;
DROP TRIGGER CREATE_PersonID;                 COMMIT;
DROP TRIGGER CREATE_DataTypeTest;             COMMIT;

DROP GENERATOR DataTypeID;                    COMMIT;
DROP GENERATOR PersonID;                      COMMIT;
DROP GENERATOR TimestampGen;                  COMMIT;

DROP TABLE Dual;                              COMMIT;
DROP TABLE DataTypeTest;                      COMMIT;
DROP TABLE Doctor;                            COMMIT;
DROP TABLE Patient;                           COMMIT;
DROP TABLE Person;                            COMMIT;

DROP EXTERNAL FUNCTION rtrim;                 COMMIT;
DROP EXTERNAL FUNCTION ltrim;                 COMMIT;


DECLARE EXTERNAL FUNCTION ltrim 
	CSTRING(255) NULL
	RETURNS CSTRING(255) FREE_IT
	ENTRY_POINT 'IB_UDF_ltrim' MODULE_NAME 'ib_udf';
COMMIT;

DECLARE EXTERNAL FUNCTION rtrim 
	CSTRING(255) NULL
	RETURNS CSTRING(255) FREE_IT
	ENTRY_POINT 'IB_UDF_rtrim' MODULE_NAME 'ib_udf';
COMMIT;


/*
Dual table FOR supporting queryies LIKE:
SELECT 1 AS id => SELECT 1 AS "id" *FROM Dual*
*/
CREATE TABLE Dual (Dummy  VARCHAR(10));
COMMIT;
INSERT INTO  Dual (Dummy) VALUES ('X');
COMMIT;

-- Person Table

CREATE TABLE Person
(
	PersonID   INTEGER     NOT NULL  PRIMARY KEY,
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS NOT NULL,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS NOT NULL,
	MiddleName VARCHAR(50),
	Gender     CHAR(1)     NOT NULL CHECK (Gender in ('M', 'F', 'U', 'O'))
); 
COMMIT;

CREATE GENERATOR PersonID;
COMMIT;

CREATE GENERATOR TimestampGen;
COMMIT;

CREATE TRIGGER CREATE_PersonID FOR Person
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.PersonID = GEN_ID(PersonID, 1);
END
COMMIT;

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M');
COMMIT;
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M');
COMMIT;

-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID INTEGER     NOT NULL,
	Taxonomy VARCHAR(50) NOT NULL,
		FOREIGN KEY (PersonID) REFERENCES Person (PersonID)
			ON DELETE CASCADE
)
COMMIT;

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');
COMMIT;

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int           NOT NULL,
	Diagnosis VARCHAR(256)  NOT NULL,
	FOREIGN KEY (PersonID) REFERENCES Person (PersonID)
			ON DELETE CASCADE
);
COMMIT;

INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution');
COMMIT;

-- Data Types test

/*
Data definitions according to:
http://www.firebirdsql.org/manual/migration-mssql-data-types.html

BUT! BLOB is ised for BINARY data! not CHAR
*/

CREATE TABLE DataTypeTest
(
	DataTypeID      INTEGER NOT NULL PRIMARY KEY,
	Binary_         BLOB,
	Boolean_        CHAR(1),
	Byte_           SMALLINT,
	Bytes_          BLOB,
	CHAR_           CHAR(1),
	DateTime_       TIMESTAMP,
	Decimal_        DECIMAL(10, 2),
	Double_         DOUBLE PRECISION,
	Guid_           CHAR(38),
	Int16_          SMALLINT,
	Int32_          INTEGER,
	Int64_          NUMERIC(11),
	Money_          DECIMAL(18, 4),
	SByte_          SMALLINT,
	Single_         FLOAT,
	Stream_         BLOB,
	String_         VARCHAR(50) CHARACTER SET UNICODE_FSS,
	UInt16_         SMALLINT,
	UInt32_         INTEGER,
	UInt64_         NUMERIC(11),
	Xml_            CHAR(1000)
)
COMMIT;

CREATE GENERATOR DataTypeID;
COMMIT;

CREATE TRIGGER CREATE_DataTypeTest FOR DataTypeTest
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.DataTypeID = GEN_ID(DataTypeID, 1); 
END
COMMIT;

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  CHAR_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,     Xml_)
VALUES
	(   NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL);
COMMIT;

INSERT INTO DataTypeTest
	(Binary_,	Boolean_,	Byte_,   Bytes_,  CHAR_,	DateTime_, Decimal_,
	 Double_,	Guid_,		Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,	Stream_,	String_, UInt16_, UInt32_,   UInt64_,
	 Xml_)
VALUES
	('dddddddddddddddd', 1,  255,'dddddddddddddddd', 'B', 'NOW', 12345.67,
	1234.567, 'dddddddddddddddddddddddddddddddd', 32767, 32768, 1000000, 12.3456, 127,
	1234.123, 'dddddddddddddddd', 'string', 32767, 32768, 200000000,
	'<root><element strattr="strvalue" intattr="12345"/></root>');
COMMIT;



DROP TABLE Parent     COMMIT;
DROP TABLE Child      COMMIT;
DROP TABLE GrandChild COMMIT;

CREATE TABLE Parent      (ParentID int, Value1 int)                    COMMIT;
CREATE TABLE Child       (ParentID int, ChildID int)                   COMMIT;
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int) COMMIT;


DROP TABLE LinqDataTypes COMMIT;

CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  timestamp,
	DateTimeValue2 timestamp,
	BoolValue      char(1),
	GuidValue      char(38),
	BinaryValue    blob,
	SmallIntValue  smallint,
	IntValue       int,
	BigIntValue    bigint
)
COMMIT;

DROP GENERATOR SequenceTestSeq COMMIT;

CREATE GENERATOR SequenceTestSeq
COMMIT;

DROP TABLE SequenceTest COMMIT;

CREATE TABLE SequenceTest
(
	ID     int         NOT NULL PRIMARY KEY,
	Value_ VARCHAR(50) NOT NULL
)
COMMIT;


DROP TRIGGER CREATE_ID
COMMIT;

DROP GENERATOR TestIdentityID
COMMIT;

DROP TABLE TestIdentity
COMMIT;

CREATE TABLE TestIdentity (
	ID INTEGER NOT NULL PRIMARY KEY
)
COMMIT;

CREATE GENERATOR TestIdentityID;
COMMIT;

CREATE TRIGGER CREATE_ID FOR TestIdentity
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.ID = GEN_ID(TestIdentityID, 1);
END
COMMIT;



DROP TRIGGER AllTypes_ID
COMMIT;

DROP GENERATOR AllTypesID
COMMIT;

DROP TABLE AllTypes
COMMIT;

CREATE TABLE AllTypes
(
	ID                       integer      NOT NULL PRIMARY KEY,

	bigintDataType           bigint,
	smallintDataType         smallint,
	decimalDataType          decimal(18),
	intDataType              int,
	floatDataType            float,
	realDataType             real,

	timestampDataType        timestamp,

	charDataType             char(1),
	varcharDataType          varchar(20),
	textDataType             blob sub_type TEXT,
	ncharDataType            char(20) character set UNICODE_FSS,
	nvarcharDataType         varchar(20) character set UNICODE_FSS,

	blobDataType             blob
)
COMMIT;

INSERT INTO AllTypes
VALUES
(
	1,

	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,

	NULL,

	NULL,
	NULL,
	NULL,
	NULL,
	NULL,

	NULL
)
COMMIT;

INSERT INTO AllTypes
VALUES
(
	2,

	1000000,
	25555,
	2222222,
	7777777,
	20.31,
	16,

	Cast('2012-12-12 12:12:12' as timestamp),

	'1',
	'234',
	'567',
	'23233',
	'3323',

	'12345'
)
COMMIT;


CREATE GENERATOR AllTypesID;
COMMIT;

CREATE TRIGGER AllTypes_ID FOR AllTypes
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.ID = GEN_ID(AllTypesID, 1);
END
COMMIT;


