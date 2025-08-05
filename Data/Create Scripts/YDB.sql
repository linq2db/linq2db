DROP TABLE IF EXISTS Parent;
GO
DROP TABLE IF EXISTS Child;
GO
DROP TABLE IF EXISTS GrandChild;
GO
DROP TABLE IF EXISTS InheritanceParent;
GO
DROP TABLE IF EXISTS InheritanceChild;
GO
DROP TABLE IF EXISTS Doctor;
GO
DROP TABLE IF EXISTS Patient;
GO
DROP TABLE IF EXISTS Person;
GO
DROP TABLE IF EXISTS LinqDataTypes;
GO
DROP TABLE IF EXISTS AllTypes;
GO
DROP TABLE IF EXISTS CollatedTable;
GO
DROP TABLE IF EXISTS TestMerge1
GO
DROP TABLE IF EXISTS TestMerge2;
GO
CREATE TABLE Parent      (ParentID Int32 NOT NULL, Value1 Int32, PRIMARY KEY (ParentID));
GO
CREATE TABLE Child       (ParentID Int32 NOT NULL, ChildID Int32 NOT NULL, PRIMARY KEY (ChildID));
GO
CREATE TABLE GrandChild  (ParentID Int32, ChildID Int32, GrandChildID Int32, PRIMARY KEY (GrandChildID));
GO
CREATE TABLE LinqDataTypes
(
	ID             Int32 NOT NULL,
	MoneyValue     Decimal(4, 2),
	DateTimeValue  Timestamp,
	DateTimeValue2 Timestamp,
	BoolValue      Bool,
	GuidValue      UUID,
	BinaryValue    String,
	SmallIntValue  Int16,
	IntValue       Int32,
	BigIntValue    Int64,
	StringValue    String,
	PRIMARY KEY (ID)
);
GO
CREATE TABLE InheritanceParent
(
	InheritanceParentId Int32 NOT NULL,
	TypeDiscriminator   Int32,
	Name                String,
	PRIMARY KEY (InheritanceParentId)
);
GO
CREATE TABLE InheritanceChild
(
	InheritanceChildId  Int32 NOT NULL,
	InheritanceParentId Int32 NOT NULL,
	TypeDiscriminator   Int32,
	Name                String,
	PRIMARY KEY (InheritanceChildId)
);
GO
CREATE TABLE Person
(
	PersonID   Int32 NOT NULL,
	FirstName  String NOT NULL,
	LastName   String NOT NULL,
	MiddleName String,
	Gender     String NOT NULL,
	PRIMARY KEY (PersonID)
);
GO
INSERT INTO Person (PersonID, FirstName, LastName, MiddleName, Gender)
	VALUES
	(1, 'John',   'Pupkin',    NULL, 'M'),
	(2, 'Tester', 'Testerson', NULL, 'M'),
	(3, 'Jane',   'Doe',       NULL, 'F'),
	(4, 'Jürgen', 'König',     'Ko', 'M');
GO
CREATE TABLE Doctor
(
	PersonID Int32 NOT NULL,
	Taxonomy String NOT NULL,
	PRIMARY KEY (PersonID)
);
GO
INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');
GO
CREATE TABLE Patient
(
	PersonID  Int32 NOT NULL,
	Diagnosis String NOT NULL,
	PRIMARY KEY (PersonID)
);
GO
INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs\' Delirium of Persecution');
GO

CREATE TABLE AllTypes
(
	ID                       Int32 NOT NULL,

	intDataType              Int32,
	smallintDataType         Int16,

	floatDataType            Float,
	doubleDataType           Double,


	ncharDataType            String,
	char20DataType           String,
	varcharDataType          String,
	charDataType             String,
	bitDataType              Bool,

	PRIMARY KEY (ID)
);
GO

INSERT INTO AllTypes
(
	ID,
	intDataType, smallintDataType,
	floatDataType, doubleDataType,
	ncharDataType, char20DataType, varcharDataType, charDataType
)
VALUES(1,
		CAST(NULL AS Int32), CAST(NULL AS Int16),
		CAST(NULL AS Float), CAST(NULL AS Double),
		CAST(NULL AS String), CAST(NULL AS String), CAST(NULL AS String), CAST(NULL AS String));
GO
INSERT INTO AllTypes
(
	ID,
	intDataType, smallintDataType,
	floatDataType, doubleDataType,
	ncharDataType, char20DataType, varcharDataType, charDataType
)
VALUES(2,
		7777777, 25555,
		20.31f, 16.2,
		'23233', 'тест', '234', '1');
GO
CREATE TABLE CollatedTable
(
	Id              Int32 NOT NULL,
	CaseSensitive   String,
	CaseInsensitive String,
	PRIMARY KEY (Id)
);
GO
CREATE TABLE TestMerge1
(
	Id              Int32 NOT NULL,
	Field1          Int32,
	Field2          Int32,
	Field3          Int32,
	Field4          Int32,
	Field5          Int32,

	FieldInt64      Int64,
	FieldBoolean    Bool,
	FieldString     String,
	FieldNString    String,
	FieldChar       String,
	FieldNChar      String,
	FieldFloat      Float,
	FieldDouble     Double,
	FieldDateTime   Timestamp,
	FieldDateTime2  Timestamp,
	FieldBinary     String,
	FieldGuid       UUID,
	FieldDecimal    Decimal(10, 0),
	FieldDate       Date,
	FieldTime       Int64,
	FieldEnumString String,
	FieldEnumNumber Int32,
	PRIMARY KEY (Id)
);
GO

CREATE TABLE TestMerge2
(
	Id              Int32 NOT NULL,
	Field1          Int32,
	Field2          Int32,
	Field3          Int32,
	Field4          Int32,
	Field5          Int32,

	FieldInt64      Int64,
	FieldBoolean    Bool,
	FieldString     String,
	FieldNString    String,
	FieldChar       String,
	FieldNChar      String,
	FieldFloat      Float,
	FieldDouble     Double,
	FieldDateTime   Timestamp,
	FieldDateTime2  Timestamp,
	FieldBinary     String,
	FieldGuid       UUID,
	FieldDecimal    Decimal(10, 0),
	FieldDate       Date,
	FieldTime       Int64,
	FieldEnumString String,
	FieldEnumNumber Int32,
	PRIMARY KEY (Id)
);
GO
