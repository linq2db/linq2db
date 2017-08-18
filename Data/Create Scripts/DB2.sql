DROP TABLE "Doctor"
GO

DROP TABLE "Patient"
GO

DROP TABLE "Person"
GO

DROP TABLE "InheritanceParent"
GO

CREATE TABLE "InheritanceParent"
(
	"InheritanceParentId" INTEGER       PRIMARY KEY NOT NULL,
	"TypeDiscriminator"   INTEGER                       NULL,
	"Name"                VARCHAR(50)                   NULL
)
GO

DROP TABLE "InheritanceChild"
GO

CREATE TABLE "InheritanceChild"
(
	"InheritanceChildId"  INTEGER      PRIMARY KEY NOT NULL,
	"InheritanceParentId" INTEGER                  NOT NULL,
	"TypeDiscriminator"   INTEGER                      NULL,
	"Name"                VARCHAR(50)                  NULL
)
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
INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Jane',   'Doe',       'F')
GO
INSERT INTO "Person" ("FirstName", "LastName", "Gender") VALUES ('Jürgen', 'König',     'M')
GO

-- Doctor Table Extension

CREATE TABLE "Doctor"
(
	"PersonID" INTEGER     PRIMARY KEY NOT NULL,
	"Taxonomy" VARCHAR(50) NOT NULL,

	FOREIGN KEY "FK_Doctor_Person" ("PersonID") REFERENCES "Person"
)
GO

INSERT INTO "Doctor" ("PersonID", "Taxonomy") VALUES (1, 'Psychiatry')
GO


-- FkTest
DROP TABLE MasterTable
GO

DROP TABLE SlaveTable
GO

CREATE TABLE MasterTable
(
	ID1 INTEGER NOT NULL,
	ID2 INTEGER NOT NULL,
	PRIMARY KEY (ID1,ID2)
)
GO

CREATE TABLE SlaveTable
(
	ID1    INTEGER NOT NULL,
	"ID 2222222222222222222222  22" INTEGER NOT NULL,
	"ID 2222222222222222"           INTEGER NOT NULL,
	FOREIGN KEY FK_SlaveTable_MasterTable ("ID 2222222222222222222222  22", ID1)
	REFERENCES MasterTable
)
GO

-- Patient Table Extension

CREATE TABLE "Patient"
(
	"PersonID"  INTEGER      PRIMARY KEY NOT NULL,
	"Diagnosis" VARCHAR(256) NOT NULL,

	FOREIGN KEY "FK_Patient_Person" ("PersonID") REFERENCES "Person"
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
	"DateTimeValue2" timestamp   NULL,
	"BoolValue"      smallint,
	"GuidValue"      char(16) for bit DATA,
	"BinaryValue"    blob(5000)  NULL,
	"SmallIntValue"  smallint,
	"IntValue"       int         NULL,
	"BigIntValue"    bigint      NULL,
	"StringValue"    VARCHAR(50) NULL
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
	char20DataType           char(20)              NULL,
	varcharDataType          varchar(20)           NULL,
	clobDataType             clob                  NULL,
	dbclobDataType           dbclob(100)           NULL,

	binaryDataType           char(5) for bit data,
	varbinaryDataType        varchar(5) for bit data,
	blobDataType             blob                  NULL,
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

CREATE OR REPLACE VIEW PersonView
AS
SELECT * FROM "Person"
GO

CREATE OR REPLACE Procedure Person_SelectByKey(in ID integer)
RESULT SETS 1
LANGUAGE SQL
BEGIN
	DECLARE C1 CURSOR FOR
		SELECT * FROM "Person" WHERE "PersonID" = ID;

	OPEN C1;
END
GO

DROP TABLE "TestMerge1"
GO
DROP TABLE "TestMerge2"
GO

CREATE TABLE "TestMerge1"
(
	"Id"       INTEGER            PRIMARY KEY NOT NULL,
	"Field1"   INTEGER                            NULL,
	"Field2"   INTEGER                            NULL,
	"Field3"   INTEGER                            NULL,
	"Field4"   INTEGER                            NULL,
	"Field5"   INTEGER                            NULL,

	"FieldInt64"      BIGINT                      NULL,
	"FieldBoolean"    SMALLINT                     NULL,
	"FieldString"     VARCHAR(20)                 NULL,
	"FieldNString"    NVARCHAR(20)                NULL,
	"FieldChar"       CHAR(1)                     NULL,
	"FieldNChar"      NCHAR(1)                    NULL,
	"FieldFloat"      REAL                        NULL,
	"FieldDouble"     DOUBLE                      NULL,
	"FieldDateTime"   TIMESTAMP(3)                NULL,
	"FieldBinary"     VARCHAR(20)  for bit data   ,
	"FieldGuid"       CHAR(16) FOR BIT DATA,
	"FieldDecimal"    DECIMAL(24, 10)             NULL,
	"FieldDate"       DATE                        NULL,
	"FieldTime"       TIME                        NULL,
	"FieldEnumString" VARCHAR(20)                 NULL,
	"FieldEnumNumber" INT                         NULL
)
GO
CREATE TABLE "TestMerge2"
(
	"Id"       INTEGER            PRIMARY KEY NOT NULL,
	"Field1"   INTEGER                            NULL,
	"Field2"   INTEGER                            NULL,
	"Field3"   INTEGER                            NULL,
	"Field4"   INTEGER                            NULL,
	"Field5"   INTEGER                            NULL,

	"FieldInt64"      BIGINT                      NULL,
	"FieldBoolean"    SMALLINT                     NULL,
	"FieldString"     VARCHAR(20)                 NULL,
	"FieldNString"    NVARCHAR(20)                NULL,
	"FieldChar"       CHAR(1)                     NULL,
	"FieldNChar"      NCHAR(1)                    NULL,
	"FieldFloat"      REAL                        NULL,
	"FieldDouble"     DOUBLE                      NULL,
	"FieldDateTime"   TIMESTAMP(3)                NULL,
	"FieldBinary"     VARCHAR(20)  for bit data   ,
	"FieldGuid"       CHAR(16) FOR BIT DATA,
	"FieldDecimal"    DECIMAL(24, 10)             NULL,
	"FieldDate"       DATE                        NULL,
	"FieldTime"       TIME                        NULL,
	"FieldEnumString" VARCHAR(20)                 NULL,
	"FieldEnumNumber" INT                         NULL
)
GO
