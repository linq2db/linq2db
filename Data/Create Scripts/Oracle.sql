ALTER SYSTEM SET DEFERRED_SEGMENT_CREATION=FALSE
/
-- default is 300
ALTER SYSTEM SET OPEN_CURSORS=10000 SID='*' SCOPE=BOTH
/
-- Cleanup schema

BEGIN
	EXECUTE IMMEDIATE 'DROP SEQUENCE ' || '"PersonSeq"';
EXCEPTION
	WHEN OTHERS THEN
		IF SQLCODE != -2289 THEN
			RAISE;
		END IF;
END;
/
DROP TABLE "Doctor"
/
DROP TABLE "Patient"
/
DROP TABLE "Person"
/
DROP SEQUENCE BinaryDataSeq
/
DROP TABLE BinaryData
/
DROP SEQUENCE "DataTypeTestSeq"
/
DROP TABLE "DataTypeTest"
/
DROP TABLE "GrandChild"
/
DROP TABLE "Child"
/
DROP TABLE "Parent"
/
DROP TABLE "StringTest"
/
DROP TABLE "LinqDataTypes"
/
DROP TABLE LINQDATATYPESBC
/
DROP SEQUENCE SequenceTestSeq
/
DROP TABLE SequenceTest
/
DROP TABLE "STG_TRADE_INFORMATION"
/
DROP table "t_test_user_contract"
/
DROP table "t_test_user"
/
DROP sequence "sq_test_user_contract"
/
DROP table "t_entity"
/

--StringTest Table
CREATE TABLE "StringTest"
	( "StringValue1"                VARCHAR2(50) NULL
	, "StringValue2"                CHAR(50)     NULL
	, "KeyValue"                    VARCHAR2(50) NOT NULL
	)
/

INSERT INTO "StringTest" ("StringValue1", "StringValue2", "KeyValue") VALUES ('Value1', 'Value2', 'HasValues')
/
INSERT INTO "StringTest" ("StringValue1", "StringValue2", "KeyValue") VALUES (null,     null,     'NullValues')


-- Inheritance Parent/Child
/
DROP TABLE "InheritanceParent"
/

CREATE TABLE "InheritanceParent"
(
	"InheritanceParentId" NUMBER        NOT NULL PRIMARY KEY,
	"TypeDiscriminator"   NUMBER            NULL,
	"Name"                NVARCHAR2(50)     NULL
)
/
DROP TABLE "InheritanceChild"
/

CREATE TABLE "InheritanceChild"
(
	"InheritanceChildId"  NUMBER        NOT NULL PRIMARY KEY,
	"InheritanceParentId" NUMBER        NOT NULL,
	"TypeDiscriminator"   NUMBER            NULL,
	"Name"                NVARCHAR2(50)     NULL
)
/

-- Person Table

CREATE SEQUENCE "PersonSeq"
/

CREATE TABLE "Person"
	( "PersonID"                     NUMBER NOT NULL PRIMARY KEY
	, "FirstName"                    VARCHAR2(50) NOT NULL
	, "LastName"                     VARCHAR2(50) NOT NULL
	, "MiddleName"                   VARCHAR2(50)
	, "Gender"                       CHAR(1) NOT NULL

	, CONSTRAINT "Ck_Person_Gender"  CHECK ("Gender" IN ('M', 'F', 'U', 'O'))
	)
/

-- Insert Trigger for Person

CREATE OR REPLACE TRIGGER "Person_Add"
BEFORE INSERT
ON "Person"
FOR EACH ROW
BEGIN
SELECT
	"PersonSeq".NEXTVAL
INTO
	:NEW."PersonID"
FROM
	dual;
END;
/

-- Doctor Table Extension

CREATE TABLE "Doctor"
	( "PersonID"                       NUMBER NOT NULL PRIMARY KEY
	, "Taxonomy"                       NVARCHAR2(50) NOT NULL

	, CONSTRAINT "Fk_Doctor_Person" FOREIGN KEY ("PersonID")
		REFERENCES "Person" ("PersonID") ON DELETE CASCADE
	)
/

-- Patient Table Extension

CREATE TABLE "Patient"
	( "PersonID"                       NUMBER NOT NULL PRIMARY KEY
	, "Diagnosis"                      NVARCHAR2(256) NOT NULL

	, CONSTRAINT "Fk_Patient_Person" FOREIGN KEY ("PersonID")
		REFERENCES "Person" ("PersonID") ON DELETE CASCADE
	)
/

-- Sample data for Person/Doctor/Patient

INSERT INTO "Person"  ("FirstName", "LastName", "Gender") VALUES ('John',   'Pupkin',    'M')
/
INSERT INTO "Person"  ("FirstName", "LastName", "Gender") VALUES ('Tester', 'Testerson', 'M')
/
INSERT INTO "Person"  ("FirstName", "LastName", "Gender") VALUES ('Jane',   'Doe',       'F')
/
INSERT INTO "Person"  ("FirstName", "LastName", "MiddleName", "Gender") VALUES ('Jürgen', 'König', 'Ko', 'M')
/
INSERT INTO "Doctor"  ("PersonID",  "Taxonomy")  VALUES (1, 'Psychiatry')
/
INSERT INTO "Patient" ("PersonID",  "Diagnosis") VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
/

-- Person_Delete

CREATE OR REPLACE
PROCEDURE Person_Delete(pPersonID IN NUMBER) IS
BEGIN
DELETE FROM
	"Person"
WHERE
	"PersonID" = pPersonID;
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
INSERT INTO "Person"
	( "LastName",  "FirstName",  "MiddleName",  "Gender")
VALUES
	(pLastName, pFirstName, pMiddleName, pGender)
RETURNING
	"PersonID"
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
INSERT INTO "Person"
	( "LastName",  "FirstName",  "MiddleName",  "Gender")
VALUES
	(pLastName, pFirstName, pMiddleName, pGender)
RETURNING
	"PersonID"
INTO
	lPersonID;

OPEN retCursor FOR
	SELECT
		"PersonID", "FirstName", "LastName", "MiddleName", "Gender"
	FROM
		"Person"
	WHERE
		"PersonID" = lPersonID;
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
		"PersonID", "FirstName", "LastName", "MiddleName", "Gender"
	FROM
		"Person";
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
		"PersonID", "FirstName", "LastName", "MiddleName", "Gender"
	FROM
		"Person"
	WHERE
		"Gender" = pGender;
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
		"PersonID", "FirstName", "LastName", "MiddleName", "Gender"
	FROM
		"Person"
	WHERE
		"PersonID" = pID;
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
		"PersonID", "FirstName", "LastName", "MiddleName", "Gender"
	FROM
		"Person"
	WHERE
		"FirstName" = pFirstName AND "LastName" = pLastName;
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
		"PersonID", "FirstName", "LastName", "MiddleName", "Gender"
	FROM
		"Person"
	WHERE
		"FirstName" LIKE pFirstName AND "LastName" LIKE pLastName;
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
	"Person"
SET
	"LastName"   = pLastName,
	"FirstName"  = pFirstName,
	"MiddleName" = pMiddleName,
	"Gender"     = pGender
WHERE
	"PersonID"   = pPersonID;
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
	"Person".*, "Patient"."Diagnosis"
FROM
	"Patient", "Person"
WHERE
	"Patient"."PersonID" = "Person"."PersonID";
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
	"Person".*, "Patient"."Diagnosis"
FROM
	"Patient", "Person"
WHERE
	"Patient"."PersonID" = "Person"."PersonID"
	AND "FirstName" = pFirstName AND "LastName" = pLastName;
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

CREATE SEQUENCE "DataTypeTestSeq"
/

CREATE TABLE "DataTypeTest"
(
	"DataTypeID"      INTEGER      NOT NULL PRIMARY KEY,
	"Binary_"         RAW(50)          NULL,
	"Boolean_"        NUMBER(1,0)      NULL,
	"Byte_"           NUMBER(3,0)      NULL,
	"Bytes_"          BLOB             NULL,
	"Char_"           NCHAR            NULL,
	"DateTime_"       DATE             NULL,
	"Decimal_"        NUMBER(19,5)     NULL,
	"Double_"         DOUBLE PRECISION NULL,
	"Guid_"           RAW(16)          NULL,
	"Int16_"          NUMBER(5,0)      NULL,
	"Int32_"          NUMBER(10,0)     NULL,
	"Int64_"          NUMBER(20,0)     NULL,
	"Money_"          NUMBER           NULL,
	"SByte_"          NUMBER(3,0)      NULL,
	"Single_"         FLOAT            NULL,
	"Stream_"         BLOB             NULL,
	"String_"         NVARCHAR2(50)    NULL,
	"UInt16_"         NUMBER(5,0)      NULL,
	"UInt32_"         NUMBER(10,0)     NULL,
	"UInt64_"         NUMBER(20,0)     NULL,
	"Xml_"            XMLTYPE          NULL
)
/

-- Insert Trigger for DataTypeTest

CREATE OR REPLACE TRIGGER "DataTypeTest_Add"
BEFORE INSERT
ON "DataTypeTest"
FOR EACH ROW
BEGIN
SELECT
	"DataTypeTestSeq".NEXTVAL
INTO
	:NEW."DataTypeID"
FROM
	dual;
END;
/

INSERT INTO "DataTypeTest"
	("Binary_",      "Boolean_",    "Byte_",     "Bytes_",   "Char_", "DateTime_", "Decimal_",
	 "Double_",         "Guid_",   "Int16_",     "Int32_",  "Int64_",    "Money_",   "SByte_",
	 "Single_",       "Stream_",  "String_",    "UInt16_", "UInt32_",   "UInt64_",     "Xml_")
VALUES
	(   NULL,          NULL,     NULL,       NULL,    NULL,      NULL,     NULL,
		NULL,          NULL,     NULL,       NULL,    NULL,      NULL,     NULL,
		NULL,          NULL,     NULL,       NULL,    NULL,      NULL,     NULL)
/

INSERT INTO "DataTypeTest"
	("Binary_",      "Boolean_",    "Byte_",     "Bytes_",   "Char_", "DateTime_", "Decimal_",
	 "Double_",         "Guid_",   "Int16_",     "Int32_",  "Int64_",    "Money_",   "SByte_",
	 "Single_",       "Stream_",  "String_",    "UInt16_", "UInt32_",   "UInt64_",     "Xml_")
VALUES
	(SYS_GUID(),          1,      255, SYS_GUID(),     'B',   SYSDATE, 12345.67,
	   1234.567, SYS_GUID(),    32767,      32768, 1000000,   12.3456,      127,
	   1234.123, SYS_GUID(), 'string',      32767,   32768, 200000000,
	XMLTYPE('<root><element strattr="strvalue" intattr="12345"/></root>'))
/



CREATE TABLE "Parent"      ("ParentID" int, "Value1" int)
/
CREATE TABLE "Child"       ("ParentID" int, "ChildID" int)
/
CREATE TABLE "GrandChild"  ("ParentID" int, "ChildID" int, "GrandChildID" int)
/


CREATE TABLE "LinqDataTypes"
(
	ID               int,
	"MoneyValue"     decimal(10,4),
	"DateTimeValue"  timestamp,
	"DateTimeValue2" timestamp,
	"BoolValue"      smallint,
	"GuidValue"      raw(16),
	"BinaryValue"    blob         NULL,
	"SmallIntValue"  smallint,
	"IntValue"       int          NULL,
	"BigIntValue"    number(20,0) NULL,
	"StringValue"    VARCHAR2(50) NULL
)
/

-- uppercased table for native bulk copy
CREATE TABLE LinqDataTypesBC
(
	ID               int,
	MoneyValue     decimal(10,4),
	DateTimeValue  timestamp,
	DateTimeValue2 timestamp,
	BoolValue      smallint,
	GuidValue      raw(16),
	SmallIntValue  smallint,
	IntValue       int          NULL,
	BigIntValue    number(20,0) NULL,
	StringValue    VARCHAR2(50) NULL
)
/

CREATE SEQUENCE SequenceTestSeq
	MINVALUE 1
	START WITH 1
	INCREMENT BY 1
	CACHE 10
/

-- names not escaped to test native bulk copy
CREATE TABLE SequenceTest
(
	ID      int NOT NULL PRIMARY KEY,
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


create table "t_test_user"
(
	"user_id"  number primary key,
	"name"     varchar2(255) not null unique
)
/

create table "t_test_user_contract"
(
	"user_contract_id" number primary key,
	"user_id"          number not null references "t_test_user" on delete cascade,
	"contract_no"      number not null,
	"name"             varchar2(255) not null,
	unique           ("user_id", "contract_no")
)
/
create sequence "sq_test_user_contract"
/

DROP SEQUENCE "TestIdentitySeq"
/
DROP TABLE "TestIdentity"
/

CREATE TABLE "TestIdentity" (
	"ID" NUMBER NOT NULL PRIMARY KEY
)
/

CREATE SEQUENCE "TestIdentitySeq"
/

CREATE OR REPLACE TRIGGER "TestIdentity_Add"
BEFORE INSERT
ON "TestIdentity"
FOR EACH ROW
BEGIN
SELECT
	"TestIdentitySeq".NEXTVAL
INTO
	:NEW.ID
FROM
	dual;
END;
/


DROP TABLE "AllTypes"
/

CREATE TABLE "AllTypes"
(
	"ID"                         int                        NOT NULL PRIMARY KEY,

	"bigintDataType"           number(20,0)                   NULL,
	"numericDataType"          numeric                        NULL,
	"bitDataType"              number(1,0)                    NULL,
	"smallintDataType"         number(5,0)                    NULL,
	"decimalDataType"          number(*,6)                    NULL,
	"smallmoneyDataType"       number(10,4)                   NULL,
	"intDataType"              number(10,0)                   NULL,
	"tinyintDataType"          number(3,0)                    NULL,
	"moneyDataType"            number                         NULL,
	"floatDataType"            binary_double                  NULL,
	"realDataType"             binary_float                   NULL,

	"datetimeDataType"         date                           NULL,
	"datetime2DataType"        timestamp                      NULL,
	"datetimeoffsetDataType"   timestamp with time zone       NULL,
	"localZoneDataType"        timestamp with local time zone NULL,

	"charDataType"             char(1)                        NULL,
	"char20DataType"           char(20)                       NULL,
	"varcharDataType"          varchar2(20)                   NULL,
	"textDataType"             clob                           NULL,
	"ncharDataType"            nchar(20)                      NULL,
	"nvarcharDataType"         nvarchar2(20)                  NULL,
	"ntextDataType"            nclob                          NULL,

	"binaryDataType"           blob                           NULL,
	"bfileDataType"            bfile                          NULL,
	"guidDataType"             raw(16)                        NULL,
	"longDataType"             long                           NULL,

	"xmlDataType"              XmlType                        NULL
)
/

DROP SEQUENCE "AllTypesSeq"
/
CREATE SEQUENCE "AllTypesSeq"
/

CREATE OR REPLACE TRIGGER "AllTypes_Add"
BEFORE INSERT
ON "AllTypes"
FOR EACH ROW
BEGIN
	SELECT "AllTypesSeq".NEXTVAL INTO :NEW.ID FROM dual;
END;
/

CREATE OR REPLACE DIRECTORY DATA_DIR AS 'C:\DataFiles'
/

INSERT INTO "AllTypes"
(
	"bigintDataType",
	"numericDataType",
	"bitDataType",
	"smallintDataType",
	"decimalDataType",
	"smallmoneyDataType",
	"intDataType",
	"tinyintDataType",
	"moneyDataType",
	"floatDataType",
	"realDataType",

	"datetimeDataType",
	"datetime2DataType",
	"datetimeoffsetDataType",
	"localZoneDataType",

	"charDataType",
	"varcharDataType",
	"textDataType",
	"ncharDataType",
	"nvarcharDataType",
	"ntextDataType",

	"binaryDataType",
	"bfileDataType",
	"guidDataType",
	"longDataType",

	"xmlDataType"
)
SELECT
	NULL bigintDataType,
	NULL numericDataType,
	NULL bitDataType,
	NULL smallintDataType,
	NULL decimalDataType,
	NULL smallmoneyDataType,
	NULL intDataType,
	NULL tinyintDataType,
	NULL moneyDataType,
	NULL floatDataType,
	NULL realDataType,

	NULL datetimeDataType,
	NULL datetime2DataType,
	NULL datetimeoffsetDataType,
	NULL localZoneDataType,

	NULL charDataType,
	NULL varcharDataType,
	NULL textDataType,
	NULL ncharDataType,
	NULL nvarcharDataType,
	NULL ntextDataType,

	NULL binaryDataType,
	NULL bfileDataType,
	NULL guidDataType,
	NULL longDataType,

	NULL xmlDataType
FROM dual
UNION ALL
SELECT
	1000000,
	9999999,
	1,
	25555,
	2222222,
	100000,
	7777777,
	100,
	100000,
	20.31,
	16.2,

	to_date  ('2012-12-12 12:12:12', 'YYYY-MM-DD HH:MI:SS'),
	timestamp '2012-12-12 12:12:12.012',
	timestamp '2012-12-12 12:12:12.012 -5:00',
	timestamp '2012-12-12 12:12:12.012',

	'1',
	'234',
	'567',
	'23233',
	'3323',
	'111',

	to_blob('00AA'),
	bfilename('DATA_DIR', 'bfile.txt'),
	sys_guid(),
	'LONG',

	XMLTYPE('<root><element strattr="strvalue" intattr="12345"/></root>')
FROM dual
/

create table "t_entity"
(
	"entity_id" integer primary key,
	"time"      date,
	"duration"  interval day(3) to second(2)
)
/

DROP TABLE "LongRawTable"
/

CREATE TABLE "LongRawTable"
(
	"ID"                NUMBER        NOT NULL PRIMARY KEY,
	"longRawDataType" long raw      NULL
)
/

INSERT INTO "LongRawTable"
SELECT 1, NULL                        FROM dual UNION ALL
SELECT 2, to_blob('4c4f4e4720524157') FROM dual -- "LONG RAW"
/

DROP TABLE "DecimalOverflow"
/

CREATE TABLE "DecimalOverflow"
(
	"Decimal1" numeric(38,20),
	"Decimal2" numeric(31,2),
	"Decimal3" numeric(38,36),
	"Decimal4" numeric(29,0),
	"Decimal5" numeric(38,38)
)
/

INSERT INTO "DecimalOverflow"
SELECT  123456789012345.12345678901234567890,  1234567890123456789.91,  12.345678901234512345678901234567890,  1234567890123456789,  .12345678901234512345678901234567890 FROM dual UNION ALL
SELECT -123456789012345.12345678901234567890, -1234567890123456789.91, -12.345678901234512345678901234567890, -1234567890123456789, -.12345678901234512345678901234567890 FROM dual UNION ALL
SELECT  12345678901234.567890123456789,                          NULL,                                  NULL,                 NULL,                                  NULL FROM dual UNION ALL
SELECT -12345678901234.567890123456789,                          NULL,                                  NULL,                 NULL,                                  NULL FROM dual UNION ALL
SELECT  12345678901234.56789012345678,                           NULL,                                  NULL,                 NULL,                                  NULL FROM dual UNION ALL
SELECT -12345678901234.56789012345678,                           NULL,                                  NULL,                 NULL,                                  NULL FROM dual UNION ALL
SELECT  12345678901234.5678901234567,                            NULL,                                  NULL,                 NULL,                                  NULL FROM dual UNION ALL
SELECT -12345678901234.5678901234567,                            NULL,                                  NULL,                 NULL,                                  NULL FROM dual


-- merge test tables
/
DROP TABLE "TestMerge1"
/
DROP TABLE "TestMerge2"
/

CREATE TABLE "TestMerge1"
(
	"Id"		NUMBER	NOT NULL PRIMARY KEY,
	"Field1"	NUMBER	NULL,
	"Field2"	NUMBER	NULL,
	"Field3"	NUMBER	NULL,
	"Field4"	NUMBER	NULL,
	"Field5"	NUMBER	NULL,

	"FieldInt64"      NUMBER(20, 0)               NULL,
	"FieldBoolean"    NUMBER(1, 0)                NULL,
	"FieldString"     VARCHAR(20)                 NULL,
	"FieldNString"    NVARCHAR2(20)               NULL,
	"FieldChar"       CHAR(1)                     NULL,
	"FieldNChar"      NCHAR(1)                    NULL,
	"FieldFloat"      BINARY_FLOAT                NULL,
	"FieldDouble"     BINARY_DOUBLE               NULL,
	"FieldDateTime"   DATE                        NULL,
	"FieldDateTime2"  TIMESTAMP(7) WITH TIME ZONE NULL,
	"FieldBinary"     BLOB                        NULL,
	"FieldGuid"       RAW(16)                     NULL,
	"FieldDecimal"    DECIMAL(24, 10)             NULL,
	"FieldEnumString" VARCHAR(20)                 NULL,
	"FieldEnumNumber" NUMBER                      NULL
)
/
CREATE TABLE "TestMerge2"
(
	"Id"		NUMBER	NOT NULL PRIMARY KEY,
	"Field1"	NUMBER	NULL,
	"Field2"	NUMBER	NULL,
	"Field3"	NUMBER	NULL,
	"Field4"	NUMBER	NULL,
	"Field5"	NUMBER	NULL,

	"FieldInt64"      NUMBER(20, 0)               NULL,
	"FieldBoolean"    NUMBER(1, 0)                NULL,
	"FieldString"     VARCHAR(20)                 NULL,
	"FieldNString"    NVARCHAR2(20)               NULL,
	"FieldChar"       CHAR(1)                     NULL,
	"FieldNChar"      NCHAR(1)                    NULL,
	"FieldFloat"      BINARY_FLOAT                NULL,
	"FieldDouble"     BINARY_DOUBLE               NULL,
	"FieldDateTime"   DATE                        NULL,
	"FieldDateTime2"  TIMESTAMP(7) WITH TIME ZONE NULL,
	"FieldBinary"     BLOB                        NULL,
	"FieldGuid"       RAW(16)                     NULL,
	"FieldDecimal"    DECIMAL(24, 10)             NULL,
	"FieldEnumString" VARCHAR(20)                 NULL,
	"FieldEnumNumber" NUMBER                      NULL
)
/

CREATE OR REPLACE
PROCEDURE AddIssue792Record IS
BEGIN
	INSERT INTO "AllTypes"("char20DataType") VALUES('issue792');
END;
/


CREATE OR REPLACE
PROCEDURE AllOutputParameters
(
	ID                         IN OUT int                          ,

	bigintDataType           IN OUT number                         ,
	numericDataType          IN OUT number                         ,
	bitDataType              IN OUT number                         ,
	smallintDataType         IN OUT number                         ,
	decimalDataType          IN OUT number                         ,
	smallmoneyDataType       IN OUT number                         ,
	intDataType              IN OUT number                         ,
	tinyintDataType          IN OUT number                         ,
	moneyDataType            IN OUT number                         ,
	floatDataType            IN OUT binary_double                  ,
	realDataType             IN OUT binary_float                   ,

	datetimeDataType         IN OUT date                           ,
	datetime2DataType        IN OUT timestamp                      ,
	datetimeoffsetDataType   IN OUT timestamp with time zone       ,
	localZoneDataType        IN OUT timestamp with local time zone ,

	charDataType             IN OUT char                           ,
	char20DataType           IN OUT char                           ,
	varcharDataType          IN OUT varchar2                       ,
	textDataType             IN OUT clob                           ,
	ncharDataType            IN OUT nchar                          ,
	nvarcharDataType         IN OUT nvarchar2                      ,
	ntextDataType            IN OUT nclob                          ,

	binaryDataType           IN OUT blob                           ,
	bfileDataType            IN OUT bfile                          ,
	guidDataType             IN OUT raw                            ,

	xmlDataType              IN OUT XmlType

) IS
BEGIN
	SELECT
		at.ID,

		at."bigintDataType",
		at."numericDataType",
		at."bitDataType",
		at."smallintDataType",
		at."decimalDataType",
		at."smallmoneyDataType",
		at."intDataType",
		at."tinyintDataType",
		at."moneyDataType",
		at."floatDataType",
		at."realDataType",

		at."datetimeDataType",
		at."datetime2DataType",
		at."datetimeoffsetDataType",
		at."localZoneDataType",

		at."charDataType",
		at."char20DataType",
		at."varcharDataType",
		at."textDataType",
		at."ncharDataType",
		at."nvarcharDataType",
		at."ntextDataType",

		at."binaryDataType",
 		at."bfileDataType",
		at."guidDataType",

		at."xmlDataType"

		INTO

		ID,

		bigintDataType,
		numericDataType,
		bitDataType,
		smallintDataType,
		decimalDataType,
		smallmoneyDataType,
		intDataType,
		tinyintDataType,
		moneyDataType,
		floatDataType,
		realDataType,

		datetimeDataType,
		datetime2DataType,
		datetimeoffsetDataType,
		localZoneDataType,

		charDataType,
		char20DataType,
		varcharDataType,
		textDataType,
		ncharDataType,
		nvarcharDataType,
		ntextDataType,

		binaryDataType,
		bfileDataType,
		guidDataType,

		xmlDataType

	FROM "AllTypes" at
	WHERE at.ID = 2;
END;
/

CREATE OR REPLACE PACKAGE ISSUE2132 AS
procedure test;
END;
/

CREATE OR REPLACE PACKAGE BODY ISSUE2132 AS
procedure test is
	begin
		return 4;
	end test;
END;
/

CREATE OR REPLACE PROCEDURE TEST2132
BEGIN
	return 6;
END;
-- schema test table, view and matview. doesn't have columns, as we test only object and it's atributes load
/
DROP VIEW "SchemaTestView"
/
DROP MATERIALIZED VIEW "SchemaTestMatView"
/
DROP TABLE "SchemaTestTable"
/
CREATE TABLE "SchemaTestTable"
(
	"Id"  NUMBER NOT NULL PRIMARY KEY
)
/
CREATE VIEW "SchemaTestView" AS SELECT "Id" FROM "SchemaTestTable"
/
CREATE MATERIALIZED VIEW "SchemaTestMatView" AS SELECT "Id" FROM "SchemaTestTable"
/
COMMENT ON TABLE "SchemaTestTable" IS 'This is table'
/
COMMENT ON MATERIALIZED VIEW "SchemaTestMatView" IS 'This is matview'
/
COMMENT ON COLUMN "SchemaTestTable"."Id" IS 'This is column'
/
COMMENT ON COLUMN "SchemaTestView"."Id" IS 'This is view column'
/
COMMENT ON COLUMN "SchemaTestMatView"."Id" IS 'This is matview column'
/

DROP TABLE "CollatedTable"
/
CREATE TABLE "CollatedTable"
(
	"Id"				INT NOT NULL,
	"CaseSensitive"		VARCHAR2(20) NOT NULL,
	"CaseInsensitive"	VARCHAR2(20) NOT NULL
)
/

DROP TYPE tf_table
/

CREATE OR REPLACE TYPE tf_type IS OBJECT (O INT)
/

CREATE OR REPLACE TYPE tf_table IS TABLE OF tf_type
/

CREATE OR REPLACE PACKAGE TEST_PACKAGE1 AS
	FUNCTION TEST_FUNCTION(i INT) RETURN INT;
	FUNCTION TEST_TABLE_FUNCTION(i INT) RETURN tf_table PIPELINED;
	PROCEDURE TEST_PROCEDURE(i INT, o OUT INT);
END TEST_PACKAGE1;
/

CREATE OR REPLACE PACKAGE BODY TEST_PACKAGE1 AS
	FUNCTION TEST_FUNCTION(i INT)
		RETURN INT IS
		BEGIN 
			RETURN i + 1;
		END;
	FUNCTION TEST_TABLE_FUNCTION(i IN INT)
		RETURN tf_table PIPELINED IS
		BEGIN
			FOR r IN (SELECT * FROM "Person") LOOP
				PIPE ROW(tf_type(i + 1));
			END LOOP;
			RETURN;
		END;
	PROCEDURE TEST_PROCEDURE(i INT, o OUT INT) AS
		BEGIN 
			o := i + 1;
		END;
END TEST_PACKAGE1;
/

CREATE OR REPLACE PACKAGE TEST_PACKAGE2 AS 
	FUNCTION TEST_FUNCTION(i INT) RETURN INT;
	FUNCTION TEST_TABLE_FUNCTION(i INT) RETURN tf_table PIPELINED;
	PROCEDURE TEST_PROCEDURE(i INT, o OUT INT);
END TEST_PACKAGE2;
/

CREATE OR REPLACE PACKAGE BODY TEST_PACKAGE2 AS
	FUNCTION TEST_FUNCTION(i INT)
		RETURN INT IS
		BEGIN 
			RETURN i + 2;
		END;
	FUNCTION TEST_TABLE_FUNCTION(i IN INT)
		RETURN tf_table PIPELINED IS
		BEGIN
			FOR r IN (SELECT * FROM "Person") LOOP
				PIPE ROW(tf_type(i + 2));
			END LOOP;
			RETURN;
		END;
	PROCEDURE TEST_PROCEDURE(i INT, o OUT INT) AS
		BEGIN 
			o := i + 2;
		END;
END TEST_PACKAGE2;
/

CREATE OR REPLACE FUNCTION TEST_FUNCTION(i INT)
RETURN INT IS
BEGIN 
	RETURN i + 3;
END;
/

CREATE OR REPLACE FUNCTION TEST_TABLE_FUNCTION(i IN INT)
RETURN tf_table PIPELINED IS
BEGIN
  FOR r IN (SELECT * FROM "Person") LOOP
    PIPE ROW(tf_type(i + 3));
  END LOOP;
  RETURN;
END;
/

CREATE OR REPLACE PROCEDURE TEST_PROCEDURE(i INT, o OUT int) AS
BEGIN 
	o := i + 3;
END;
/
