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
	ID             SERIAL NOT NULL,
	MoneyValue     Decimal(6, 2),
	DateTimeValue  Timestamp,
	DateTimeValue2 Timestamp,
	BoolValue      Bool,
	GuidValue      UUID,
	BinaryValue    Bytes,
	SmallIntValue  Int16,
	IntValue       Int32,
	BigIntValue    Int64,
	StringValue    Text,
	PRIMARY KEY (ID)
);
GO
CREATE TABLE InheritanceParent
(
	InheritanceParentId Int32 NOT NULL,
	TypeDiscriminator   Int32,
	Name                Text,
	PRIMARY KEY (InheritanceParentId)
);
GO
CREATE TABLE InheritanceChild
(
	InheritanceChildId  Int32 NOT NULL,
	InheritanceParentId Int32 NOT NULL,
	TypeDiscriminator   Int32,
	Name                Text,
	PRIMARY KEY (InheritanceChildId)
);
GO
CREATE TABLE Person
(
	PersonID   SERIAL NOT NULL,
	FirstName  Text NOT NULL,
	LastName   Text NOT NULL,
	MiddleName Text,
	Gender     Text NOT NULL,
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
	Taxonomy Text NOT NULL,
	PRIMARY KEY (PersonID)
);
GO
INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');
GO
CREATE TABLE Patient
(
	PersonID  Int32 NOT NULL,
	Diagnosis Text NOT NULL,
	PRIMARY KEY (PersonID)
);
GO
INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs\' Delirium of Persecution');
GO

CREATE TABLE AllTypes
(
	ID                       SERIAL NOT NULL,

	intDataType              Int32,
	smallintDataType         Int16,

	floatDataType            Float,
	doubleDataType           Double,


	ncharDataType            Bytes,
	char20DataType           Bytes,
	varcharDataType          Bytes,
	charDataType             Bytes,
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
		CAST(NULL AS Bytes), CAST(NULL AS Bytes), CAST(NULL AS Bytes), CAST(NULL AS Bytes));
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
	CaseSensitive   Bytes,
	CaseInsensitive Bytes,
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
	FieldString     Bytes,
	FieldNString    Bytes,
	FieldChar       Bytes,
	FieldNChar      Bytes,
	FieldFloat      Float,
	FieldDouble     Double,
	FieldDateTime   Timestamp,
	FieldDateTime2  Timestamp,
	FieldBinary     Bytes,
	FieldGuid       UUID,
	FieldDecimal    Decimal(10, 0),
	FieldDate       Date,
	FieldTime       Int64,
	FieldEnumString Bytes,
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
	FieldString     Bytes,
	FieldNString    Bytes,
	FieldChar       Bytes,
	FieldNChar      Bytes,
	FieldFloat      Float,
	FieldDouble     Double,
	FieldDateTime   Timestamp,
	FieldDateTime2  Timestamp,
	FieldBinary     Bytes,
	FieldGuid       UUID,
	FieldDecimal    Decimal(10, 0),
	FieldDate       Date,
	FieldTime       Int64,
	FieldEnumString Bytes,
	FieldEnumNumber Int32,
	PRIMARY KEY (Id)
);
GO
