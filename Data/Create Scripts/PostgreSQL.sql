﻿DROP TABLE IF EXISTS "Doctor"
GO

DROP TABLE IF EXISTS "Patient"
GO

DROP FUNCTION IF EXISTS "TestTableFunctionSchema"()
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


CREATE TABLE "Person"
(
	--PersonID   INTEGER PRIMARY KEY DEFAULT NEXTVAL('Seq'),
	"PersonID"   SERIAL PRIMARY KEY,
	"FirstName"  VARCHAR(50) NOT NULL,
	"LastName"   VARCHAR(50) NOT NULL,
	"MiddleName" VARCHAR(50),
	"Gender"     CHAR(1)     NOT NULL
)
GO

COMMENT ON TABLE  "Person"            IS 'This is the Person table';
COMMENT ON COLUMN "Person"."PersonID" IS 'This is the Person.PersonID column';
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
	"PersonID" INTEGER     primary key references "Person"("PersonID") NOT NULL,
	"Taxonomy" VARCHAR(50) NOT NULL
)
GO

INSERT INTO "Doctor" ("PersonID", "Taxonomy") VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE "Patient"
(
	"PersonID"  INTEGER      primary key references "Person"("PersonID") NOT NULL,
	"Diagnosis" VARCHAR(256) NOT NULL
)
GO

INSERT INTO "Patient" ("PersonID", "Diagnosis") VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
GO


CREATE OR REPLACE FUNCTION reverse(text) RETURNS text
	AS $_$
DECLARE
original alias for $1;
	reverse_str text;
	i int4;
BEGIN
	reverse_str := '';
	FOR i IN REVERSE LENGTH(original)..1 LOOP
		reverse_str := reverse_str || substr(original,i,1);
	END LOOP;
RETURN reverse_str;
END;$_$
	LANGUAGE plpgsql IMMUTABLE;
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
	"BinaryValue"    bytea       NULL,
	"SmallIntValue"  smallint,
	"IntValue"       int         NULL,
	"BigIntValue"    bigint      NULL,
	"StringValue"    varchar(50) NULL
)
GO

CREATE OR REPLACE FUNCTION "GetParentByID"(id int)
RETURNS TABLE ("ParentID" int, "Value1" int)
AS $$ SELECT * FROM "Parent" WHERE "ParentID" = $1 $$
LANGUAGE SQL;
GO

DROP TABLE IF EXISTS  entity
GO

CREATE TABLE entity
(
	the_name character varying(255) NOT NULL,
	CONSTRAINT entity_name_key UNIQUE (the_name)
)
GO

CREATE OR REPLACE FUNCTION add_if_not_exists(p_name character varying)
	RETURNS void AS
$BODY$
BEGIN
	BEGIN
		insert into entity(the_name) values(p_name);
	EXCEPTION WHEN unique_violation THEN
		-- is exists, do nothing
	END;
END;
$BODY$
	LANGUAGE plpgsql;
GO


DROP TABLE IF EXISTS "SequenceTest1"
GO

DROP TABLE IF EXISTS "SequenceTest2"
GO

DROP TABLE IF EXISTS "SequenceTest3"
GO

DROP TABLE IF EXISTS "SequenceCustomNamingTest"
GO

DROP SEQUENCE IF EXISTS SequenceTestSeq
GO

CREATE SEQUENCE SequenceTestSeq INCREMENT 1 START 1
GO

DROP SEQUENCE IF EXISTS "SequenceTest2_ID_seq"
GO

CREATE SEQUENCE "SequenceTest2_ID_seq" INCREMENT 1 START 1
GO

DROP SEQUENCE IF EXISTS test_schema."SequenceCustomNamingTest__seq__"
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

CREATE SEQUENCE "TestIdentity_ID_seq" INCREMENT 1 START 1
GO

CREATE TABLE "TestIdentity" (
	"ID" INTEGER PRIMARY KEY DEFAULT NEXTVAL('"TestIdentity_ID_seq"')
)
GO


DROP TABLE IF EXISTS AllTypes
GO
DROP TABLE IF EXISTS "AllTypes"
GO

DROP TYPE IF EXISTS color
GO

CREATE TYPE color AS ENUM ('Red', 'Green', 'Blue');
GO

CREATE TABLE "AllTypes"
(
	"ID"                  serial               NOT NULL PRIMARY KEY,

	"bigintDataType"      bigint                   NULL,
	"numericDataType"     numeric                  NULL,
	"smallintDataType"    smallint                 NULL,
	"intDataType"         int                      NULL,
	"moneyDataType"       money                    NULL,
	"doubleDataType"      double precision         NULL,
	"realDataType"        real                     NULL,

	"timestampDataType"   timestamp                NULL,
	"timestampTZDataType" timestamp with time zone NULL,
	"dateDataType"        date                     NULL,
	"timeDataType"        time                     NULL,
	"timeTZDataType"      time with time zone      NULL,
	"intervalDataType"    interval                 NULL,

	"charDataType"        char(1)                  NULL,
	"char20DataType"      char(20)                 NULL,
	"varcharDataType"     varchar(20)              NULL,
	"textDataType"        text                     NULL,

	"binaryDataType"      bytea                    NULL,

	"uuidDataType"        uuid                     NULL,
	"bitDataType"         bit(3)                   NULL,
	"booleanDataType"     boolean                  NULL,
	"colorDataType"       color                    NULL,

	"pointDataType"       point                    NULL,
	"lsegDataType"        lseg                     NULL,
	"boxDataType"         box                      NULL,
	"pathDataType"        path                     NULL,
	"polygonDataType"     polygon                  NULL,
	"circleDataType"      circle                   NULL,
	"lineDataType"        line                     NULL,

	"inetDataType"        inet                     NULL,
	"cidrDataType"        cidr                     NULL,
	"macaddrDataType"     macaddr                  NULL,
-- SKIP PostgreSQL.9.2 BEGIN
-- SKIP PostgreSQL.9.3 BEGIN
-- SKIP PostgreSQL.9.5 BEGIN
-- SKIP PostgreSQL BEGIN
	"macaddr8DataType"  macaddr8                   NULL,
-- SKIP PostgreSQL.9.2 END
-- SKIP PostgreSQL.9.3 END
-- SKIP PostgreSQL.9.5 END
-- SKIP PostgreSQL END

	"jsonDataType"        json                     NULL,
-- SKIP PostgreSQL.9.2 BEGIN
-- SKIP PostgreSQL.9.3 BEGIN
"jsonbDataType"       jsonb                    NULL,
-- SKIP PostgreSQL.9.2 END
-- SKIP PostgreSQL.9.3 END

	"xmlDataType"         xml                      NULL,
	"varBitDataType"      varbit                   NULL
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
	"timeTZDataType",
	"intervalDataType",

	"charDataType",
	"varcharDataType",
	"textDataType",

	"binaryDataType",

	"uuidDataType",
	"bitDataType",
	"booleanDataType",
	"colorDataType",

	"pointDataType",
	"lsegDataType",
	"boxDataType",
	"pathDataType",
	"polygonDataType",
	"circleDataType",

	"inetDataType",
	"macaddrDataType",

	"xmlDataType",
	"varBitDataType"
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

	Cast('2012-12-12 12:12:12' as timestamp),
	Cast('2012-12-12 12:12:12-04' as timestamp with time zone),
	Cast('2012-12-12 12:12:12' as date),
	Cast('2012-12-12 12:12:12' as time),
	Cast('12:12:12' as time with time zone),
	Cast('1 3:05:20' as interval),

	'1',
	'234',
	'567',

	E'\\052'::bytea,

	Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid),
	B'101',
	true,
	'Green'::color,

	'(1,2)'::point,
	'((1,2),(3,4))'::lseg,
	'((1,2),(3,4))'::box,
	'((1,2),(3,4))'::path,
	'((1,2),(3,4))'::polygon,
	'((1,2),3)'::circle,

	'192.168.1.1'::inet,
	'01:02:03:04:05:06'::macaddr,

	XMLPARSE (DOCUMENT'<root><element strattr="strvalue" intattr="12345"/></root>'),

	B'1011'

GO

DROP TABLE IF EXISTS TestSameName
GO

DROP TABLE IF EXISTS test_schema.TestSameName
GO

DROP TABLE IF EXISTS test_schema.TestSerialIdentity
GO

DROP TABLE IF EXISTS test_schema."TestSchemaIdentity"
GO

DROP SEQUENCE IF EXISTS test_schema."TestSchemaIdentity_ID_seq"
GO

DROP SCHEMA IF EXISTS test_schema
GO

CREATE SCHEMA test_schema
GO

CREATE SEQUENCE test_schema."TestSchemaIdentity_ID_seq" INCREMENT 1 START 1
GO


CREATE SEQUENCE test_schema."SequenceCustomNamingTest__seq__" INCREMENT 1 START 1
GO

CREATE TABLE "SequenceCustomNamingTest"
(
	"ID"    INTEGER PRIMARY KEY DEFAULT NEXTVAL('test_schema."SequenceCustomNamingTest__seq__"'),
	"Value" VARCHAR(50)
)
GO


CREATE TABLE test_schema."TestSchemaIdentity" (
	"ID" INTEGER PRIMARY KEY DEFAULT NEXTVAL('test_schema."TestSchemaIdentity_ID_seq"')
)
GO

CREATE TABLE test_schema.TestSerialIdentity
(
	"ID" serial NOT NULL PRIMARY KEY
)
GO

CREATE TABLE test_schema.TestSameName
(
	ID serial NOT NULL PRIMARY KEY
)
GO

CREATE TABLE TestSameName
(
	ID serial NOT NULL PRIMARY KEY
)
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
	"FieldChar"       CHAR(1)                  NULL,
	"FieldNChar"      CHAR(1)                  NULL,
	"FieldFloat"      FLOAT(24)                NULL,
	"FieldDouble"     FLOAT(53)                NULL,
	"FieldDateTime"   TIMESTAMP                NULL,
	"FieldDateTime2"  TIMESTAMP WITH TIME ZONE NULL,
	"FieldBinary"     BYTEA                    NULL,
	"FieldGuid"       UUID                     NULL,
	"FieldDecimal"    DECIMAL(24, 10)          NULL,
	"FieldDate"       DATE                     NULL,
	"FieldTime"       TIME(7)                  NULL,
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
	"FieldChar"       CHAR(1)                  NULL,
	"FieldNChar"      CHAR(1)                  NULL,
	"FieldFloat"      FLOAT(24)                NULL,
	"FieldDouble"     FLOAT(53)                NULL,
	"FieldDateTime"   TIMESTAMP                NULL,
	"FieldDateTime2"  TIMESTAMP WITH TIME ZONE NULL,
	"FieldBinary"     BYTEA                    NULL,
	"FieldGuid"       UUID                     NULL,
	"FieldDecimal"    DECIMAL(24, 10)          NULL,
	"FieldDate"       DATE                     NULL,
	"FieldTime"       TIME(7)                  NULL,
	"FieldEnumString" VARCHAR(20)              NULL,
	"FieldEnumNumber" INT                      NULL
)
GO

CREATE OR REPLACE FUNCTION AddIssue792Record()
	RETURNS void AS
$BODY$
BEGIN
	INSERT INTO dbo.AllTypes(char20DataType) VALUES('issue792');
END;
$BODY$
	LANGUAGE PLPGSQL;
GO

CREATE OR REPLACE FUNCTION "TestTableFunctionSchema"() RETURNS SETOF "AllTypes"
AS $$ SELECT * FROM "AllTypes" $$ LANGUAGE SQL;
GO

CREATE OR REPLACE FUNCTION "TestFunctionParameters"(param1 INT, INOUT param2 INT, OUT param3 INT)
AS $$ SELECT param1, param2 $$ LANGUAGE SQL;
GO

CREATE OR REPLACE FUNCTION "TestTableFunction"(param1 INT) RETURNS TABLE(param2 INT)
AS $$ SELECT param1 UNION ALL SELECT param1 $$ LANGUAGE SQL;
GO

CREATE OR REPLACE FUNCTION "TestTableFunction1"(param1 INT, param2 INT) RETURNS TABLE(param3 INT, param4 INT)
AS $$ SELECT param1, 23 UNION ALL SELECT 333, param2 $$ LANGUAGE SQL;
GO

CREATE OR REPLACE FUNCTION "TestScalarFunction"(param INT) RETURNS VARCHAR(10)
AS $$ BEGIN RETURN 'done'; END $$ LANGUAGE PLPGSQL;
GO

CREATE OR REPLACE FUNCTION "TestSingleOutParameterFunction"(param1 INT, OUT param2 INT)
AS $$ BEGIN param2 := param1 + 123; END $$ LANGUAGE PLPGSQL;
GO

DROP AGGREGATE IF EXISTS test_avg(float8)
GO
CREATE AGGREGATE test_avg(float8)
(
	sfunc = float8_accum,
	stype = float8[],
	finalfunc = float8_avg,
	initcond = '{0,0,0}'
);

GO

CREATE OR REPLACE FUNCTION "bool"(param INT) RETURNS VARCHAR(20)
AS $$ BEGIN RETURN 'issue1295test'; END $$ LANGUAGE PLPGSQL;
GO
