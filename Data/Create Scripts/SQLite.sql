--
-- Helper table
--
DROP TABLE IF EXISTS Dual;
CREATE TABLE Dual (Dummy  VARCHAR(10));
INSERT INTO  Dual (Dummy) VALUES ('X');

DROP TABLE IF EXISTS InheritanceParent;
CREATE TABLE InheritanceParent
(
	InheritanceParentId integer      NOT NULL CONSTRAINT PK_InheritanceParent,
	TypeDiscriminator   integer          NULL,
	Name                nvarchar(50)     NULL
);

DROP TABLE IF EXISTS InheritanceChild;
CREATE TABLE InheritanceChild
(
	InheritanceChildId  integer      NOT NULL CONSTRAINT PK_InheritanceChild,
	InheritanceParentId integer      NOT NULL,
	TypeDiscriminator   integer          NULL,
	Name                nvarchar(50)     NULL
);
--
-- Person Table
--
DROP TABLE IF EXISTS Person;
CREATE TABLE Person
(
	PersonID   integer      NOT NULL CONSTRAINT PK_Person PRIMARY KEY AUTOINCREMENT,
	FirstName  nvarchar(50) NOT NULL,
	LastName   nvarchar(50) NOT NULL,
	MiddleName nvarchar(50)     NULL,
	Gender     char(1)      NOT NULL CONSTRAINT CK_Person_Gender CHECK (Gender in ('M', 'F', 'U', 'O'))
);

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M');
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M');
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jane',   'Doe',       'F');
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Jürgen', 'König',     'M');

--
-- Doctor Table Extension
--
DROP TABLE IF EXISTS Doctor;
CREATE TABLE Doctor
(
	PersonID integer      NOT NULL CONSTRAINT PK_Doctor PRIMARY KEY,
	Taxonomy nvarchar(50) NOT NULL,
	CONSTRAINT FK_Doctor_Person FOREIGN KEY(PersonID) REFERENCES Person(PersonID)
);

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');

--
-- Patient Table Extension
--
DROP TABLE IF EXISTS Patient;
CREATE TABLE Patient
(
	PersonID  integer       NOT NULL CONSTRAINT PK_Patient PRIMARY KEY,
	Diagnosis nvarchar(256) NOT NULL,

	CONSTRAINT FK_Patient_Person FOREIGN KEY(PersonID) REFERENCES Person(PersonID)
);
INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution');

--
-- Babylon test
--
DROP TABLE IF EXISTS Parent;
DROP TABLE IF EXISTS Child;
DROP TABLE IF EXISTS GrandChild;

CREATE TABLE Parent      (ParentID int, Value1 int);
CREATE TABLE Child       (ParentID int, ChildID int);
CREATE TABLE GrandChild  (ParentID int, ChildID int, GrandChildID int);

DROP TABLE IF EXISTS LinqDataTypes;
CREATE TABLE LinqDataTypes
(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  datetime,
	DateTimeValue2 datetime2,
	BoolValue      boolean,
	GuidValue      uniqueidentifier,
	BinaryValue    binary(5000) NULL,
	SmallIntValue  smallint,
	IntValue       int          NULL,
	BigIntValue    bigint       NULL,
	StringValue    nvarchar(50) NULL
);


DROP TABLE IF EXISTS TestIdentity
GO

CREATE TABLE TestIdentity (
	ID integer NOT NULL CONSTRAINT PK_TestIdentity PRIMARY KEY AUTOINCREMENT
)
GO

DROP TABLE IF EXISTS AllTypes
GO

CREATE TABLE AllTypes
(
	ID                       integer          NOT NULL CONSTRAINT PK_AllTypes PRIMARY KEY AUTOINCREMENT,

	bigintDataType           bigint           NULL,
	numericDataType          numeric          NULL,
	bitDataType              bit              NULL,
	smallintDataType         smallint         NULL,
	decimalDataType          decimal          NULL,
	intDataType              int              NULL,
	tinyintDataType          tinyint          NULL,
	moneyDataType            money            NULL,
	floatDataType            float            NULL,
	realDataType             real             NULL,

	datetimeDataType         datetime         NULL,

	charDataType             char(1)          NULL,
	char20DataType           char(20)         NULL,
	varcharDataType          varchar(20)      NULL,
	textDataType             text             NULL,
	ncharDataType            nchar(20)        NULL,
	nvarcharDataType         nvarchar(20)     NULL,
	ntextDataType            ntext            NULL,

	binaryDataType           binary           NULL,
	varbinaryDataType        varbinary        NULL,
	imageDataType            image            NULL,

	uniqueidentifierDataType uniqueidentifier NULL,
	objectDataType           Object           NULL
)
GO

INSERT INTO AllTypes
(
	bigintDataType, numericDataType, bitDataType, smallintDataType, decimalDataType,
	intDataType, tinyintDataType, moneyDataType, floatDataType, realDataType, 
	datetimeDataType,
	charDataType, varcharDataType, textDataType, ncharDataType, nvarcharDataType, ntextDataType,
	objectDataType
)
SELECT
	     NULL,      NULL,  NULL,    NULL,    NULL,   NULL,  NULL,   NULL,  NULL, NULL,
	     NULL,
	     NULL,      NULL,  NULL,    NULL,    NULL,   NULL,
	     NULL
UNION ALL
SELECT
	 1000000,    9999999,     1,   25555, 2222222, 7777777,  100, 100000, 20.31, 16.2,
	'2012-12-12 12:12:12',
	      '1',     '234', '567', '23233',  '3323',  '111',
	       10

GO


--
-- Demonstration Tables for Issue #784
--

-- Parent table
DROP TABLE IF EXISTS FKTestCompany
GO

CREATE TABLE FKTestCompany
(
    CompanyID    integer      NOT NULL PRIMARY KEY,
    Name         nvarchar(50) NOT NULL
)
GO

-- Child table
DROP TABLE IF EXISTS FKTestDepartment
GO

CREATE TABLE FKTestDepartment
(
    Company      integer      NOT NULL,
    DepartmentID integer      NOT NULL,
    Name         nvarchar(50) NOT NULL,
    PRIMARY KEY(Company, DepartmentID),
    -- Test: the foreign key targets the parent table without a column 
    -- reference.  This should automatically match against the primary key
    -- of the target table.
    CONSTRAINT FK_Department_Company FOREIGN KEY(Company) REFERENCES FKTestCompany ON DELETE CASCADE
)
GO

-- Second-level child table, alternate semantics
DROP TABLE IF EXISTS FKTestPosition
GO

CREATE TABLE FKTestPosition
(
    Company      integer      NOT NULL,
    Department   integer      NOT NULL,
    PositionID   integer      NOT NULL,
    Name         nvarchar(50) NOT NULL,
    PRIMARY KEY(Company, Department, PositionID),
    -- Test: one level deeper, this should link to both fields in the 
    -- primary key of the FKTestDepartment table
    CONSTRAINT FK_Position_Department FOREIGN KEY(Company, Department) REFERENCES FKTestDepartment ON DELETE CASCADE
	-- A simpler foreign key for the above would be:
	--    FOREIGN KEY(Department) REFERENCES FKTestDepartment(DepartmentID) ON DELETE CASCADE
)
GO

