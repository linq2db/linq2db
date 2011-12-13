
DROP TABLE Doctor
GO
DROP TABLE Patient
GO

-- Person Table

DROP TABLE Person
GO

CREATE TABLE Person
(
	PersonID   int         AUTO_INCREMENT NOT NULL,
	FirstName  varchar(50) NOT NULL,
	LastName   varchar(50) NOT NULL,
	MiddleName varchar(50)     NULL,
	Gender     char(1)     NOT NULL,
	CONSTRAINT PK_Person PRIMARY KEY CLUSTERED (PersonID)
)
GO

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M')
GO
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M')
GO

-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID int         NOT NULL,
	Taxonomy varchar(50) NOT NULL,
	CONSTRAINT PK_Doctor        PRIMARY KEY CLUSTERED (PersonID),
	CONSTRAINT FK_Doctor_Person FOREIGN KEY (PersonID)
		REFERENCES Person(PersonID)
)
GO

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry')
GO

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int          NOT NULL,
	Diagnosis varchar(256) NOT NULL,
	CONSTRAINT PK_Patient        PRIMARY KEY CLUSTERED (PersonID),
	CONSTRAINT FK_Patient_Person FOREIGN KEY (PersonID)
		REFERENCES Person (PersonID)
)
GO

INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution')
GO


-- GetPersonById

DROP Procedure GetPersonById
GO

CREATE Procedure GetPersonById(_ID INT)
BEGIN

	SELECT * FROM Person WHERE PersonID = _ID;

END
GO

-- GetPersonByName

DROP Procedure GetPersonByName
GO

CREATE Procedure GetPersonByName
(
	_firstName varchar(50),
	_lastName  varchar(50)
)
BEGIN

	SELECT * FROM Person WHERE FirstName = _firstName AND LastName = _lastName;

END
GO

-- Person_SelectByKey

DROP Procedure Person_SelectByKey
GO

CREATE Procedure Person_SelectByKey(id int)
BEGIN

    SELECT * FROM Person WHERE PersonID = id;

END
GO

-- Person_SelectAll

DROP Procedure Person_SelectAll
GO

CREATE Procedure Person_SelectAll()
BEGIN

	SELECT * FROM Person;

END
GO

-- Person_SelectByName

DROP Procedure Person_SelectByName
GO

CREATE Procedure Person_SelectByName
(
	firstName varchar(50),
	lastName  varchar(50)
)
BEGIN

	SELECT
		*
	FROM
		Person
	WHERE
		FirstName = firstName AND LastName = lastName;

END
GO

-- Person_SelectListByName

DROP Procedure Person_SelectListByName
GO

CREATE Procedure Person_SelectListByName
(
	firstName varchar(50),
	lastName  varchar(50)
)
BEGIN

	SELECT
		*
	FROM
		Person
	WHERE
		FirstName like firstName AND LastName like lastName;

END
GO

-- Person_Insert

DROP Procedure Person_Insert
GO

CREATE Procedure Person_Insert
(
	FirstName  varchar(50),
	LastName   varchar(50),
	MiddleName varchar(50),
	Gender     char(1)
)
BEGIN

	INSERT INTO Person
		(LastName, FirstName, MiddleName, Gender)
	VALUES
		(LastName, FirstName, MiddleName, Gender);

	SELECT LAST_INSERT_ID() AS PersonID;

END
GO

-- Person_Insert_OutputParameter

DROP Procedure Person_Insert_OutputParameter
GO

CREATE Procedure Person_Insert_OutputParameter
(
	FirstName  varchar(50),
	LastName   varchar(50),
	MiddleName varchar(50),
	Gender     char(1),
	OUT PersonID int
)
BEGIN

	INSERT INTO Person
		(LastName, FirstName, MiddleName, Gender)
	VALUES
		(LastName, FirstName, MiddleName, Gender);

	SET PersonID = LAST_INSERT_ID();

END
GO

-- Person_Update

DROP Procedure Person_Update
GO

CREATE Procedure Person_Update
(
	PersonID   int,
	FirstName  varchar(50),
	LastName   varchar(50),
	MiddleName varchar(50),
	Gender     char(1)
)
BEGIN

	UPDATE
		Person
	SET
		LastName   = LastName,
		FirstName  = FirstName,
		MiddleName = MiddleName,
		Gender     = Gender
	WHERE
		PersonID = PersonID;

END
GO

-- Person_Delete

DROP Procedure Person_Delete
GO

CREATE Procedure Person_Delete
(
	PersonID int
)
BEGIN

	DELETE FROM Person WHERE PersonID = PersonID;

END
GO

-- Patient_SelectAll

DROP Procedure Patient_SelectAll
GO

CREATE Procedure Patient_SelectAll()
BEGIN

	SELECT
		Person.*, Patient.Diagnosis
	FROM
		Patient, Person
	WHERE
		Patient.PersonID = Person.PersonID;

END
GO

-- Patient_SelectByName

DROP Procedure Patient_SelectByName
GO

CREATE Procedure Patient_SelectByName
(
	firstName varchar(50),
	lastName  varchar(50)
)
BEGIN

	SELECT
		Person.*, Patient.Diagnosis
	FROM
		Patient, Person
	WHERE
		Patient.PersonID = Person.PersonID
		AND FirstName = firstName AND LastName = lastName;

END
GO

-- BinaryData Table

DROP TABLE BinaryData
GO

CREATE TABLE BinaryData
(
	BinaryDataID int             AUTO_INCREMENT NOT NULL,
	Stamp        timestamp       NOT NULL,
	Data         varbinary(1024) NOT NULL,
	CONSTRAINT PK_BinaryData PRIMARY KEY CLUSTERED (BinaryDataID)
)
GO

-- OutRefTest

DROP Procedure OutRefTest
GO

CREATE Procedure OutRefTest
(
	    ID             int,
	OUT outputID       int,
	OUT inputOutputID  int,
	    str            varchar(50),
	OUT outputStr      varchar(50),
	OUT inputOutputStr varchar(50)
)
BEGIN

	SET outputID       = ID;
	SET inputOutputID  = ID + inputOutputID;
	SET outputStr      = str;
	SET inputOutputStr = str + inputOutputStr;

END
GO

-- OutRefEnumTest

DROP Procedure OutRefEnumTest
GO

CREATE Procedure OutRefEnumTest
(
	    str            varchar(50),
	OUT outputStr      varchar(50),
	OUT inputOutputStr varchar(50)
)
BEGIN

	SET outputStr      = str;
	SET inputOutputStr = str + inputOutputStr;

END
GO

-- ExecuteScalarTest

DROP Procedure Scalar_DataReader
GO

CREATE Procedure Scalar_DataReader()
BEGIN

	SELECT
		12345   AS intField,
		'54321' AS stringField;

END
GO

DROP Procedure Scalar_OutputParameter
GO

CREATE Procedure Scalar_OutputParameter
(
	OUT outputInt    int,
	OUT outputString varchar(50)
)
BEGIN

	SET outputInt    = 12345;
	SET outputString = '54321';

END
GO

-- Data Types test

DROP TABLE DataTypeTest
GO

CREATE TABLE DataTypeTest
(
	DataTypeID      int              AUTO_INCREMENT NOT NULL,
	Binary_         binary(50)       NULL,
	Boolean_        bit              NOT NULL,
	Byte_           tinyint          NULL,
	Bytes_          varbinary(50)    NULL,
	Char_           char(1)          NULL,
	DateTime_       datetime         NULL,
	Decimal_        decimal(20,2)    NULL,
	Double_         float            NULL,
	Guid_           varbinary(50)    NULL,
	Int16_          smallint         NULL,
	Int32_          int              NULL,
	Int64_          bigint           NULL,
	Money_          decimal(20,4)    NULL,
	SByte_          tinyint          NULL,
	Single_         real             NULL,
	Stream_         varbinary(50)    NULL,
	String_         varchar(50)      NULL,
	UInt16_         smallint         NULL,
	UInt32_         int              NULL,
	UInt64_         bigint           NULL,
	Xml_            varchar(1000)    NULL,
	CONSTRAINT PK_DataType PRIMARY KEY CLUSTERED (DataTypeID)
)
GO

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  Char_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,     Xml_)
VALUES
	(   NULL,        0,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL)
GO

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  Char_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,
	 Xml_)
VALUES
	( UUID(),       1,     127,  UUID(),     'B', CurDate(), 12345.67,
	1234.567,  UUID(),   32767,   32768, 1000000,   12.3456,      127,
	1234.123,  UUID(), 'string',  32767,   32768, 200000000,
	'<root><element strattr="strvalue" intattr="12345"/></root>')
GO



DROP TABLE Parent
GO
DROP TABLE Child
GO
DROP TABLE GrandChild
GO

CREATE TABLE Parent     (ParentID int, Value1 int)
GO
CREATE TABLE Child      (ParentID int, ChildID int)
GO
CREATE TABLE GrandChild (ParentID int, ChildID int, GrandChildID int)
GO


DROP TABLE LinqDataTypes
GO

CREATE TABLE LinqDataTypes
(
	ID            int,
	MoneyValue    decimal(10,4),
	DateTimeValue datetime,
	BoolValue     boolean,
	GuidValue     char(36),
	BinaryValue   varbinary(5000),
	SmallIntValue smallint,
	IntValue      int NULL,
	BigIntValue   bigint NULL
)
GO
