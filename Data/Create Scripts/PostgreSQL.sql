DROP TABLE "Doctor"
GO

DROP TABLE "Patient"
GO

DROP TABLE "Person"
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

INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('John',   'Pupkin',    'M')
GO
INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Tester', 'Testerson', 'M')
GO

-- Doctor Table Extension

CREATE TABLE "Doctor"
(
	"PersonID" INTEGER     NOT NULL,
	"Taxonomy" VARCHAR(50) NOT NULL
)
GO

INSERT INTO "Doctor" ("PersonID", "Taxonomy") VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE "Patient"
(
	"PersonID"  INTEGER      NOT NULL,
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


DROP TABLE "Parent"
GO
DROP TABLE "Child"
GO
DROP TABLE "GrandChild"
GO

CREATE TABLE "Parent"      ("ParentID" int, "Value1" int)
GO
CREATE TABLE "Child"       ("ParentID" int, "ChildID" int)
GO
CREATE TABLE "GrandChild"  ("ParentID" int, "ChildID" int, "GrandChildID" int)
GO


DROP TABLE "LinqDataTypes"
GO

CREATE TABLE "LinqDataTypes"
(
	"ID"             int,
	"MoneyValue"     decimal(10,4),
	"DateTimeValue"  timestamp,
	"DateTimeValue2" timestamp,
	"BoolValue"      boolean,
	"GuidValue"      uuid,
	"BinaryValue"    bytea,
	"SmallIntValue"  smallint,
	"IntValue"       int NULL,
	"BigIntValue"    bigint NULL
)
GO


DROP TABLE entity
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


DROP TABLE "SequenceTest1"
GO

DROP TABLE "SequenceTest2"
GO

DROP TABLE "SequenceTest3"
GO

DROP SEQUENCE SequenceTestSeq
GO

CREATE SEQUENCE SequenceTestSeq INCREMENT 1 START 1
GO

DROP SEQUENCE "SequenceTest2_ID_seq"
GO

CREATE SEQUENCE "SequenceTest2_ID_seq" INCREMENT 1 START 1
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
