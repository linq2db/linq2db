DROP TABLE IF EXISTS "Doctor"
GO
DROP TABLE IF EXISTS "Patient"
GO
DROP TABLE IF EXISTS "Person"
GO

DROP TABLE IF EXISTS "InheritanceParent"
GO

CREATE TABLE "InheritanceParent"
(
	"InheritanceParentId" INTEGER       PRIMARY KEY,
	"TypeDiscriminator"   INTEGER                       NULL,
	"Name"                VARCHAR(50)                   NULL
)
GO

DROP TABLE IF EXISTS "InheritanceChild"
GO

CREATE TABLE "InheritanceChild"
(
	"InheritanceChildId"  INTEGER      PRIMARY KEY,
	"InheritanceParentId" INTEGER                  NOT NULL,
	"TypeDiscriminator"   INTEGER                      NULL,
	"Name"                VARCHAR(50)                  NULL
)
GO


DROP SEQUENCE IF EXISTS "Person_PersonID_seq"
GO

CREATE SEQUENCE "Person_PersonID_seq" START 1
GO

CREATE TABLE "Person"
(
	"PersonID"   INTEGER PRIMARY KEY DEFAULT NEXTVAL('"Person_PersonID_seq"'),
	"FirstName"  VARCHAR(50) NOT NULL,
	"LastName"   VARCHAR(50) NOT NULL,
	"MiddleName" VARCHAR(50),
	"Gender"     CHAR(1)     NOT NULL
)
GO

INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('John',   'Pupkin',    'M')
GO
INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Tester', 'Testerson', 'M')
GO
INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Jane',   'Doe',       'F')
GO
INSERT INTO "Person" ("FirstName", "LastName", "MiddleName", "Gender") VALUES ('Jürgen', 'König', 'Ko', 'M')
GO

-- Doctor Table Extension

CREATE TABLE "Doctor"
(
	"PersonID" INTEGER     NOT NULL PRIMARY KEY,
	"Taxonomy" VARCHAR(50) NOT NULL
)
GO

INSERT INTO "Doctor" ("PersonID", "Taxonomy") VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE "Patient"
(
	"PersonID"  INTEGER      NOT NULL PRIMARY KEY,
	"Diagnosis" VARCHAR(256) NOT NULL
)
GO

INSERT INTO "Patient" ("PersonID", "Diagnosis") VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
GO


DROP TABLE IF EXISTS "Parent"
GO
DROP TABLE IF EXISTS "Child"
GO
DROP TABLE IF EXISTS "GrandChild"
GO

CREATE TABLE "Parent"      ("ParentID" int, "Value1" int)
GO
CREATE TABLE "Child"       ("ParentID" int, "ChildID" int)
GO
CREATE TABLE "GrandChild"  ("ParentID" int, "ChildID" int, "GrandChildID" int)
GO


DROP TABLE IF EXISTS "LinqDataTypes"
GO

CREATE TABLE "LinqDataTypes"
(
	"ID"             int,
	"MoneyValue"     decimal(10,4),
	"DateTimeValue"  timestamp,
	"DateTimeValue2" timestamp,
	"BoolValue"      boolean,
	"GuidValue"      uuid,
	"BinaryValue"    blob         NULL,
	"SmallIntValue"  smallint,
	"IntValue"       int          NULL,
	"BigIntValue"    bigint       NULL,
	"StringValue"    varchar(50)  NULL
)
GO

DROP SEQUENCE IF EXISTS SequenceTestSeq
GO

CREATE SEQUENCE SequenceTestSeq INCREMENT BY 1 START 1
GO

DROP TABLE IF EXISTS "SequenceTest1"
GO

DROP TABLE IF EXISTS "SequenceTest2"
GO

DROP TABLE IF EXISTS "SequenceTest3"
GO

DROP SEQUENCE IF EXISTS "SequenceTest2_ID_seq"
GO

CREATE SEQUENCE "SequenceTest2_ID_seq" INCREMENT BY 1 START 1
GO

CREATE TABLE "SequenceTest1"
(
	"ID"    INTEGER PRIMARY KEY,
	"Value" VARCHAR(50)
)
GO

CREATE TABLE "SequenceTest2"
(
	"ID"    INTEGER PRIMARY KEY DEFAULT NEXTVAL('"SequenceTest2_ID_seq"'),
	"Value" VARCHAR(50)
)
GO

CREATE TABLE "SequenceTest3"
(
	"ID"    INTEGER PRIMARY KEY DEFAULT NEXTVAL('SequenceTestSeq'),
	"Value" VARCHAR(50)
)
GO

DROP TABLE IF EXISTS "TestIdentity"
GO

DROP SEQUENCE IF EXISTS "TestIdentity_ID_seq"
GO

CREATE SEQUENCE "TestIdentity_ID_seq" START 1
GO

CREATE TABLE "TestIdentity" (
	"ID" INTEGER PRIMARY KEY DEFAULT NEXTVAL('"TestIdentity_ID_seq"')
)
GO


DROP TABLE IF EXISTS "AllTypes"
GO

DROP SEQUENCE IF EXISTS "AllTypes_ID_seq"
GO

CREATE SEQUENCE "AllTypes_ID_seq" START 1
GO

CREATE TABLE "AllTypes"
(
	"ID"                  INTEGER      NOT NULL PRIMARY KEY DEFAULT NEXTVAL('"AllTypes_ID_seq"'),

	"bigintDataType"      BIGINT                   NULL,
	"numericDataType"     NUMERIC                  NULL,
	"smallintDataType"    SMALLINT                 NULL,
	"intDataType"         INTEGER                  NULL,
	"decimalDataType"     DECIMAL(6,3)             NULL,
	"moneyDataType"       DECIMAL(19,4)            NULL,
	"doubleDataType"      DOUBLE                   NULL,
	"realDataType"        FLOAT                    NULL,

	"timestampDataType"   TIMESTAMP                NULL,
	"timestampTZDataType" TIMESTAMPTZ              NULL,
	"dateDataType"        DATE                     NULL,
	"timeDataType"        TIME                     NULL,
	"intervalDataType"    INTERVAL                 NULL,
	"intervalDataType2"   INTERVAL                 NULL,

	"charDataType"        VARCHAR(1)               NULL,
	"ncharDataType"       VARCHAR(20)              NULL,
	"char20DataType"      VARCHAR(20)              NULL,
	"varcharDataType"     VARCHAR(20)              NULL,
	"textDataType"        VARCHAR                  NULL,
	"floatDataType"       FLOAT                    NULL,

	"binaryDataType"      BLOB                     NULL,
	"varBinaryDataType"   BLOB                     NULL,

	"uuidDataType"        UUID                     NULL,
	"booleanDataType"     BOOLEAN                  NULL,

	"jsonDataType"        JSON                     NULL
)
GO

INSERT INTO "AllTypes"
(
	"bigintDataType",
	"numericDataType",
	"smallintDataType",
	"intDataType",
	"moneyDataType",
	"doubleDataType",
	"realDataType",

	"timestampDataType",
	"timestampTZDataType",
	"dateDataType",
	"timeDataType",
	"intervalDataType",

	"charDataType",
	"varcharDataType",
	"textDataType",

	"uuidDataType",
	"booleanDataType"
)
SELECT
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
	NULL,

	NULL,
	NULL
UNION ALL
SELECT
	1000000,
	9999999,
	25555,
	7777777,
	100000,
	20.31,
	16.2,

	CAST('2012-12-12 12:12:12' AS TIMESTAMP),
	CAST('2012-12-12 12:12:12-04' AS TIMESTAMPTZ),
	CAST('2012-12-12' AS DATE),
	CAST('12:12:12' AS TIME),
	CAST('1 day 3 hours 5 minutes 20 seconds' AS INTERVAL),

	'1',
	'234',
	'567',

	CAST('6F9619FF-8B86-D011-B42D-00C04FC964FF' AS UUID),
	true

GO

DROP TABLE IF EXISTS "TestMerge1"
GO

DROP TABLE IF EXISTS "TestMerge2"
GO

CREATE TABLE "TestMerge1"
(
	"Id"		INTEGER	PRIMARY KEY,
	"Field1"	INTEGER	NULL,
	"Field2"	INTEGER	NULL,
	"Field3"	INTEGER	NULL,
	"Field4"	INTEGER	NULL,
	"Field5"	INTEGER	NULL,

	"FieldInt64"      BIGINT                   NULL,
	"FieldBoolean"    BOOLEAN                  NULL,
	"FieldString"     VARCHAR(20)              NULL,
	"FieldNString"    VARCHAR(20)              NULL,
	"FieldChar"       VARCHAR(1)               NULL,
	"FieldNChar"      VARCHAR(1)               NULL,
	"FieldFloat"      FLOAT                    NULL,
	"FieldDouble"     DOUBLE                   NULL,
	"FieldDateTime"   TIMESTAMP                NULL,
	"FieldDateTime2"  TIMESTAMPTZ              NULL,
	"FieldBinary"     BLOB                     NULL,
	"FieldGuid"       UUID                     NULL,
	"FieldDecimal"    DECIMAL(24, 10)          NULL,
	"FieldDate"       DATE                     NULL,
	"FieldTime"       TIME                     NULL,
	"FieldEnumString" VARCHAR(20)              NULL,
	"FieldEnumNumber" INT                      NULL
)
GO

CREATE TABLE "TestMerge2"
(
	"Id"		INTEGER	PRIMARY KEY,
	"Field1"	INTEGER	NULL,
	"Field2"	INTEGER	NULL,
	"Field3"	INTEGER	NULL,
	"Field4"	INTEGER	NULL,
	"Field5"	INTEGER	NULL,

	"FieldInt64"      BIGINT                   NULL,
	"FieldBoolean"    BOOLEAN                  NULL,
	"FieldString"     VARCHAR(20)              NULL,
	"FieldNString"    VARCHAR(20)              NULL,
	"FieldChar"       VARCHAR(1)               NULL,
	"FieldNChar"      VARCHAR(1)               NULL,
	"FieldFloat"      FLOAT                    NULL,
	"FieldDouble"     DOUBLE                   NULL,
	"FieldDateTime"   TIMESTAMP                NULL,
	"FieldDateTime2"  TIMESTAMPTZ              NULL,
	"FieldBinary"     BLOB                     NULL,
	"FieldGuid"       UUID                     NULL,
	"FieldDecimal"    DECIMAL(24, 10)          NULL,
	"FieldDate"       DATE                     NULL,
	"FieldTime"       TIME                     NULL,
	"FieldEnumString" VARCHAR(20)              NULL,
	"FieldEnumNumber" INT                      NULL
)
GO

DROP TABLE IF EXISTS "CollatedTable"
GO
CREATE TABLE "CollatedTable"
(
	"Id"				INT NOT NULL,
	"CaseSensitive"		VARCHAR(20) NOT NULL,
	"CaseInsensitive"	VARCHAR(20) NOT NULL
)
GO

DROP TABLE IF EXISTS "TestMergeIdentity"
GO
DROP SEQUENCE IF EXISTS "TestMergeIdentity_Id_seq"
GO

CREATE SEQUENCE "TestMergeIdentity_Id_seq" START 1
GO

CREATE TABLE "TestMergeIdentity"
(
	"Id"     INTEGER NOT NULL PRIMARY KEY DEFAULT NEXTVAL('"TestMergeIdentity_Id_seq"'),
	"Field"  INT NULL
)
GO
