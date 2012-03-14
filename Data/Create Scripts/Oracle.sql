-- Cleanup schema

DECLARE
	REFCURSOR SYS_REFCURSOR;
	SEQUENCE_NAME VARCHAR2(30);
	TABLE_NAME VARCHAR2(30);
	CONSTRAINT_NAME VARCHAR2(30);
BEGIN
OPEN REFCURSOR FOR
	SELECT
		SEQUENCE_NAME
	FROM
		USER_SEQUENCES;
LOOP
	FETCH
		REFCURSOR
	INTO
		SEQUENCE_NAME;
	EXIT
		WHEN REFCURSOR%NOTFOUND;
	EXECUTE IMMEDIATE
		'DROP SEQUENCE ' || SEQUENCE_NAME;
END LOOP;

OPEN REFCURSOR FOR
	SELECT
		CONSTRAINT_NAME, TABLE_NAME
	FROM
		USER_CONSTRAINTS
	WHERE
		CONSTRAINT_TYPE = 'R';
LOOP
	FETCH
		REFCURSOR
	INTO
		CONSTRAINT_NAME, TABLE_NAME;
	EXIT
		WHEN REFCURSOR%NOTFOUND;
	EXECUTE IMMEDIATE
		'ALTER TABLE ' || TABLE_NAME || ' DROP CONSTRAINT ' || CONSTRAINT_NAME;
END LOOP;

OPEN REFCURSOR FOR
	SELECT
		TABLE_NAME
	FROM
		USER_TABLES;
LOOP
	FETCH
		REFCURSOR
	INTO
		TABLE_NAME;
	EXIT
		WHEN REFCURSOR%NOTFOUND;
	EXECUTE IMMEDIATE
		'DROP TABLE ' || TABLE_NAME;
END LOOP;
END;
/

-- Person Table

CREATE SEQUENCE PersonSeq
/

CREATE TABLE Person
	( PersonID                     NUMBER NOT NULL PRIMARY KEY
	, Firstname                    VARCHAR2(50) NOT NULL
	, Lastname                     VARCHAR2(50) NOT NULL
	, Middlename                   VARCHAR2(50)
	, Gender                       CHAR(1) NOT NULL
	
	, CONSTRAINT Ck_Person_Gender  CHECK (Gender IN ('M', 'F', 'U', 'O'))
	)
/

-- Insert Trigger for Person

CREATE OR REPLACE TRIGGER Person_Add
BEFORE INSERT
ON Person
FOR EACH ROW
BEGIN
SELECT
	PersonSeq.NEXTVAL
INTO
	:NEW.PersonID
FROM
	dual;
END;
/

-- Doctor Table Extension

CREATE TABLE Doctor
	( PersonID                       NUMBER NOT NULL PRIMARY KEY
	, Taxonomy                       NVARCHAR2(50) NOT NULL
	
	, CONSTRAINT Fk_Doctor_Person FOREIGN KEY (PersonID)
		REFERENCES Person (PersonID) ON DELETE CASCADE
	)
/

-- Patient Table Extension

CREATE TABLE Patient
	( PersonID                       NUMBER NOT NULL PRIMARY KEY
	, Diagnosis                      NVARCHAR2(256) NOT NULL
	
	, CONSTRAINT Fk_Patient_Person FOREIGN KEY (PersonID)
		REFERENCES Person (PersonID) ON DELETE CASCADE
	)
/

-- Sample data for Person/Doctor/Patient

INSERT INTO Person  (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M')
/
INSERT INTO Person  (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M')
/
INSERT INTO Doctor  (PersonID,  Taxonomy)  VALUES (PersonSeq.CURRVAL, 'Psychiatry')
/
INSERT INTO Patient (PersonID,  Diagnosis) VALUES (PersonSeq.CURRVAL, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
/

-- Person_Delete

CREATE OR REPLACE 
PROCEDURE Person_Delete(pPersonID IN NUMBER) IS
BEGIN
DELETE FROM
	Person
WHERE
	PersonID = pPersonID;
END;
/

-- Person_Insert

CREATE OR REPLACE 
PROCEDURE Person_Insert_OutputParameter
	( pFirstName  IN NVARCHAR2
	, pLastName   IN NVARCHAR2
	, pMiddleName IN NVARCHAR2
	, pGender     IN CHAR
	, pPersonID   OUT NUMBER
	) IS
BEGIN
INSERT INTO Person
	( LastName,  FirstName,  MiddleName,  Gender)
VALUES
	(pLastName, pFirstName, pMiddleName, pGender)
RETURNING
	PersonID
INTO
	pPersonID;
END;
/

CREATE OR REPLACE 
FUNCTION Person_Insert
	( pFirstName  IN NVARCHAR2
	, pLastName   IN NVARCHAR2
	, pMiddleName IN NVARCHAR2
	, pGender     IN CHAR
	)
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
	lPersonID NUMBER;
BEGIN
INSERT INTO Person
	( LastName,  FirstName,  MiddleName,  Gender)
VALUES
	(pLastName, pFirstName, pMiddleName, pGender)
RETURNING
	PersonID
INTO
	lPersonID;

OPEN retCursor FOR
	SELECT
		PersonID, Firstname, Lastname, Middlename, Gender     
	FROM
		Person
	WHERE
		PersonID = lPersonID;
RETURN
	retCursor;
END;
/

-- Person_SelectAll

CREATE OR REPLACE 
FUNCTION Person_SelectAll
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
	SELECT
		PersonID, Firstname, Lastname, Middlename, Gender     
	FROM
		Person;
RETURN
	retCursor;
END;
/

-- Person_SelectAllByGender

CREATE OR REPLACE 
FUNCTION Person_SelectAllByGender(pGender IN CHAR)
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
	SELECT
		PersonID, Firstname, Lastname, Middlename, Gender     
	FROM
		Person
	WHERE
		Gender = pGender;
RETURN
	retCursor;
END;
/

-- Person_SelectByKey

CREATE OR REPLACE 
FUNCTION Person_SelectByKey(pID IN NUMBER)
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
	SELECT
		PersonID, Firstname, Lastname, Middlename, Gender     
	FROM
		Person
	WHERE
		PersonID = pID;
RETURN
	retCursor;
END;
/

-- Person_SelectByName

CREATE OR REPLACE 
FUNCTION Person_SelectByName
	( pFirstName IN NVARCHAR2
	, pLastName  IN NVARCHAR2
	)
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
	SELECT
		PersonID, Firstname, Lastname, Middlename, Gender     
	FROM
		Person
	WHERE
		FirstName = pFirstName AND LastName = pLastName;
RETURN
	retCursor;
END;
/

-- Person_SelectListByName

CREATE OR REPLACE 
FUNCTION Person_SelectListByName
	( pFirstName IN NVARCHAR2
	, pLastName  IN NVARCHAR2
	)
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
	SELECT
		PersonID, Firstname, Lastname, Middlename, Gender     
	FROM
		Person
	WHERE
		FirstName LIKE pFirstName AND LastName LIKE pLastName;
RETURN
	retCursor;
END;
/

CREATE OR REPLACE 
PROCEDURE Person_Update
	( pPersonID   IN NUMBER
	, pFirstName  IN NVARCHAR2
	, pLastName   IN NVARCHAR2
	, pMiddleName IN NVARCHAR2
	, pGender     IN CHAR
	) IS
BEGIN
UPDATE
	Person
SET
	LastName   = pLastName,
	FirstName  = pFirstName,
	MiddleName = pMiddleName,
	Gender     = pGender
WHERE
	PersonID   = pPersonID;
END;
/

-- Patient_SelectAll

CREATE OR REPLACE 
FUNCTION Patient_SelectAll
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
SELECT
	Person.*, Patient.Diagnosis
FROM
	Patient, Person
WHERE
	Patient.PersonID = Person.PersonID;
RETURN
	retCursor;
END;
/


-- Patient_SelectByName

CREATE OR REPLACE 
FUNCTION Patient_SelectByName
	( pFirstName IN NVARCHAR2
	, pLastName  IN NVARCHAR2
	)
RETURN SYS_REFCURSOR IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
SELECT
	Person.*, Patient.Diagnosis
FROM
	Patient, Person
WHERE
	Patient.PersonID = Person.PersonID
	AND FirstName = pFirstName AND LastName = pLastName;
RETURN
	retCursor;
END;
/

-- BinaryData Table

CREATE SEQUENCE BinaryDataSeq
/

CREATE TABLE BinaryData
	( BinaryDataID                 NUMBER NOT NULL PRIMARY KEY
	, Stamp                        TIMESTAMP DEFAULT SYSDATE NOT NULL
	, Data                         BLOB NOT NULL
	)
/

-- Insert Trigger for Binarydata

CREATE OR REPLACE TRIGGER BinaryData_Add
BEFORE INSERT
ON BinaryData
FOR EACH ROW
BEGIN
SELECT
	BinaryDataSeq.NEXTVAL
INTO
	:NEW.BinaryDataID
FROM
	dual;
END;
/

-- OutRefTest

CREATE OR REPLACE 
PROCEDURE OutRefTest
	( pID             IN     NUMBER
	, pOutputID       OUT    NUMBER
	, pInputOutputID  IN OUT NUMBER
	, pStr            IN     NVARCHAR2
	, pOutputStr      OUT    NVARCHAR2
	, pInputOutputStr IN OUT NVARCHAR2
	) IS
BEGIN
	pOutputID       := pID;
	pInputOutputID  := pID + pInputOutputID;
	pOutputStr      := pStr;
	pInputOutputStr := pStr || pInputOutputStr;
END;
/

CREATE OR REPLACE 
PROCEDURE OutRefEnumTest
	( pStr            IN     NVARCHAR2
	, pOutputStr      OUT    NVARCHAR2
	, pInputOutputStr IN OUT NVARCHAR2
	) IS
BEGIN
	pOutputStr      := pStr;
	pInputOutputStr := pStr || pInputOutputStr;
END;
/

-- ArrayTest

CREATE OR REPLACE 
PROCEDURE ArrayTest
	( pIntArray            IN     DBMS_UTILITY.NUMBER_ARRAY
	, pOutputIntArray      OUT    DBMS_UTILITY.NUMBER_ARRAY
	, pInputOutputIntArray IN OUT DBMS_UTILITY.NUMBER_ARRAY
	, pStrArray            IN     DBMS_UTILITY.NAME_ARRAY
	, pOutputStrArray      OUT    DBMS_UTILITY.NAME_ARRAY
	, pInputOutputStrArray IN OUT DBMS_UTILITY.NAME_ARRAY
	) IS
BEGIN
pOutputIntArray := pIntArray;

FOR i IN pIntArray.FIRST..pIntArray.LAST LOOP
	pInputOutputIntArray(i) := pInputOutputIntArray(i) + pIntArray(i);
END LOOP;

pOutputStrArray := pStrArray;

FOR i IN pStrArray.FIRST..pStrArray.LAST LOOP
	pInputOutputStrArray(i) := pInputOutputStrArray(i) || pStrArray(i);
END LOOP;
END;
/

CREATE OR REPLACE 
PROCEDURE ScalarArray
	( pOutputIntArray      OUT    DBMS_UTILITY.NUMBER_ARRAY
	) IS
BEGIN
FOR i IN 1..5 LOOP
	pOutputIntArray(i) := i;
END LOOP;
END;
/

-- ResultSetTest

CREATE OR REPLACE 
PROCEDURE RESULTSETTEST
	( mr OUT SYS_REFCURSOR
	, sr OUT SYS_REFCURSOR
	) IS
BEGIN
OPEN mr FOR
	SELECT       1 as MasterID FROM dual
	UNION SELECT 2 as MasterID FROM dual;
OPEN sr FOR
	SELECT       4 SlaveID, 1 as MasterID FROM dual
	UNION SELECT 5 SlaveID, 2 as MasterID FROM dual
	UNION SELECT 6 SlaveID, 2 as MasterID FROM dual
	UNION SELECT 7 SlaveID, 1 as MasterID FROM dual;
END;
/

-- ExecuteScalarTest

CREATE OR REPLACE 
FUNCTION Scalar_DataReader
RETURN SYS_REFCURSOR
IS
	retCursor SYS_REFCURSOR;
BEGIN
OPEN retCursor FOR
	SELECT
		12345 intField, '54321' stringField 
	FROM
		DUAL;
RETURN
	retCursor;
END;
/

CREATE OR REPLACE 
PROCEDURE Scalar_OutputParameter
	( pOutputInt    OUT BINARY_INTEGER
	, pOutputString OUT NVARCHAR2
	) IS
BEGIN
	pOutputInt := 12345;
	pOutputString := '54321';
END;
/

CREATE OR REPLACE 
FUNCTION Scalar_ReturnParameter
RETURN BINARY_INTEGER IS
BEGIN
RETURN
	12345;
END;
/

-- Data Types test

CREATE SEQUENCE DataTypeTestSeq
/

CREATE TABLE DataTypeTest
(
	DataTypeID      INTEGER      NOT NULL PRIMARY KEY,
	Binary_         RAW(50)          NULL,
	Boolean_        NUMBER(1,0)      NULL,
	Byte_           NUMBER(3,0)      NULL,
	Bytes_          BLOB             NULL,
	Char_           NCHAR            NULL,
	DateTime_       DATE             NULL,
	Decimal_        NUMBER(19,5)     NULL,
	Double_         DOUBLE PRECISION NULL,
	Guid_           RAW(16)          NULL,
	Int16_          NUMBER(5,0)      NULL,
	Int32_          NUMBER(10,0)     NULL,
	Int64_          NUMBER(20,0)     NULL,
	Money_          NUMBER           NULL,
	SByte_          NUMBER(3,0)      NULL,
	Single_         FLOAT            NULL,
	Stream_         BLOB             NULL,
	String_         NVARCHAR2(50)    NULL,
	UInt16_         NUMBER(5,0)      NULL,
	UInt32_         NUMBER(10,0)     NULL,
	UInt64_         NUMBER(20,0)     NULL,
	Xml_            XMLTYPE          NULL
)
/

-- Insert Trigger for DataTypeTest

CREATE OR REPLACE TRIGGER DataTypeTest_Add
BEFORE INSERT
ON DataTypeTest
FOR EACH ROW
BEGIN
SELECT
	DataTypeTestSeq.NEXTVAL
INTO
	:NEW.DataTypeID
FROM
	dual;
END;
/

INSERT INTO DataTypeTest
	(Binary_,      Boolean_,    Byte_,     Bytes_,   Char_, DateTime_, Decimal_,
	 Double_,         Guid_,   Int16_,     Int32_,  Int64_,    Money_,   SByte_,
	 Single_,       Stream_,  String_,    UInt16_, UInt32_,   UInt64_,     Xml_)
VALUES
	(   NULL,          NULL,     NULL,       NULL,    NULL,      NULL,     NULL,
	    NULL,          NULL,     NULL,       NULL,    NULL,      NULL,     NULL,
	    NULL,          NULL,     NULL,       NULL,    NULL,      NULL,     NULL)
/

INSERT INTO DataTypeTest
	(Binary_,      Boolean_,    Byte_,     Bytes_,   Char_, DateTime_, Decimal_,
	 Double_,         Guid_,   Int16_,     Int32_,  Int64_,    Money_,   SByte_,
	 Single_,       Stream_,  String_,    UInt16_, UInt32_,   UInt64_,
	 Xml_)
VALUES
	(SYS_GUID(),          1,      255, SYS_GUID(),     'B',   SYSDATE, 12345.67,
	   1234.567, SYS_GUID(),    32767,      32768, 1000000,   12.3456,      127,
	   1234.123, SYS_GUID(), 'string',      32767,   32768, 200000000,
	XMLTYPE('<root><element strattr="strvalue" intattr="12345"/></root>'))
/



CREATE TABLE Parent      (ParentID int, Value1 int)
/
CREATE TABLE Child       (ParentID int, ChildID int)
/
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int)
/


CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  timestamp,
	DateTimeValue2 timestamp,
	BoolValue      smallint,
	GuidValue      raw(16),
	BinaryValue    blob,
	SmallIntValue  smallint,
	IntValue       int NULL,
	BigIntValue    number(20,0) NULL
)
/

CREATE SEQUENCE SequenceTestSeq
	MINVALUE 1
	START WITH 1
	INCREMENT BY 1
	CACHE 10
/

CREATE TABLE SequenceTest
(
	ID                 int NOT NULL PRIMARY KEY,
	Value VARCHAR2(50) NOT NULL
)
/

CREATE TABLE "STG_TRADE_INFORMATION"
(
	"STG_TRADE_ID"          NUMBER NOT NULL ENABLE,
	"STG_TRADE_VERSION"     NUMBER NOT NULL ENABLE,
	"INFORMATION_TYPE_ID"   NUMBER NOT NULL ENABLE,
	"INFORMATION_TYPE_NAME" VARCHAR2(50 BYTE),
	"VALUE"                 VARCHAR2(4000 BYTE),
	"VALUE_AS_INTEGER"      NUMBER,
	"VALUE_AS_DATE"         DATE
)
/


create table t_test_user
(
	user_id  number primary key,
	name     varchar2(255) not null unique
)
/

create table t_test_user_contract
(
	user_contract_id number primary key,
	user_id          number not null references t_test_user on delete cascade,
	contract_no      number not null,
	name             varchar2(255) not null,
	unique           (user_id, contract_no)
)
/

create sequence sq_test_user
/
create sequence sq_test_user_contract
/
