To create a data model template take a look at one of the CopyMe.XXX.tt.txt file in your LinqToDB.Templates project folder.

* Use the following initialization before you call the LoadMetadata() method.

```c#
NamespaceName            = "DataModels";       // Namespace of the generated classes.

DataContextName          = null;               // DataContext class name. If null - database name + "DB".
BaseDataContextClass     = null;               // Base DataContext class name. If null - LinqToDB.Data.DataConnection.
GenerateConstructors     = true;               // Enforce generating DataContext constructors.
DefaultConfiguration     = null;               // Defines default configuration for default DataContext constructor.

BaseEntityClass          = null;               // Base Entity class name. If null - none.
DatabaseName             = null;               // Table database name - [Table(Database="DatabaseName")].
GenerateDatabaseName     = false;              // Always generate table database name, even though DatabaseName is null.
IncludeDefaultSchema     = true;               // Default schema name is generated - [Table(Database="Northwind", Schema="dbo", Name="Customers")]
OneToManyAssociationType = "IEnumerable<{0}>"; // One To Many association type. Change it to "List<{0}>" if needed.
GenerateAssociations     = true;               // Enforce generating associations.
GenerateBackReferences   = true;               // Enforce generating backreference associations.

ReplaceSimilarTables     = true;               // Replaces stored procedure result class names with similar to existing table class names.
GenerateFindExtensions   = true;               // Generates find extension methods based on PKs information.
IsCompactColumns         = true;               // If true, column compact view.

PluralizeClassNames                 = false;   // If true, pluralizes table class names.
SingularizeClassNames               = true;    // If true, singularizes table class names.
PluralizeDataContextPropertyNames   = true;    // If true, pluralizes DataContext property names.
SingularizeDataContextPropertyNames = false;   // If true, singularizes DataContex pProperty names.

GenerateDataTypes                   = false;   // If true, generates the DataType/Length/Precision/Scale properties of the Column attribute (unless overriden by the properties below).
GenerateDataTypeProperty            = null;    // If true, generates the DataType property of the Column attribute. If false, excludes generation on the DataType property even if GenerateDataTypes == true.
GenerateLengthProperty              = null;    // If true, generates the Length property of the Column attribute. If false, excludes generation on the Length property even if GenerateDataTypes == true.
GeneratePrecisionProperty           = null;    // If true, generates the Precision property of the Column attribute. If false, excludes generation on the Precision property even if GenerateDataTypes == true.
GenerateScaleProperty               = null;    // If true, generates the Scale property of the Column attribute. If false, excludes generation on the Scale property even if GenerateDataTypes == true.
GenerateDbTypes                     = false;   // If true, generates the DbType property of the Column attribute.

GenerateObsoleteAttributeForAliases = false;   // If true, generates [Obsolete] attribute for aliases.
IsCompactColumnAliases              = true;    // If true, column alias compact view.

GetSchemaOptions.ExcludedSchemas = new[] { "TestUser", "SYSSTAT" }; // Defines excluded schemas.
GetSchemaOptions.IncludedSchemas = new[] { "TestUser", "SYS" };     // Defines only included schemas.
```

* Use the following code to modify your model befor you call the GenerateModel() method.

```c#
GetTable("Person").TypeName = "MyName";                                             // Replaces table name.
GetColumn("Person", "PersonID").MemberName = "ID";                                  // Replaces column PersonID of Person table with ID.
GetFK("Orders", "FK_Orders_Customers").MemberName      = "Customers";               // Replaces association name.
GetFK("Orders", "FK_Orders_Customers").AssociationType = AssociationType.OneToMany; // Changes association type.

SetTable(string tableName,
	string TypeName = null,
	string DataContextPropertyName = null)

	.Column(string columnName, string MemberName = null, string Type = null, bool? IsNullable = null)
	.FK    (string fkName,     string MemberName = null, AssociationType? AssociationType = null)
	;

Model.Usings.Add("MyNamespace"); // Adds using of namespace.

// Replaces all the columns where name is 'TableName' + 'ID' with 'ID'.
foreach (var t in Tables.Values)
	foreach (var c in t.Columns.Values)
		if (c.IsPrimaryKey && c.MemberName == t.TypeName + "ID")
			c.MemberName = "ID";
```

* Useful members and data structues.

```c#
Dictionary<string,Table>     Tables     = new Dictionary<string,Table>    ();
Dictionary<string,Procedure> Procedures = new Dictionary<string,Procedure>();

Table      GetTable     (string name);
Procedure  GetProcedure (string name);
Column     GetColumn    (string tableName, string columnName);
ForeignKey GetFK        (string tableName, string fkName);
ForeignKey GetForeignKey(string tableName, string fkName);

public class Table
{
	public string Schema;
	public string TableName;
	public string DataContextPropertyName;
	public bool   IsView;
	public string Description;
	public string AliasPropertyName;
	public string AliasTypeName;
	public string TypeName;

	public Dictionary<string,Column>     Columns;
	public Dictionary<string,ForeignKey> ForeignKeys;
}

public partial class Column : Property
{
	public string    ColumnName; // Column name in database
	public bool      IsNullable;
	public bool      IsIdentity;
	public string    ColumnType; // Type of the column in database
	public DbType    DbType;
	public string    Description;
	public bool      IsPrimaryKey;
	public int       PrimaryKeyOrder;
	public bool      SkipOnUpdate;
	public bool      SkipOnInsert;
	public bool      IsDuplicateOrEmpty;
	public string    AliasName;
	public string    MemberName;
}

public enum AssociationType
{
	Auto,
	OneToOne,
	OneToMany,
	ManyToOne,
}

public partial class ForeignKey : Property
{
	public string           KeyName;
	public Table            OtherTable;
	public List<Column>     ThisColumns;
	public List<Column>     OtherColumns;
	public bool             CanBeNull;
	public ForeignKey       BackReference;
	public string           MemberName;
	public AssociationType  AssociationType;
}

public partial class Procedure : Method
{
	public string          Schema;
	public string          ProcedureName;
	public bool            IsFunction;
	public bool            IsTableFunction;
	public bool            IsDefaultSchema;

	public Table           ResultTable;
	public Exception       ResultException;
	public List<Table>     SimilarTables;
	public List<Parameter> ProcParameters;
}

public class Parameter
{
	public string   SchemaName;
	public string   SchemaType;
	public bool     IsIn;
	public bool     IsOut;
	public bool     IsResult;
	public int?     Size;
	public string   ParameterName;
	public string   ParameterType;
	public Type     SystemType;
	public string   DataType;
}
```
