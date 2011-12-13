DROP TABLE "Doctor"
GO

DROP TABLE "Patient"
GO

DROP TABLE "Person"
GO

CREATE TABLE "Person"
( 
	"PersonID"   INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
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
	"ID"            int,
	"MoneyValue"    decimal(10,4),
	"DateTimeValue" timestamp,
	"BoolValue"     smallint,
	"GuidValue"     char(16) for bit DATA,
	"BinaryValue"   blob(5000),
	"SmallIntValue" smallint,
	"IntValue"      int NULL,
	"BigIntValue"   bigint NULL
)
GO
