DROP TABLE TEMP/Doctor;
DROP TABLE TEMP/Patient;
DROP TABLE TEMP/Person;

CREATE TABLE TEMP/Person( 
	PersonID   INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
	FirstName  VARCHAR(50) NOT NULL,
	LastName   VARCHAR(50) NOT NULL,
	MiddleName VARCHAR(50),
	Gender     CHAR(1)     NOT NULL
);

INSERT INTO TEMP/Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M');
INSERT INTO TEMP/Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M');

-- Doctor Table Extension

CREATE TABLE TEMP/Doctor(
	PersonID INTEGER     NOT NULL,
	Taxonomy VARCHAR(50) NOT NULL,
	FOREIGN KEY FK_Doctor_Person(PersonID) REFERENCES TEMP/Person
);

INSERT INTO TEMP/Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');

-- FkTest
DROP TABLE TEMP/MasterTable;
DROP TABLE TEMP/SlaveTable;

CREATE TABLE TEMP/MasterTable(
	ID1 INTEGER NOT NULL,
	ID2 INTEGER NOT NULL,
	PRIMARY KEY (ID1,ID2)
);

CREATE TABLE TEMP/SlaveTable(
	ID1    INTEGER NOT NULL,
	"ID 2222222222222222222222  22" INTEGER NOT NULL,
	"ID 2222222222222222"           INTEGER NOT NULL,
	FOREIGN KEY FK_SlaveTable_MasterTable ("ID 2222222222222222222222  22", ID1)
	REFERENCES TEMP/MasterTable
);

-- Patient Table Extension

CREATE TABLE TEMP/Patient(
	PersonID  INTEGER      NOT NULL,
	Diagnosis VARCHAR(256) NOT NULL
);

INSERT INTO TEMP/Patient(PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution');

DROP TABLE TEMP/Parent;
DROP TABLE TEMP/Child;
DROP TABLE TEMP/GrandChild;

CREATE TABLE TEMP/Parent      (ParentID int, Value1 int);
CREATE TABLE TEMP/Child       (ParentID int, ChildID int);
CREATE TABLE TEMP/GrandChild  (ParentID int, ChildID int, GrandChildID int);

DROP TABLE TEMP/LinqDataTypes;

CREATE TABLE TEMP/LinqDataTypes(
	ID             int,
	MoneyValue     decimal(10,4),
	DateTimeValue  timestamp,
	DateTimeValue2 timestamp  Default NULL,
	BoolValue      smallint,
	GuidValue      char(16) for bit DATA,
	BinaryValue    blob(5000) Default NULL,
	SmallIntValue  smallint,
	IntValue       int        Default NULL,
	BigIntValue    bigint     Default NULL
);

DROP TABLE TEMP/TestIdentity;

CREATE TABLE TEMP/TestIdentity (
	ID   INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL
);

DROP TABLE TEMP/AllTypes;

CREATE TABLE TEMP/AllTypes(
	  ID INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL

	, bigintDataType           bigint                  Default NULL
	, binaryDataType           binary(20)              Default NULL
	, blobDataType             blob                    Default NULL
	, charDataType             char(1)                 Default NULL
	, CharForBitDataType       char(5) for bit data    Default NULL
	, clobDataType             clob                    Default NULL
	, dataLinkDataType         dataLink                Default NULL
	, dateDataType             date                    Default NULL
	, dbclobDataType           dbclob(100)             Default NULL
	, decfloat16DataType       float(5)                Default NULL
	, decfloat34DataType       float(50)               Default NULL
	, decimalDataType          decimal(30)             Default NULL
	, doubleDataType           double                  Default NULL
	, graphicDataType          graphic(10)             Default NULL
  , intDataType              int                     Default NULL
	, numericDataType          numeric                 Default NULL
	, realDataType             real                    Default NULL
	, rowIdDataType            rowId                              
	, smallintDataType         smallint                Default NULL
	, timeDataType             time                    Default NULL
	, timestampDataType        timestamp               Default NULL
	, varbinaryDataType        varbinary(20)           Default NULL
	, varcharDataType          varchar(20)             Default NULL
	, varCharForBitDataType    varchar(5) for bit data Default NULL
	, varGraphicDataType       vargraphic(10)          Default NULL
--, xmlDataType              xml(20)                 Default NULL
);


INSERT INTO TEMP/AllTypes (xmlDataType) VALUES (NULL);

INSERT INTO TEMP/AllTypes(
	  bigintDataType           
	, binaryDataType           
	, blobDataType             
	, charDataType             
	, CharForBitDataType       
	, clobDataType             
	, dataLinkDataType         
	, dateDataType             
	, dbclobDataType           
	, decfloat16DataType       
	, decfloat34DataType       
	, decimalDataType          
	, doubleDataType           
	, graphicDataType          
        , intDataType              
	, numericDataType          
	, realDataType             
	, rowIdDataType            
	, smallintDataType         
	, timeDataType             
	, timestampDataType        
	, varbinaryDataType        
	, varcharDataType          
	, varCharForBitDataType    
	, varGraphicDataType       
--	, xmlDataType              
) VALUES (
	  1000000                    --bigIntDataType         
	, Cast('123' as binary)      --binaryDataType           
	, Cast('234' as blob)        --blobDataType             
	, 'Y'                        --charDataType             
	, '123'                      --CharForBitDataType       
	, Cast('567' as clob)        --clobDataType             
	, DEFAULT                    --dataLinkDataType         
	, CURRENT_DATE               --dateDataType             
	, Cast('890' as dbclob)      --dbclobDataType           
	, 888.456                    --decfloat16DataType       
	, 777.987                    --decfloat34DataType       
	, 666.987                    --decimalDataType          
	, 555.987                    --doubleDataType           
	, Cast('graphic' as graphic) --graphicDataType          
  , 444444                     --intDataType              
	, 333.987                    --numericDataType          
	, 222.987                    --realDataType             
	, DEFAULT                    --rowIdDataType            
	, 100                        --smallintDataType         
	, CURRENT_TIME               --timeDataType             
	, CURRENT_TIMESTAMP          --timestampDataType        
	, Cast('456' as binary)      --varbinaryDataType        
	, 'var-char'                 --varcharDataType          
	, 'vcfb'                     --varCharForBitDataType    
	, DEFAULT                    --varGraphicDataType
  
--	, '<root><element strattr="strvalue" intattr="12345"/></root>' --xmlDataType  
);

DROP VIEW TEMP/PersonView;

CREATE VIEW TEMP/PersonView
AS
SELECT * FROM TEMP/Person;

DROP Procedure TEMP/Person_SelectByKey;

CREATE Procedure TEMP/Person_SelectByKey(in ID integer)
RESULT SETS 1
LANGUAGE SQL
BEGIN
	DECLARE C1 CURSOR FOR
		SELECT * FROM TEMP/Person WHERE PersonID = ID;

	OPEN C1;
END;
