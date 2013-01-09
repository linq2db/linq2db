-- Cleanup schema

DROP SEQUENCE PersonSeq
/
DROP TABLE Doctor
/
DROP TABLE Patient
/
DROP TABLE Person
/
DROP SEQUENCE DataTypeTestSeq
/
DROP TABLE DataTypeTest
/
DROP TABLE GrandChild
/
DROP TABLE Child
/
DROP TABLE Parent
/
DROP TABLE LinqDataTypes
/
DROP SEQUENCE SequenceTestSeq
/
DROP TABLE SequenceTest
/
DROP TABLE "STG_TRADE_INFORMATION"
/
DROP table t_test_user_contract
/
DROP table t_test_user
/
DROP sequence sq_test_user
/
DROP sequence sq_test_user_contract
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
	BinaryValue    blob         NULL,
	SmallIntValue  smallint,
	IntValue       int          NULL,
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


DROP SEQUENCE TestIdentitySeq
/
DROP TABLE TestIdentity
/

CREATE TABLE TestIdentity (
	ID NUMBER NOT NULL PRIMARY KEY
)
/

CREATE SEQUENCE TestIdentitySeq
/

CREATE OR REPLACE TRIGGER TestIdentity_Add
BEFORE INSERT
ON TestIdentity
FOR EACH ROW
BEGIN
SELECT
	TestIdentitySeq.NEXTVAL
INTO
	:NEW.ID
FROM
	dual;
END;
/


DROP TABLE AllTypes
/

CREATE TABLE AllTypes
(
	ID                       int                        NOT NULL PRIMARY KEY,

	bigintDataType           number(20,0)                   NULL,
	numericDataType          numeric                        NULL,
	bitDataType              number(1,0)                    NULL,
	smallintDataType         number(5,0)                    NULL,
	decimalDataType          number(*,6)                    NULL,
	smallmoneyDataType       number(10,4)                   NULL,
	intDataType              number(10,0)                   NULL,
	tinyintDataType          number(3,0)                    NULL,
	moneyDataType            number                         NULL,
	floatDataType            binary_double                  NULL,
	realDataType             binary_float                   NULL,

	datetimeDataType         date                           NULL,
	datetime2DataType        timestamp                      NULL,
	datetimeoffsetDataType   timestamp with time zone       NULL,
	localZoneDataType        timestamp with local time zone NULL,

	charDataType             char(1)                        NULL,
	varcharDataType          varchar2(20)                   NULL,
	textDataType             clob                           NULL,
	ncharDataType            nchar(20)                      NULL,
	nvarcharDataType         nvarchar2(20)                  NULL,
	ntextDataType            nclob                          NULL,

	binaryDataType           blob                           NULL,
	bfileDataType            bfile                          NULL,

	uriDataType              UriType                        NULL,
	xmlDataType              XmlType                        NULL
) 
/

DROP SEQUENCE AllTypesSeq
/
CREATE SEQUENCE AllTypesSeq
/

CREATE OR REPLACE TRIGGER AllTypes_Add
BEFORE INSERT
ON AllTypes
FOR EACH ROW
BEGIN
	SELECT AllTypesSeq.NEXTVAL INTO :NEW.ID FROM dual;
END;
/

CREATE OR REPLACE DIRECTORY DATA_DIR AS 'C:\DataFiles'
/

INSERT INTO AllTypes
(
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
	varcharDataType,
	textDataType,
	ncharDataType,
	nvarcharDataType,
	ntextDataType,

	binaryDataType,
	bfileDataType,

	uriDataType,
	xmlDataType
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

	NULL uriDataType,
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

	to_date     ('2012-12-12 12:12:12', 'YYYY-MM-DD HH:MI:SS'),
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

	SYS.URIFACTORY.GETURI('http://www.linq2db.com'),
	XMLTYPE('<root><element strattr="strvalue" intattr="12345"/></root>')
FROM dual
/
