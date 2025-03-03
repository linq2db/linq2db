﻿//---------------------------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated by T4Model template for T4 (https://github.com/linq2db/linq2db).
//    Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//---------------------------------------------------------------------------------------------------

#pragma warning disable 1573, 1591
#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Mapping;

namespace Default.DB2
{
	public partial class TestDataDB : LinqToDB.Data.DataConnection
	{
		#region Tables

		public ITable<ALLTYPE>           ALLTYPES            { get { return this.GetTable<ALLTYPE>(); } }
		public ITable<Child>             Children            { get { return this.GetTable<Child>(); } }
		public ITable<CollatedTable>     CollatedTables      { get { return this.GetTable<CollatedTable>(); } }
		public ITable<Doctor>            Doctors             { get { return this.GetTable<Doctor>(); } }
		public ITable<GrandChild>        GrandChildren       { get { return this.GetTable<GrandChild>(); } }
		public ITable<InheritanceChild>  InheritanceChildren { get { return this.GetTable<InheritanceChild>(); } }
		public ITable<InheritanceParent> InheritanceParents  { get { return this.GetTable<InheritanceParent>(); } }
		public ITable<KeepIdentityTest>  KeepIdentityTests   { get { return this.GetTable<KeepIdentityTest>(); } }
		public ITable<LinqDataType>      LinqDataTypes       { get { return this.GetTable<LinqDataType>(); } }
		public ITable<MASTERTABLE>       Mastertables        { get { return this.GetTable<MASTERTABLE>(); } }
		public ITable<Parent>            Parents             { get { return this.GetTable<Parent>(); } }
		public ITable<Patient>           Patients            { get { return this.GetTable<Patient>(); } }
		public ITable<Person>            People              { get { return this.GetTable<Person>(); } }
		public ITable<PERSONVIEW>        Personviews         { get { return this.GetTable<PERSONVIEW>(); } }
		public ITable<SLAVETABLE>        Slavetables         { get { return this.GetTable<SLAVETABLE>(); } }
		public ITable<TestIdentity>      TestIdentities      { get { return this.GetTable<TestIdentity>(); } }
		public ITable<TestMerge1>        TestMerge1          { get { return this.GetTable<TestMerge1>(); } }
		public ITable<TestMerge2>        TestMerge2          { get { return this.GetTable<TestMerge2>(); } }

		#endregion

		#region .ctor

		public TestDataDB()
		{
			InitDataContext();
			InitMappingSchema();
		}

		public TestDataDB(string configuration)
			: base(configuration)
		{
			InitDataContext();
			InitMappingSchema();
		}

		public TestDataDB(DataOptions options)
			: base(options)
		{
			InitDataContext();
			InitMappingSchema();
		}

		public TestDataDB(DataOptions<TestDataDB> options)
			: base(options.Options)
		{
			InitDataContext();
			InitMappingSchema();
		}

		partial void InitDataContext  ();
		partial void InitMappingSchema();

		#endregion

		#region Table Functions

		#region TestMODULE1TestTableFunction

		[Sql.TableFunction(Schema="DB2INST1", Package="TEST_MODULE1", Name="TEST_TABLE_FUNCTION")]
		public ITable<TestTableFUNCTIONResult> TestMODULE1TestTableFunction(int? i)
		{
			return this.TableFromExpression(() => TestMODULE1TestTableFunction(i));
		}

		public partial class TestTableFUNCTIONResult
		{
			public int? O { get; set; }
		}

		#endregion

		#region TestMODULE2TestTableFunction

		[Sql.TableFunction(Schema="DB2INST1", Package="TEST_MODULE2", Name="TEST_TABLE_FUNCTION")]
		public ITable<TestTableFUNCTIONResult0> TestMODULE2TestTableFunction(int? i)
		{
			return this.TableFromExpression(() => TestMODULE2TestTableFunction(i));
		}

		public partial class TestTableFUNCTIONResult0
		{
			public int? O { get; set; }
		}

		#endregion

		#region TestTableFunction

		[Sql.TableFunction(Schema="DB2INST1", Name="TEST_TABLE_FUNCTION")]
		public ITable<TestTableFUNCTIONResult1> TestTableFunction(int? i)
		{
			return this.TableFromExpression(() => TestTableFunction(i));
		}

		public partial class TestTableFUNCTIONResult1
		{
			public int? O { get; set; }
		}

		#endregion

		#endregion
	}

	[Table(Schema="DB2INST1", Name="ALLTYPES")]
	public partial class ALLTYPE
	{
		[PrimaryKey, Identity] public int       ID                { get; set; } // INTEGER
		[Column,     Nullable] public long?     BIGINTDATATYPE    { get; set; } // BIGINT
		[Column,     Nullable] public int?      INTDATATYPE       { get; set; } // INTEGER
		[Column,     Nullable] public short?    SMALLINTDATATYPE  { get; set; } // SMALLINT
		[Column,     Nullable] public decimal?  DECIMALDATATYPE   { get; set; } // DECIMAL
		[Column,     Nullable] public decimal?  DECFLOATDATATYPE  { get; set; } // DECFLOAT(16)
		[Column,     Nullable] public float?    REALDATATYPE      { get; set; } // REAL
		[Column,     Nullable] public double?   DOUBLEDATATYPE    { get; set; } // DOUBLE
		[Column,     Nullable] public char?     CHARDATATYPE      { get; set; } // CHARACTER(1)
		[Column,     Nullable] public string?   CHAR20DATATYPE    { get; set; } // CHARACTER(20)
		[Column,     Nullable] public string?   VARCHARDATATYPE   { get; set; } // VARCHAR(20)
		[Column,     Nullable] public string?   CLOBDATATYPE      { get; set; } // CLOB(1048576)
		[Column,     Nullable] public string?   DBCLOBDATATYPE    { get; set; } // DBCLOB(100)
		[Column,     Nullable] public byte[]?   BINARYDATATYPE    { get; set; } // CHAR (5) FOR BIT DATA
		[Column,     Nullable] public byte[]?   VARBINARYDATATYPE { get; set; } // VARCHAR (5) FOR BIT DATA
		[Column,     Nullable] public byte[]?   BLOBDATATYPE      { get; set; } // BLOB(1048576)
		[Column,     Nullable] public string?   GRAPHICDATATYPE   { get; set; } // GRAPHIC(10)
		[Column,     Nullable] public DateTime? DATEDATATYPE      { get; set; } // DATE
		[Column,     Nullable] public TimeSpan? TIMEDATATYPE      { get; set; } // TIME
		[Column,     Nullable] public DateTime? TIMESTAMPDATATYPE { get; set; } // TIMESTAMP
		[Column,     Nullable] public string?   XMLDATATYPE       { get; set; } // XML
	}

	[Table(Schema="DB2INST1", Name="Child")]
	public partial class Child
	{
		[Column, Nullable] public int? ParentID { get; set; } // INTEGER
		[Column, Nullable] public int? ChildID  { get; set; } // INTEGER
	}

	[Table(Schema="DB2INST1", Name="CollatedTable")]
	public partial class CollatedTable
	{
		[Column, NotNull] public int    Id              { get; set; } // INTEGER
		[Column, NotNull] public string CaseSensitive   { get; set; } = null!; // VARCHAR(80)
		[Column, NotNull] public string CaseInsensitive { get; set; } = null!; // VARCHAR(80)
	}

	[Table(Schema="DB2INST1", Name="Doctor")]
	public partial class Doctor
	{
		[PrimaryKey, NotNull] public int    PersonID { get; set; } // INTEGER
		[Column,     NotNull] public string Taxonomy { get; set; } = null!; // VARCHAR(50)

		#region Associations

		/// <summary>
		/// FK_Doctor_Person (DB2INST1.Person)
		/// </summary>
		[Association(ThisKey=nameof(PersonID), OtherKey=nameof(Default.DB2.Person.PersonID), CanBeNull=false)]
		public Person Person { get; set; } = null!;

		#endregion
	}

	[Table(Schema="DB2INST1", Name="GrandChild")]
	public partial class GrandChild
	{
		[Column, Nullable] public int? ParentID     { get; set; } // INTEGER
		[Column, Nullable] public int? ChildID      { get; set; } // INTEGER
		[Column, Nullable] public int? GrandChildID { get; set; } // INTEGER
	}

	[Table(Schema="DB2INST1", Name="InheritanceChild")]
	public partial class InheritanceChild
	{
		[PrimaryKey, NotNull    ] public int     InheritanceChildId  { get; set; } // INTEGER
		[Column,     NotNull    ] public int     InheritanceParentId { get; set; } // INTEGER
		[Column,        Nullable] public int?    TypeDiscriminator   { get; set; } // INTEGER
		[Column,        Nullable] public string? Name                { get; set; } // VARCHAR(50)
	}

	[Table(Schema="DB2INST1", Name="InheritanceParent")]
	public partial class InheritanceParent
	{
		[PrimaryKey, NotNull    ] public int     InheritanceParentId { get; set; } // INTEGER
		[Column,        Nullable] public int?    TypeDiscriminator   { get; set; } // INTEGER
		[Column,        Nullable] public string? Name                { get; set; } // VARCHAR(50)
	}

	[Table(Schema="DB2INST1", Name="KeepIdentityTest")]
	public partial class KeepIdentityTest
	{
		[PrimaryKey, Identity] public int  ID    { get; set; } // INTEGER
		[Column,     Nullable] public int? Value { get; set; } // INTEGER
	}

	[Table(Schema="DB2INST1", Name="LinqDataTypes")]
	public partial class LinqDataType
	{
		[Column, Nullable] public int?      ID             { get; set; } // INTEGER
		[Column, Nullable] public decimal?  MoneyValue     { get; set; } // DECIMAL(10,4)
		[Column, Nullable] public DateTime? DateTimeValue  { get; set; } // TIMESTAMP
		[Column, Nullable] public DateTime? DateTimeValue2 { get; set; } // TIMESTAMP
		[Column, Nullable] public short?    BoolValue      { get; set; } // SMALLINT
		[Column, Nullable] public byte[]?   GuidValue      { get; set; } // CHAR (16) FOR BIT DATA
		[Column, Nullable] public byte[]?   BinaryValue    { get; set; } // BLOB(5000)
		[Column, Nullable] public short?    SmallIntValue  { get; set; } // SMALLINT
		[Column, Nullable] public int?      IntValue       { get; set; } // INTEGER
		[Column, Nullable] public long?     BigIntValue    { get; set; } // BIGINT
		[Column, Nullable] public string?   StringValue    { get; set; } // VARCHAR(50)
	}

	[Table(Schema="DB2INST1", Name="MASTERTABLE")]
	public partial class MASTERTABLE
	{
		[PrimaryKey(0), NotNull] public int ID1 { get; set; } // INTEGER
		[PrimaryKey(1), NotNull] public int ID2 { get; set; } // INTEGER

		#region Associations

		/// <summary>
		/// FK_SLAVETABLE_MASTERTABLE_BackReference (DB2INST1.SLAVETABLE)
		/// </summary>
		[Association(ThisKey=nameof(ID1) + ", " + nameof(ID2), OtherKey=nameof(Default.DB2.SLAVETABLE.ID222222222222222222222222) + ", " + nameof(Default.DB2.SLAVETABLE.ID1), CanBeNull=true)]
		public IEnumerable<SLAVETABLE> Slavetables { get; set; } = null!;

		#endregion
	}

	[Table(Schema="DB2INST1", Name="Parent")]
	public partial class Parent
	{
		[Column, Nullable] public int? ParentID { get; set; } // INTEGER
		[Column, Nullable] public int? Value1   { get; set; } // INTEGER
	}

	[Table(Schema="DB2INST1", Name="Patient")]
	public partial class Patient
	{
		[PrimaryKey, NotNull] public int    PersonID  { get; set; } // INTEGER
		[Column,     NotNull] public string Diagnosis { get; set; } = null!; // VARCHAR(256)

		#region Associations

		/// <summary>
		/// FK_Patient_Person (DB2INST1.Person)
		/// </summary>
		[Association(ThisKey=nameof(PersonID), OtherKey=nameof(Default.DB2.Person.PersonID), CanBeNull=false)]
		public Person Person { get; set; } = null!;

		#endregion
	}

	[Table(Schema="DB2INST1", Name="Person")]
	public partial class Person
	{
		[PrimaryKey, Identity   ] public int     PersonID   { get; set; } // INTEGER
		[Column,     NotNull    ] public string  FirstName  { get; set; } = null!; // VARCHAR(50)
		[Column,     NotNull    ] public string  LastName   { get; set; } = null!; // VARCHAR(50)
		[Column,        Nullable] public string? MiddleName { get; set; } // VARCHAR(50)
		[Column,     NotNull    ] public char    Gender     { get; set; } // CHARACTER(1)

		#region Associations

		/// <summary>
		/// FK_Doctor_Person_BackReference (DB2INST1.Doctor)
		/// </summary>
		[Association(ThisKey=nameof(PersonID), OtherKey=nameof(Default.DB2.Doctor.PersonID), CanBeNull=true)]
		public Doctor? Doctor { get; set; }

		/// <summary>
		/// FK_Patient_Person_BackReference (DB2INST1.Patient)
		/// </summary>
		[Association(ThisKey=nameof(PersonID), OtherKey=nameof(Default.DB2.Patient.PersonID), CanBeNull=true)]
		public Patient? Patient { get; set; }

		#endregion
	}

	[Table(Schema="DB2INST1", Name="PERSONVIEW", IsView=true)]
	public partial class PERSONVIEW
	{
		[Column, NotNull    ] public int     PersonID   { get; set; } // INTEGER
		[Column, NotNull    ] public string  FirstName  { get; set; } = null!; // VARCHAR(50)
		[Column, NotNull    ] public string  LastName   { get; set; } = null!; // VARCHAR(50)
		[Column,    Nullable] public string? MiddleName { get; set; } // VARCHAR(50)
		[Column, NotNull    ] public char    Gender     { get; set; } // CHARACTER(1)
	}

	[Table(Schema="DB2INST1", Name="SLAVETABLE")]
	public partial class SLAVETABLE
	{
		[Column(),                                NotNull] public int ID1                        { get; set; } // INTEGER
		[Column("ID 2222222222222222222222  22"), NotNull] public int ID222222222222222222222222 { get; set; } // INTEGER
		[Column("ID 2222222222222222"),           NotNull] public int ID2222222222222222         { get; set; } // INTEGER

		#region Associations

		/// <summary>
		/// FK_SLAVETABLE_MASTERTABLE (DB2INST1.MASTERTABLE)
		/// </summary>
		[Association(ThisKey=nameof(ID222222222222222222222222) + ", " + nameof(ID1), OtherKey=nameof(Default.DB2.MASTERTABLE.ID1) + ", " + nameof(Default.DB2.MASTERTABLE.ID2), CanBeNull=false)]
		public MASTERTABLE MASTERTABLE { get; set; } = null!;

		#endregion
	}

	[Table(Schema="DB2INST1", Name="TestIdentity")]
	public partial class TestIdentity
	{
		[PrimaryKey, Identity] public int ID { get; set; } // INTEGER
	}

	[Table(Schema="DB2INST1", Name="TestMerge1")]
	public partial class TestMerge1
	{
		[PrimaryKey, NotNull    ] public int       Id              { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field1          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field2          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field3          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field4          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field5          { get; set; } // INTEGER
		[Column,        Nullable] public long?     FieldInt64      { get; set; } // BIGINT
		[Column,        Nullable] public short?    FieldBoolean    { get; set; } // SMALLINT
		[Column,        Nullable] public string?   FieldString     { get; set; } // VARCHAR(20)
		[Column,        Nullable] public string?   FieldNString    { get; set; } // VARCHAR(80)
		[Column,        Nullable] public char?     FieldChar       { get; set; } // CHARACTER(1)
		[Column,        Nullable] public string?   FieldNChar      { get; set; } // CHARACTER(4)
		[Column,        Nullable] public float?    FieldFloat      { get; set; } // REAL
		[Column,        Nullable] public double?   FieldDouble     { get; set; } // DOUBLE
		[Column,        Nullable] public DateTime? FieldDateTime   { get; set; } // TIMESTAMP
		[Column,        Nullable] public byte[]?   FieldBinary     { get; set; } // VARCHAR (20) FOR BIT DATA
		[Column,        Nullable] public byte[]?   FieldGuid       { get; set; } // CHAR (16) FOR BIT DATA
		[Column,        Nullable] public decimal?  FieldDecimal    { get; set; } // DECIMAL(24,10)
		[Column,        Nullable] public DateTime? FieldDate       { get; set; } // DATE
		[Column,        Nullable] public TimeSpan? FieldTime       { get; set; } // TIME
		[Column,        Nullable] public string?   FieldEnumString { get; set; } // VARCHAR(20)
		[Column,        Nullable] public int?      FieldEnumNumber { get; set; } // INTEGER
	}

	[Table(Schema="DB2INST1", Name="TestMerge2")]
	public partial class TestMerge2
	{
		[PrimaryKey, NotNull    ] public int       Id              { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field1          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field2          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field3          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field4          { get; set; } // INTEGER
		[Column,        Nullable] public int?      Field5          { get; set; } // INTEGER
		[Column,        Nullable] public long?     FieldInt64      { get; set; } // BIGINT
		[Column,        Nullable] public short?    FieldBoolean    { get; set; } // SMALLINT
		[Column,        Nullable] public string?   FieldString     { get; set; } // VARCHAR(20)
		[Column,        Nullable] public string?   FieldNString    { get; set; } // VARCHAR(80)
		[Column,        Nullable] public char?     FieldChar       { get; set; } // CHARACTER(1)
		[Column,        Nullable] public string?   FieldNChar      { get; set; } // CHARACTER(4)
		[Column,        Nullable] public float?    FieldFloat      { get; set; } // REAL
		[Column,        Nullable] public double?   FieldDouble     { get; set; } // DOUBLE
		[Column,        Nullable] public DateTime? FieldDateTime   { get; set; } // TIMESTAMP
		[Column,        Nullable] public byte[]?   FieldBinary     { get; set; } // VARCHAR (20) FOR BIT DATA
		[Column,        Nullable] public byte[]?   FieldGuid       { get; set; } // CHAR (16) FOR BIT DATA
		[Column,        Nullable] public decimal?  FieldDecimal    { get; set; } // DECIMAL(24,10)
		[Column,        Nullable] public DateTime? FieldDate       { get; set; } // DATE
		[Column,        Nullable] public TimeSpan? FieldTime       { get; set; } // TIME
		[Column,        Nullable] public string?   FieldEnumString { get; set; } // VARCHAR(20)
		[Column,        Nullable] public int?      FieldEnumNumber { get; set; } // INTEGER
	}

	public static partial class TestDataDBStoredProcedures
	{
		#region ADDISSUE792RECORD

		public static int ADDISSUE792RECORD(this TestDataDB dataConnection)
		{
			return dataConnection.ExecuteProc("DB2INST1.ADDISSUE792RECORD");
		}

		#endregion

		#region PersonSelectbykey

		public static int PersonSelectbykey(this TestDataDB dataConnection, int? id)
		{
			var parameters = new []
			{
				new DataParameter("ID", id, LinqToDB.DataType.Int32)
			};

			return dataConnection.ExecuteProc("DB2INST1.PERSON_SELECTBYKEY", parameters);
		}

		#endregion

		#region TestMODULE1TestProcedure

		public static int TestMODULE1TestProcedure(this TestDataDB dataConnection, int? i)
		{
			var parameters = new []
			{
				new DataParameter("I", i, LinqToDB.DataType.Int32)
			};

			return dataConnection.ExecuteProc("DB2INST1.TEST_MODULE1.TEST_PROCEDURE", parameters);
		}

		#endregion

		#region TestMODULE2TestProcedure

		public static int TestMODULE2TestProcedure(this TestDataDB dataConnection, int? i)
		{
			var parameters = new []
			{
				new DataParameter("I", i, LinqToDB.DataType.Int32)
			};

			return dataConnection.ExecuteProc("DB2INST1.TEST_MODULE2.TEST_PROCEDURE", parameters);
		}

		#endregion

		#region TestProcedure

		public static int TestProcedure(this TestDataDB dataConnection, int? i)
		{
			var parameters = new []
			{
				new DataParameter("I", i, LinqToDB.DataType.Int32)
			};

			return dataConnection.ExecuteProc("DB2INST1.TEST_PROCEDURE", parameters);
		}

		#endregion
	}

	public static partial class SqlFunctions
	{
		#region TestFunction

		[Sql.Function(Name="DB2INST1.TEST_FUNCTION", ServerSideOnly=true)]
		public static int? TestFunction(int? i)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region TestMODULE1TestFunction

		[Sql.Function(Name="DB2INST1.TEST_MODULE1.TEST_FUNCTION", ServerSideOnly=true)]
		public static int? TestMODULE1TestFunction(int? i)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region TestMODULE2TestFunction

		[Sql.Function(Name="DB2INST1.TEST_MODULE2.TEST_FUNCTION", ServerSideOnly=true)]
		public static int? TestMODULE2TestFunction(int? i)
		{
			throw new InvalidOperationException();
		}

		#endregion
	}

	public static partial class TableExtensions
	{
		public static ALLTYPE? Find(this ITable<ALLTYPE> table, int ID)
		{
			return table.FirstOrDefault(t =>
				t.ID == ID);
		}

		public static Doctor? Find(this ITable<Doctor> table, int PersonID)
		{
			return table.FirstOrDefault(t =>
				t.PersonID == PersonID);
		}

		public static InheritanceChild? Find(this ITable<InheritanceChild> table, int InheritanceChildId)
		{
			return table.FirstOrDefault(t =>
				t.InheritanceChildId == InheritanceChildId);
		}

		public static InheritanceParent? Find(this ITable<InheritanceParent> table, int InheritanceParentId)
		{
			return table.FirstOrDefault(t =>
				t.InheritanceParentId == InheritanceParentId);
		}

		public static KeepIdentityTest? Find(this ITable<KeepIdentityTest> table, int ID)
		{
			return table.FirstOrDefault(t =>
				t.ID == ID);
		}

		public static MASTERTABLE? Find(this ITable<MASTERTABLE> table, int ID1, int ID2)
		{
			return table.FirstOrDefault(t =>
				t.ID1 == ID1 &&
				t.ID2 == ID2);
		}

		public static Patient? Find(this ITable<Patient> table, int PersonID)
		{
			return table.FirstOrDefault(t =>
				t.PersonID == PersonID);
		}

		public static Person? Find(this ITable<Person> table, int PersonID)
		{
			return table.FirstOrDefault(t =>
				t.PersonID == PersonID);
		}

		public static TestIdentity? Find(this ITable<TestIdentity> table, int ID)
		{
			return table.FirstOrDefault(t =>
				t.ID == ID);
		}

		public static TestMerge1? Find(this ITable<TestMerge1> table, int Id)
		{
			return table.FirstOrDefault(t =>
				t.Id == Id);
		}

		public static TestMerge2? Find(this ITable<TestMerge2> table, int Id)
		{
			return table.FirstOrDefault(t =>
				t.Id == Id);
		}
	}
}
