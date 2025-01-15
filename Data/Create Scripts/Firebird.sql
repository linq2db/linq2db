-- SKIP Firebird.2.5 BEGIN
DROP PACKAGE TEST_PACKAGE1;                     COMMIT;
DROP PACKAGE TEST_PACKAGE2;                     COMMIT;
DROP PROCEDURE TEST_PROCEDURE;                  COMMIT;
DROP PROCEDURE TEST_TABLE_FUNCTION;             COMMIT;
DROP FUNCTION TEST_FUNCTION;
-- SKIP Firebird.3 BEGIN
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
SELECT 1 FROM rdb$database
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END
-- SKIP Firebird.3 END
COMMIT;
-- SKIP Firebird.2.5 END

DROP PROCEDURE "AddIssue792Record";             COMMIT;
DROP PROCEDURE "Person_SelectByKey";            COMMIT;
DROP PROCEDURE "Person_SelectAll";              COMMIT;
DROP PROCEDURE "Person_SelectByName";           COMMIT;
DROP PROCEDURE "Person_Insert";                 COMMIT;
DROP PROCEDURE "Person_Insert_OutputParameter"; COMMIT;
DROP PROCEDURE "Person_Update";                 COMMIT;
DROP PROCEDURE "Person_Delete";                 COMMIT;
DROP PROCEDURE "Patient_SelectAll";             COMMIT;
DROP PROCEDURE "Patient_SelectByName";          COMMIT;
DROP PROCEDURE "OutRefTest";                    COMMIT;
DROP PROCEDURE "OutRefEnumTest";                COMMIT;
DROP PROCEDURE "Scalar_DataReader";             COMMIT;
DROP PROCEDURE "Scalar_OutputParameter";        COMMIT;
DROP PROCEDURE "Scalar_ReturnParameter";        COMMIT;
-- SKIP Firebird.2.5 BEGIN
-- SKIP Firebird.3 BEGIN
DROP PROCEDURE test_v4_types;
-- SKIP Firebird.2.5 END
-- SKIP Firebird.3 END
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
SELECT 1 FROM rdb$database
-- SKIP Firebird.4 END
-- SKIP Firebird.5 END
COMMIT;

DROP VIEW "PersonView";                         COMMIT;

DROP TRIGGER "CREATE_PersonID";                 COMMIT;
DROP TRIGGER "CREATE_DataTypeTest";             COMMIT;

DROP TABLE "Dual";                              COMMIT;
DROP TABLE "DataTypeTest";                      COMMIT;
DROP TABLE "Doctor";                            COMMIT;
DROP TABLE "Patient";                           COMMIT;
DROP TABLE "Person";                            COMMIT;

DROP GENERATOR "DataTypeID";                    COMMIT;
DROP GENERATOR "PersonID";                      COMMIT;

DROP EXTERNAL FUNCTION RTRIM;                   COMMIT;
DROP EXTERNAL FUNCTION LTRIM;                   COMMIT;


DECLARE EXTERNAL FUNCTION LTRIM
	CSTRING(255) NULL
	RETURNS CSTRING(255) FREE_IT
	ENTRY_POINT 'IB_UDF_ltrim' MODULE_NAME 'ib_udf';
COMMIT;

DECLARE EXTERNAL FUNCTION RTRIM
	CSTRING(255) NULL
	RETURNS CSTRING(255) FREE_IT
	ENTRY_POINT 'IB_UDF_rtrim' MODULE_NAME 'ib_udf';
COMMIT;


/*
Dual table FOR supporting queryies LIKE:
SELECT 1 AS id => SELECT 1 AS "id" *FROM Dual*
*/
CREATE TABLE "Dual" ("Dummy"  VARCHAR(10));
COMMIT;
INSERT INTO  "Dual" ("Dummy") VALUES ('X');
COMMIT;

DROP TABLE "InheritanceParent";
COMMIT;

CREATE TABLE "InheritanceParent"
(
	"InheritanceParentId" INTEGER     NOT NULL PRIMARY KEY,
	"TypeDiscriminator"   INTEGER,
	"Name"                VARCHAR(50)
);
COMMIT;

DROP TABLE "InheritanceChild";
COMMIT;

CREATE TABLE "InheritanceChild"
(
	"InheritanceChildId"  INTEGER     NOT NULL PRIMARY KEY,
	"InheritanceParentId" INTEGER     NOT NULL,
	"TypeDiscriminator"   INTEGER,
	"Name"                VARCHAR(50)
);
COMMIT;


-- Person Table

CREATE TABLE "Person"
(
	"PersonID"   INTEGER     NOT NULL  PRIMARY KEY,
	"FirstName"  VARCHAR(50) CHARACTER SET UNICODE_FSS NOT NULL,
	"LastName"   VARCHAR(50) CHARACTER SET UNICODE_FSS NOT NULL,
	"MiddleName" VARCHAR(50) CHARACTER SET UNICODE_FSS,
	"Gender"     CHAR(1)     NOT NULL CHECK ("Gender" in ('M', 'F', 'U', 'O'))
);
COMMIT;

CREATE GENERATOR "PersonID";
COMMIT;

CREATE TRIGGER "CREATE_PersonID" FOR "Person"
BEFORE INSERT POSITION 0
AS BEGIN
	NEW."PersonID" = GEN_ID("PersonID", 1);
END;
COMMIT;

INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('John',   'Pupkin',    'M');
COMMIT;
INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Tester', 'Testerson', 'M');
COMMIT;
INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Jane',   'Doe',       'F');
COMMIT;
-- INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Jürgen', 'König',     'M');
INSERT INTO "Person" ("FirstName", "LastName", "MiddleName", "Gender") VALUES (_utf8 x'4AC3BC7267656E', _utf8 x'4BC3B66E6967', 'Ko', 'M');
COMMIT;

-- Doctor Table Extension

CREATE TABLE "Doctor"
(
	"PersonID" INTEGER     NOT NULL PRIMARY KEY,
	"Taxonomy" VARCHAR(50) NOT NULL,
		CONSTRAINT "FK_Doctor_Person" FOREIGN KEY ("PersonID") REFERENCES "Person" ("PersonID")
			ON DELETE CASCADE
);
COMMIT;

INSERT INTO "Doctor" ("PersonID", "Taxonomy") VALUES (1, 'Psychiatry');
COMMIT;

-- Patient Table Extension

CREATE TABLE "Patient"
(
	"PersonID"  int           NOT NULL PRIMARY KEY,
	"Diagnosis" VARCHAR(256)  NOT NULL,
	FOREIGN KEY ("PersonID") REFERENCES "Person" ("PersonID")
			ON DELETE CASCADE
);
COMMIT;

INSERT INTO "Patient" ("PersonID", "Diagnosis") VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution');
COMMIT;

-- Data Types test

/*
Data definitions according to:
http://www.firebirdsql.org/manual/migration-mssql-data-types.html

BUT! BLOB is used for BINARY data! not CHAR
*/

CREATE TABLE "DataTypeTest"
(
	"DataTypeID"      INTEGER NOT NULL PRIMARY KEY,
	"Binary_"         BLOB,
-- SKIP Firebird.2.5 BEGIN
	"Boolean_"        BOOLEAN,
-- SKIP Firebird.2.5 END
-- SKIP Firebird.3 BEGIN
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
	"Boolean_"        CHAR(1),
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END
-- SKIP Firebird.3 END
	"Byte_"           SMALLINT,
	"Bytes_"          BLOB,
	CHAR_             CHAR(1),
	"DateTime_"       TIMESTAMP,
	"Decimal_"        DECIMAL(10, 2),
	"Double_"         DOUBLE PRECISION,
	"Guid_"           CHAR(16) CHARACTER SET OCTETS,
	"Int16_"          SMALLINT,
	"Int32_"          INTEGER,
	"Int64_"          NUMERIC(11),
	"Money_"          DECIMAL(18, 4),
	"SByte_"          SMALLINT,
	"Single_"         FLOAT,
	"Stream_"         BLOB,
	"String_"         VARCHAR(50) CHARACTER SET UNICODE_FSS,
	"UInt16_"         SMALLINT,
	"UInt32_"         INTEGER,
	"UInt64_"         NUMERIC(11),
	"Xml_"            CHAR(1000)
);
COMMIT;

CREATE GENERATOR "DataTypeID";
COMMIT;

CREATE TRIGGER "CREATE_DataTypeTest" FOR "DataTypeTest"
BEFORE INSERT POSITION 0
AS BEGIN
	NEW."DataTypeID" = GEN_ID("DataTypeID", 1);
END;
COMMIT;

INSERT INTO "DataTypeTest"
	("Binary_", "Boolean_",   "Byte_",  "Bytes_",  CHAR_,  "DateTime_", "Decimal_",
	 "Double_",    "Guid_",  "Int16_",  "Int32_",  "Int64_",    "Money_",   "SByte_",
	 "Single_",  "Stream_", "String_", "UInt16_", "UInt32_",   "UInt64_",     "Xml_")
VALUES
	(   NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
		NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
		NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL);
COMMIT;

INSERT INTO "DataTypeTest"
	("Binary_",	"Boolean_",	"Byte_",   "Bytes_",  CHAR_,		"DateTime_", "Decimal_",
	 "Double_",	"Guid_",	"Int16_",  "Int32_",  "Int64_",		"Money_",   "SByte_",
	 "Single_",	"Stream_",	"String_", "UInt16_", "UInt32_",	"UInt64_",
	 "Xml_")
VALUES
	('dddddddddddddddd',
-- SKIP Firebird.2.5 BEGIN
	TRUE
-- SKIP Firebird.2.5 END
-- SKIP Firebird.3 BEGIN
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
	'1'
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END
-- SKIP Firebird.3 END
	,255,'dddddddddddddddd', 'B', 'NOW', 12345.67,
	1234.567, X'dddddddddddddddddddddddddddddddd', 32767, 32768, 1000000, 12.3456, 127,
	1234.123, 'dddddddddddddddd', 'string', 32767, 32768, 200000000,
	'<root><element strattr="strvalue" intattr="12345"/></root>');
COMMIT;



DROP TABLE "Parent";     COMMIT;
DROP TABLE "Child";      COMMIT;
DROP TABLE "GrandChild"; COMMIT;

CREATE TABLE "Parent"      ("ParentID" int, "Value1" int);                    COMMIT;
CREATE TABLE "Child"       ("ParentID" int, "ChildID" int);                   COMMIT;
CREATE TABLE "GrandChild"  ("ParentID" int, "ChildID" int, "GrandChildID" int); COMMIT;


DROP TABLE "LinqDataTypes"; COMMIT;

CREATE TABLE "LinqDataTypes"
(
	ID               int,
	"MoneyValue"     decimal(10,4),
	"DateTimeValue"  timestamp,
	"DateTimeValue2" timestamp,
-- SKIP Firebird.2.5 BEGIN
	"BoolValue"      BOOLEAN,
-- SKIP Firebird.2.5 END
-- SKIP Firebird.3 BEGIN
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
	"BoolValue"      char(1),
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END
-- SKIP Firebird.3 END
	"GuidValue"      CHAR(16) CHARACTER SET OCTETS,
	"BinaryValue"    blob,
	"SmallIntValue"  smallint,
	"IntValue"       int,
	"BigIntValue"    bigint,
	"StringValue"    VARCHAR(50)
);
COMMIT;

DROP GENERATOR "SequenceTestSeq"; COMMIT;

CREATE GENERATOR "SequenceTestSeq";
COMMIT;

DROP TABLE "SequenceTest"; COMMIT;

CREATE TABLE "SequenceTest"
(
	ID       int         NOT NULL PRIMARY KEY,
	"Value_" VARCHAR(50) NOT NULL
);
COMMIT;


DROP TRIGGER CREATE_ID;
COMMIT;

DROP GENERATOR "TestIdentityID";
COMMIT;

DROP TABLE "TestIdentity";
COMMIT;

CREATE TABLE "TestIdentity" (
	ID INTEGER NOT NULL PRIMARY KEY
);
COMMIT;

CREATE GENERATOR "TestIdentityID";
COMMIT;

CREATE TRIGGER CREATE_ID FOR "TestIdentity"
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.ID = GEN_ID("TestIdentityID", 1);
END;
COMMIT;



DROP TRIGGER "AllTypes_ID";
COMMIT;

DROP GENERATOR "AllTypesID";
COMMIT;

DROP TABLE "AllTypes";
COMMIT;

CREATE TABLE "AllTypes"
(
	ID                       integer      NOT NULL PRIMARY KEY,

	"bigintDataType"           bigint,
	"smallintDataType"         smallint,
	"decimalDataType"          decimal(18),
	"intDataType"              int,
	"floatDataType"            float,
	"realDataType"             real,
	"doubleDataType"           double precision,

	"timestampDataType"        timestamp,

	"charDataType"             char(1),
	"char20DataType"           char(20),
	"varcharDataType"          varchar(20),
	"textDataType"             blob sub_type TEXT,
	"ncharDataType"            char(20) character set UNICODE_FSS,
	"nvarcharDataType"         varchar(20) character set UNICODE_FSS,

-- SKIP Firebird.2.5 BEGIN
-- SKIP Firebird.3 BEGIN
	"timestampTZDataType"      timestamp with time zone,
	"timeTZDataType"           time with time zone,
	"decfloat16DataType"       decfloat(16),
	"decfloat34DataType"       decfloat,
	"int128DataType"           int128,
-- SKIP Firebird.3 END
-- SKIP Firebird.2.5 END

	"blobDataType"             blob
);
COMMIT;

CREATE GENERATOR "AllTypesID";
COMMIT;

CREATE TRIGGER "AllTypes_ID" FOR "AllTypes"
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.ID = GEN_ID("AllTypesID", 1);
END;
COMMIT;

INSERT INTO "AllTypes"
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
	NULL,
	NULL,

-- SKIP Firebird.2.5 BEGIN
-- SKIP Firebird.3 BEGIN
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
-- SKIP Firebird.3 END
-- SKIP Firebird.2.5 END

	NULL
);
COMMIT;

INSERT INTO "AllTypes"
VALUES
(
	2,

	1000000,
	25555,
	2222222,
	7777777,
	20.31,
	16,
	16.17,

	Cast('2012-12-12 12:12:12' as timestamp),

	'1',
	'1',
	'234',
	'567',
	'23233',
	'3323',

-- SKIP Firebird.2.5 BEGIN
-- SKIP Firebird.3 BEGIN
	'2020-12-12 12:24:35 Europe/Andorra',
	'12:13 Australia/Hobart',
	1234567890.123456,
	123456789012345678901234567890.1234,
	170141183460469231731687303715884105727,
-- SKIP Firebird.3 END
-- SKIP Firebird.2.5 END

	'12345'
);
COMMIT;


CREATE VIEW "PersonView"
AS
	SELECT * FROM "Person";
COMMIT;


-- Person_SelectByKey

CREATE PROCEDURE "Person_SelectByKey"(id INTEGER)
RETURNS (
	PersonID   INTEGER,
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1)
	)
AS
BEGIN
	SELECT "PersonID", "FirstName", "LastName", "MiddleName", "Gender" FROM "Person"
	WHERE "PersonID" = :id
	INTO
		:PersonID,
		:FirstName,
		:LastName,
		:MiddleName,
		:Gender;
	SUSPEND;
END;
COMMIT;

-- Person_SelectAll

CREATE PROCEDURE "Person_SelectAll"
RETURNS (
	PersonID   INTEGER,
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1)
	)
AS
BEGIN
	FOR
		SELECT "PersonID", "FirstName", "LastName", "MiddleName", "Gender" FROM "Person"
		INTO
			:PersonID,
			:FirstName,
			:LastName,
			:MiddleName,
			:Gender
	DO SUSPEND;
END;
COMMIT;

-- Person_SelectByName

CREATE PROCEDURE "Person_SelectByName"
(
	in_FirstName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	in_LastName  VARCHAR(50) CHARACTER SET UNICODE_FSS
)
RETURNS
(
	PersonID   int,
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1)
)
AS
BEGIN

	FOR SELECT "PersonID", "FirstName", "LastName", "MiddleName", "Gender" FROM "Person"
		WHERE "FirstName" LIKE :in_FirstName and "LastName" LIKE :in_LastName
	INTO
		:PersonID,
		:FirstName,
		:LastName,
		:MiddleName,
		:Gender
	DO SUSPEND;
END;
COMMIT;

-- Person_Insert

CREATE PROCEDURE "Person_Insert"
(
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1)
)
RETURNS (PersonID INTEGER)
AS
BEGIN
	INSERT INTO "Person"
		( "LastName",  "FirstName",  "MiddleName",  "Gender")
	VALUES
		(:LastName, :FirstName, :MiddleName, :Gender);

	SELECT MAX("PersonID") FROM "Person"
		INTO :PersonID;
	SUSPEND;
END;
COMMIT;

-- Person_Insert_OutputParameter

CREATE PROCEDURE "Person_Insert_OutputParameter"
(
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1)
)
RETURNS (PersonID INTEGER)
AS
BEGIN
	INSERT INTO "Person"
		( "LastName",  "FirstName",  "MiddleName",  "Gender")
	VALUES
		(:LastName, :FirstName, :MiddleName, :Gender);

	SELECT max("PersonID") FROM "Person"
	INTO :PersonID;
	SUSPEND;
END;
COMMIT;

-- Person_Update

CREATE PROCEDURE "Person_Update"(
	PersonID   INTEGER,
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1)
	)
AS
BEGIN
	UPDATE
		"Person"
	SET
		"LastName"   = :LastName,
		"FirstName"  = :FirstName,
		"MiddleName" = :MiddleName,
		"Gender"     = :Gender
	WHERE
		"PersonID" = :PersonID;
END;
COMMIT;

-- Person_Delete

CREATE PROCEDURE "Person_Delete"(
	"PersonID" INTEGER
	)
AS
BEGIN
	DELETE FROM "Person" WHERE "PersonID" = :"PersonID";
END;
COMMIT;

-- Patient_SelectAll

CREATE PROCEDURE "Patient_SelectAll"
RETURNS (
	PersonID   int,
	FirstName  VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName   VARCHAR(50) CHARACTER SET UNICODE_FSS,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1),
	Diagnosis  VARCHAR(256)
	)
AS
BEGIN
	FOR
		SELECT
			"Person"."PersonID",
			"FirstName",
			"LastName",
			"MiddleName",
			"Gender",
			"Patient"."Diagnosis"
		FROM
			"Patient", "Person"
		WHERE
			"Patient"."PersonID" = "Person"."PersonID"
		INTO
			:PersonID,
			:FirstName,
			:LastName,
			:MiddleName,
			:Gender,
			:Diagnosis
	DO SUSPEND;
END;
COMMIT;

-- Patient_SelectByName

CREATE PROCEDURE "Patient_SelectByName"(
	FirstName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	LastName  VARCHAR(50) CHARACTER SET UNICODE_FSS
	)
RETURNS (
	PersonID   int,
	MiddleName VARCHAR(50) CHARACTER SET UNICODE_FSS,
	Gender     CHAR(1),
	Diagnosis  VARCHAR(256)
	)
AS
BEGIN
	FOR
		SELECT
			"Person"."PersonID",
			"MiddleName",
			"Gender",
			"Patient"."Diagnosis"
		FROM
			"Patient", "Person"
		WHERE
			"Patient"."PersonID" = "Person"."PersonID"
			and "FirstName" = :FirstName and "LastName" = :LastName
		INTO
			:PersonID,
			:MiddleName,
			:Gender,
			:Diagnosis
	DO SUSPEND;
END;
COMMIT;


-- OutRefTest

/*
Fake input parameters are used to "emulate" input/output parameters.
Each inout parameter should be defined in RETURNS(...) section
and allso have a "mirror" in input section, mirror name shoul be:
FdpDataProvider.InOutInputParameterPrefix + [parameter name]
ex:
in_inputOutputID is input mirror FOR inout parameter inputOutputID
*/
CREATE PROCEDURE "OutRefTest"(
	ID					INTEGER,
	in_inputOutputID	INTEGER,
	str					VARCHAR(50),
	in_inputOutputStr	VARCHAR(50)
	)
RETURNS(
	inputOutputID  INTEGER,
	inputOutputStr VARCHAR(50),
	outputID       INTEGER,
	outputStr      VARCHAR(50)
	)
AS
BEGIN
	outputID       = ID;
	inputOutputID  = ID + in_inputOutputID;
	outputStr      = str;
	inputOutputStr = str || in_inputOutputStr;
	SUSPEND;
END;
COMMIT;

-- OutRefEnumTest

CREATE PROCEDURE "OutRefEnumTest"(
		str					VARCHAR(50),
		in_inputOutputStr	VARCHAR(50)
		)
RETURNS (
	inputOutputStr VARCHAR(50),
	outputStr      VARCHAR(50)
	)
AS
BEGIN
	outputStr      = str;
	inputOutputStr = str || in_inputOutputStr;
	SUSPEND;
END;
COMMIT;

-- ExecuteScalarTest

CREATE PROCEDURE "Scalar_DataReader"
RETURNS(
	intField	INTEGER,
	stringField	VARCHAR(50)
	)
AS
BEGIN
	intField = 12345;
	stringField = '54321';
	SUSPEND;
END;
COMMIT;

CREATE PROCEDURE "Scalar_OutputParameter"
RETURNS (
	outputInt      INTEGER,
	outputString   VARCHAR(50)
	)
AS
BEGIN
	outputInt = 12345;
	outputString = '54321';
	SUSPEND;
END;
COMMIT;

/*
"Return_Value" is the name for ReturnValue "emulating"
may be changed: FdpDataProvider.ReturnParameterName
*/
CREATE PROCEDURE "Scalar_ReturnParameter"
RETURNS (Return_Value INTEGER)
AS
BEGIN
	Return_Value = 12345;
	SUSPEND;
END;
COMMIT;

DROP TABLE "CamelCaseName";
COMMIT;

CREATE TABLE "CamelCaseName"
(
	"Id"     INTEGER NOT NULL PRIMARY KEY,
	Name1    VARCHAR(20),
	"Name2"  VARCHAR(20),
	"NAME3"  VARCHAR(20),
	"_NAME4" VARCHAR(20),
	"NAME 5" VARCHAR(20)
);
COMMIT;


DROP TABLE "TestMerge1";                            COMMIT;
DROP TABLE "TestMerge2";                            COMMIT;

CREATE TABLE "TestMerge1"
(
	"Id"     INTEGER     NOT NULL PRIMARY KEY,
	"Field1" INTEGER,
	"Field2" INTEGER,
	"Field3" INTEGER,
	"Field4" INTEGER,
	"Field5" INTEGER,

	"FieldInt64"      BIGINT,
-- SKIP Firebird.2.5 BEGIN
	"FieldBoolean"    BOOLEAN,
-- SKIP Firebird.2.5 END
-- SKIP Firebird.3 BEGIN
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
	"FieldBoolean"    CHAR(1),
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END
-- SKIP Firebird.3 END
	"FieldString"     VARCHAR(20),
	"FieldNString"    VARCHAR(20) CHARACTER SET UNICODE_FSS,
	"FieldChar"       CHAR(1),
	"FieldNChar"      CHAR(1) CHARACTER SET UNICODE_FSS,
	"FieldFloat"      FLOAT,
	"FieldDouble"     DOUBLE PRECISION,
	"FieldDateTime"   TIMESTAMP,
	"FieldBinary"     BLOB(20),
	"FieldGuid"       CHAR(16) CHARACTER SET OCTETS,
	"FieldDecimal"    DECIMAL(18, 10),
	"FieldDate"       DATE,
	"FieldTime"       TIMESTAMP,
	"FieldEnumString" VARCHAR(20),
	"FieldEnumNumber" INT
);
COMMIT;

CREATE TABLE "TestMerge2"
(
	"Id"     INTEGER     NOT NULL PRIMARY KEY,
	"Field1" INTEGER,
	"Field2" INTEGER,
	"Field3" INTEGER,
	"Field4" INTEGER,
	"Field5" INTEGER,

	"FieldInt64"      BIGINT,
-- SKIP Firebird.2.5 BEGIN
	"FieldBoolean"    BOOLEAN,
-- SKIP Firebird.2.5 END
-- SKIP Firebird.3 BEGIN
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
	"FieldBoolean"    CHAR(1),
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END
-- SKIP Firebird.3 END
	"FieldString"     VARCHAR(20),
	"FieldNString"    VARCHAR(20) CHARACTER SET UNICODE_FSS,
	"FieldChar"       CHAR(1),
	"FieldNChar"      CHAR(1) CHARACTER SET UNICODE_FSS,
	"FieldFloat"      FLOAT,
	"FieldDouble"     DOUBLE PRECISION,
	"FieldDateTime"   TIMESTAMP,
	"FieldBinary"     BLOB(20),
	"FieldGuid"       CHAR(16) CHARACTER SET OCTETS,
	"FieldDecimal"    DECIMAL(18, 10),
	"FieldDate"       DATE,
	"FieldTime"       TIMESTAMP,
	"FieldEnumString" VARCHAR(20),
	"FieldEnumNumber" INT
);
COMMIT;

CREATE PROCEDURE "AddIssue792Record"
AS
BEGIN
	INSERT INTO "AllTypes"("char20DataType") VALUES('issue792');
END;
COMMIT;

-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
SELECT 1 FROM rdb$database
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END

-- SKIP Firebird.2.5 BEGIN
-- SKIP Firebird.3 BEGIN
CREATE PROCEDURE test_v4_types
(
	tstz       timestamp with time zone,
	ttz        time with time zone,
	decfloat16 decfloat(16),
	decfloat34 decfloat,
	int_128    int128
)
RETURNS
(
	col_tstz       timestamp with time zone,
	col_ttz        time with time zone,
	col_decfloat16 decfloat(16),
	col_decfloat34 decfloat,
	col_int_128    int128
)
AS
BEGIN
	FOR SELECT FIRST 1 :tstz, :ttz, :decfloat16, :decfloat34, :int_128 FROM rdb$database
	INTO
		:col_tstz,
		:col_ttz,
		:col_decfloat16,
		:col_decfloat34,
		:col_int_128
	DO SUSPEND;
END;
-- SKIP Firebird.3 END
-- SKIP Firebird.2.5 END
COMMIT;

DROP TABLE "CollatedTable"
COMMIT;

CREATE TABLE "CollatedTable"
(
	"Id"				INT NOT NULL,
	"CaseSensitive"		VARCHAR(20) CHARACTER SET UTF8 COLLATE UNICODE,
	"CaseInsensitive"	VARCHAR(20) CHARACTER SET UTF8 COLLATE UNICODE_CI
)
COMMIT;

-- SKIP Firebird.2.5 BEGIN

CREATE OR ALTER PACKAGE TEST_PACKAGE1
AS
BEGIN
	PROCEDURE	TEST_PROCEDURE(I INT)	RETURNS (O INT);
	PROCEDURE	TEST_TABLE_FUNCTION(I INT)	RETURNS (O INT);
	FUNCTION	TEST_FUNCTION(I INT)	RETURNS INT;
END
COMMIT;

RECREATE PACKAGE BODY TEST_PACKAGE1
AS
BEGIN
	PROCEDURE TEST_PROCEDURE(I INT) RETURNS (O INT)
	AS
	BEGIN
		O = I + 1;
	END
	PROCEDURE TEST_TABLE_FUNCTION(I INT) RETURNS (O INT)
	AS
	BEGIN
		FOR SELECT :I + 1 FROM "Person"
		INTO :O
		DO SUSPEND;
	END
	FUNCTION TEST_FUNCTION(I INT) RETURNS INT
	AS
	BEGIN
		RETURN I + 1;
	END
END
COMMIT;

CREATE OR ALTER PACKAGE TEST_PACKAGE2
AS
BEGIN
	PROCEDURE TEST_PROCEDURE(I INT) RETURNS (O INT);
	PROCEDURE TEST_TABLE_FUNCTION(I INT) RETURNS (O INT);
	FUNCTION TEST_FUNCTION(I INT) RETURNS INT;
END
COMMIT;

RECREATE PACKAGE BODY TEST_PACKAGE2
AS
BEGIN
	PROCEDURE TEST_PROCEDURE(I INT) RETURNS (O INT)
	AS
	BEGIN
		O = I + 2;
	END
	PROCEDURE TEST_TABLE_FUNCTION(I INT) RETURNS (O INT)
	AS
	BEGIN
		FOR SELECT :I + 2 FROM "Person"
		INTO :O
		DO SUSPEND;
	END
	FUNCTION TEST_FUNCTION(I INT) RETURNS INT
	AS
	BEGIN
		RETURN I + 2;
	END
END
COMMIT;

CREATE  PROCEDURE TEST_PROCEDURE(I INT) RETURNS (O INT)
AS
	BEGIN
		O = I + 3;
	END
COMMIT;

CREATE PROCEDURE TEST_TABLE_FUNCTION(I INT)
RETURNS (O INT)
AS
BEGIN
	FOR SELECT :I + 3 FROM "Person"
	INTO :O
	DO SUSPEND;
END
COMMIT;

CREATE  FUNCTION TEST_FUNCTION(I INT) RETURNS INT
AS
	BEGIN
		RETURN I + 3;
	END

-- SKIP Firebird.2.5 END
-- SKIP Firebird.3 BEGIN
-- SKIP Firebird.4 BEGIN
-- SKIP Firebird.5 BEGIN
SELECT 1 FROM rdb$database
-- SKIP Firebird.5 END
-- SKIP Firebird.4 END
-- SKIP Firebird.3 END
COMMIT;
