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
	"ID"             int,
	"MoneyValue"     decimal(10,4),
	"DateTimeValue"  timestamp,
	"DateTimeValue2" timestamp  NULL,
	"BoolValue"      smallint,
	"GuidValue"      char(16) for bit DATA,
	"BinaryValue"    blob(5000) NULL,
	"SmallIntValue"  smallint,
	"IntValue"       int        NULL,
	"BigIntValue"    bigint     NULL
)
GO

DROP TABLE "TestIdentity"
GO

CREATE TABLE "TestIdentity" (
	"ID"   INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL
)
GO


DROP TABLE AllTypes
GO

CREATE TABLE AllTypes
(
	ID INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,

	bigintDataType           bigint                NULL,
	intDataType              int                   NULL,
	smallintDataType         smallint              NULL,
	decimalDataType          decimal(30)           NULL,
	decfloatDataType         decfloat              NULL,
	realDataType             real                  NULL,
	doubleDataType           double                NULL,

	charDataType             char(1)               NULL,
	varcharDataType          varchar(20)           NULL,
	clobDataType             clob                  NULL,
	dbclobDataType           dbclob(100)           NULL,

	binaryDataType           char(5) for bit data,
	varbinaryDataType        varchar(5) for bit data,
	blobDataType             blob(10)              NULL,
	graphicDataType          graphic(10)           NULL,

	dateDataType             date                  NULL,
	timeDataType             time                  NULL,
	timestampDataType        timestamp             NULL,

	xmlDataType              xml                   NULL
)
GO


INSERT INTO AllTypes (xmlDataType) VALUES (NULL)
GO

INSERT INTO AllTypes
(
	bigintDataType,
	intDataType,
	smallintDataType,
	decimalDataType,
	decfloatDataType,
	realDataType,
	doubleDataType,

	charDataType,
	varcharDataType,
	clobDataType,
	dbclobDataType,

	binaryDataType,
	varbinaryDataType,
	blobDataType,
	graphicDataType,

	dateDataType,
	timeDataType,
	timestampDataType,

	xmlDataType
)
VALUES
(
	1000000,
	7777777,
	100,
	9999999,
	8888888,
	20.31,
	16.2,

	'1',
	'234',
	'55645',
	'6687',

	'123',
	'1234',
	Cast('234' as blob),
	'23',

	Cast('2012-12-12' as date),
	Cast('12:12:12' as time),
	Cast('2012-12-12 12:12:12.012' as timestamp),

	'<root><element strattr="strvalue" intattr="12345"/></root>'
)
GO
