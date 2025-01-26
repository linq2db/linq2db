using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.DataModel;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Schema;

namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Data model-related options.
	/// </summary>
	public sealed class DataModelOptions
	{
		internal DataModelOptions() { }

		#region General
		/// <summary>
		/// Enables generation of Database name in mappings (for tables, views, procedures and functions).
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool IncludeDatabaseName { get; set; }

		/// <summary>
		/// Enables generation of Schema name for default schemas in mappings (for tables, views, procedures and functions).
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool GenerateDefaultSchema { get; set; }

		/// <summary>
		/// Specifies type of generated metadata source.
		/// <list type="bullet">
		/// <item>Default: <see cref="MetadataSource.Attributes"/></item>
		/// <item>In T4 compability mode: <see cref="MetadataSource.Attributes"/></item>
		/// </list>
		/// </summary>
		public MetadataSource Metadata { get; set; } = MetadataSource.Attributes;
		#endregion

		#region Entities
		/// <summary>
		/// Gets or sets name of class to use as base class for generated entities. Must be a full type name with namespace.
		/// If type is nested, it should use + for type separator, e.g. <c>"My.NameSpace.SomeClass+BaseEntity"</c>
		/// Current limitaion - type cannot be generic.
		/// <list type="bullet">
		/// <item>Default: <c>null</c></item>
		/// <item>In T4 compability mode: <c>null</c></item>
		/// </list>
		/// </summary>
		public string? BaseEntityClass { get; set; }

		/// <summary>
		/// Enables partial class modifier applied to entity mapping classes.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool EntityClassIsPartial { get; set; }

		/// <summary>
		/// Gets or sets name generation and normalization rules for entity column properties.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.T4CompatNonPluralized"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <see cref="NormalizationOptions.MaxUpperCaseWordLength"/>=2</item>
		/// </list>
		/// </summary>
		public NormalizationOptions EntityColumnPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		/// <summary>
		/// Gets or sets name generation and normalization rules for entity classes.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <see cref="Pluralization.Singular"/>, <c>PluralizeOnlyIfLastWordIsText = true</c></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.T4CompatNonPluralized"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <see cref="Pluralization.Singular"/>, <c>PluralizeOnlyIfLastWordIsText = true</c></item>
		/// </list>
		/// </summary>
		public NormalizationOptions EntityClassNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.Singular, PluralizeOnlyIfLastWordIsText = true };

		/// <summary>
		/// Gets or sets name generation and normalization rules for entity table access property in data context class.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <see cref="Pluralization.PluralIfLongerThanOne"/>, <c>PluralizeOnlyIfLastWordIsText = true</c></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.T4CompatPluralized"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <see cref="Pluralization.PluralIfLongerThanOne"/>, <c>PluralizeOnlyIfLastWordIsText = true</c></item>
		/// </list>
		/// </summary>
		public NormalizationOptions EntityContextPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.PluralIfLongerThanOne, PluralizeOnlyIfLastWordIsText = true };

		/// <summary>
		/// Gets or sets custom name generator for entity class name.
		/// <list type="bullet">
		/// <item>Default: not set</item>
		/// <item>In T4 compability mode: not set</item>
		/// </list>
		/// </summary>
		public Func<TableLikeObject, string?>? EntityClassNameProvider { get; set; }

		/// <summary>
		/// Gets or sets custom name generator for data context property name to access entity table.
		/// <list type="bullet">
		/// <item>Default: not set</item>
		/// <item>In T4 compability mode: not set</item>
		/// </list>
		/// </summary>
		public Func<TableLikeObject, string?>? EntityContextPropertyNameProvider { get; set; }

		/// <summary>
		/// Enables generation of <see cref="DataType"/> enum value for entity column mapping.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateDataType { get; set; }

		/// <summary>
		/// Enables generation of database type name for entity column mapping.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateDbType { get; set; }

		/// <summary>
		/// Enables generation of database type length for entity column mapping.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateLength { get; set; }

		/// <summary>
		/// Enables generation of database type precision for entity column mapping.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GeneratePrecision { get; set; }

		/// <summary>
		/// Enables generation of database type scale for entity column mapping.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateScale { get; set; }

		/// <summary>
		/// Enables the use of a type discriminator parameter in fluent mapping.
		/// Enabling this requires that a set of extension methods taking (this FluentMappingBuilder builder) and type discriminators.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool UseFluentEntityTypeDiscriminator { get; set; }
		#endregion

		#region Context
		/// <summary>
		/// Enables generation of comment with database information on data context class.
		/// Includes database name, data source and server version values if available from schema provider for current database.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool IncludeDatabaseInfo { get; set; }

		/// <summary>
		/// Enables generation of default data context constructor.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool HasDefaultConstructor { get; set; } = true;

		/// <summary>
		/// Enables generation of data context constructor with <c>(<see cref="string"/> configurationName)</c> parameter.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool HasConfigurationConstructor { get; set; } = true;

		/// <summary>
		/// Enables generation of data context constructor with non-generic <c>(<see cref="DataOptions"/> options)</c> parameter.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool HasUntypedOptionsConstructor { get; set; }

		/// <summary>
		/// Enables generation of data context constructor with generic <c>(<see cref="DataOptions{T}"/> options)</c> parameter,
		/// where <c>T</c> is generated data context class.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool HasTypedOptionsConstructor { get; set; } = true;

		/// <summary>
		/// Gets or sets name of data context class. When not set, database name will be used.
		/// If database name not provided by schema provider, <c>"MyDataContext"</c> used as name.
		/// <list type="bullet">
		/// <item>Default: not set</item>
		/// <item>In T4 compability mode: not set</item>
		/// </list>
		/// </summary>
		public string? ContextClassName { get; set; }

		/// <summary>
		/// Gets or sets the modifier of data context class. When not set, public will be used. Context class is always partial.
		/// <list type="bullet">
		/// <item>Default: not set</item>
		/// <item>In T4 compability mode: not set</item>
		/// </list>
		/// </summary>
		public Modifiers? ContextClassModifier { get; set; }

		/// <summary>
		/// Gets or sets type name (full name with namespace) of base class for generated data context. When not specified, <see cref="DataConnection"/> type used.
		/// Provided type should implement <see cref="IDataContext"/> interface and it is recommented to use wether <see cref="DataConnection"/>
		/// or <see cref="DataContext"/> classes or classes, derived from them.
		/// <list type="bullet">
		/// <item>Default: not set</item>
		/// <item>In T4 compability mode: not set</item>
		/// </list>
		/// </summary>
		public string? BaseContextClass { get; set; }

		/// <summary>
		/// Gets or sets name generation and normalization rules for data context class name when name not provided by user but generated automatically.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode:  <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions DataContextClassNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		/// <summary>
		/// Enables generation of InitDataContext partial method on data context class.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool GenerateInitDataContextMethod { get; set; } = true;
		#endregion

		#region Associations
		/// <summary>
		/// Gets or sets name generation and normalization rules for assocation from foreign key source entity side.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.Association"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.Association"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions SourceAssociationPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.Association, Pluralization = Pluralization.None };

		/// <summary>
		/// Gets or sets name generation and normalization rules for assocation from foreign key target entity side with singular cardinality.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.Association"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.Association"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions TargetSingularAssociationPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.Association, Pluralization = Pluralization.None };

		/// <summary>
		/// Gets or sets name generation and normalization rules for assocation from foreign key target entity side with multiple cardinality.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.Association"/>, <see cref="Pluralization.PluralIfLongerThanOne"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.Association"/>, <see cref="Pluralization.PluralIfLongerThanOne"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions TargetMultipleAssociationPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.Association, Pluralization = Pluralization.PluralIfLongerThanOne };

		/// <summary>
		/// Enables generation of associations for foreign keys as entity properties.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool GenerateAssociations { get; set; } = true;

		/// <summary>
		/// Enables generation of associations for foreign keys as extension methods (with entity as extended type).
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateAssociationExtensions { get; set; }

		/// <summary>
		/// Enables use of array as collection type for association properties/methods.
		/// Otherwise see <see cref="AssociationCollectionType"/> setting.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool AssociationCollectionAsArray { get; set; }

		/// <summary>
		/// When specified, many-sided association property/method will use specified type as return type.
		/// Type must be open generic type with one type argument, e.g. <see cref="IEnumerable{T}"/>, <see cref="List{T}"/> or <see cref="ICollection{T}"/>.
		/// E.g. <code>"System.Collections.Generic.List&lt;&gt;"</code>
		/// If not configured, <see cref="IEnumerable{T}"/> type will be used.
		/// Option ignored if <see cref="AssociationCollectionType"/> set to <c>true</c>.
		/// <list type="bullet">
		/// <item>Default: not set</item>
		/// <item>In T4 compability mode: not set</item>
		/// </list>
		/// </summary>
		public string? AssociationCollectionType { get; set; }
		#endregion

		#region Procedures/Functions
		/// <summary>
		/// Enables reuse of generated entity mapping class and stored procedure or table function return record type, when record mappings match known entity mappings (record has same set of columns by name and type including nullability as entity).
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool MapProcedureResultToEntity { get; set; } = true;

		/// <summary>
		/// Gets or sets name generation and normalization rules for stored procedures and functions method parameters.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.CamelCase"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.CamelCase"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions ProcedureParameterNameOptions { get; set; } = new() { Casing = NameCasing.CamelCase, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		/// <summary>
		/// Gets or sets name generation and normalization rules for stored procedures and functions method names.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions ProcedureNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		/// <summary>
		/// Gets or sets name generation and normalization rules for mapping class for result tuple value of scalar function.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Result"</c></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Result"</c></item>
		/// </list>
		/// </summary>
		public NormalizationOptions FunctionTupleResultClassNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Result" };

		/// <summary>
		/// Gets or sets name generation and normalization rules for field properies of result tuple value mapping class of scalar function.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions FunctionTupleResultPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		/// <summary>
		/// Gets or sets name generation and normalization rules for custom mapping class for result record of stored procedure or table function.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Result"</c></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Result"</c></item>
		/// </list>
		/// </summary>
		public NormalizationOptions ProcedureResultClassNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Result" };
		/// <summary>
		/// Gets or sets name generation and normalization rules for custom mapping class for async stored procedure results wrapper for procedure with multiple returns.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Results"</c></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Results"</c></item>
		/// </list>
		/// </summary>
		public NormalizationOptions AsyncProcedureResultClassNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Results" };
		/// <summary>
		/// Gets or sets name generation and normalization rules for custom mapping class properties for async stored procedure results wrapper for procedure with multiple returns.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions AsyncProcedureResultClassPropertiesNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };
		/// <summary>
		/// Gets or sets name generation and normalization rules for column properties of custom mapping class for result record of stored procedure or table function.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.None"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <see cref="NormalizationOptions.MaxUpperCaseWordLength"/>=2</item>
		/// </list>
		/// </summary>
		public NormalizationOptions ProcedureResultColumnPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		/// <summary>
		/// When <c>true</c>, table function mapping use <see cref="ITable{T}"/> as return type.
		/// Otherwise <see cref="IQueryable{T}"/> type used.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool TableFunctionReturnsTable { get; set; }

		/// <summary>
		/// Enables generation of error if stored procedure or table function schema load failed.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateProceduresSchemaError { get; set; }

		/// <summary>
		/// Skip generation of mappings for stored procedure, if it failed to load it's schema.
		/// Otherwise mapping will be generated, but procedure will have only parameters without return data sets.
		/// This option doesn't affect table functions with schema errors - for functions  we skip them on error always, because
		/// table function must have return result set.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool SkipProceduresWithSchemaErrors { get; set; }

		/// <summary>
		/// When <c>true</c>, stored procedure mapping use <see cref="List{T}"/> as return type.
		/// Otherwise <see cref="IEnumerable{T}"/> type used.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateProcedureResultAsList { get; set; }

		/// <summary>
		/// Enables generation of database type name in stored procedure parameter mapping.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateProcedureParameterDbType { get; set; }

		/// <summary>
		/// Enables generation of sync version of stored procedure mapping.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>true</c></item>
		/// </list>
		/// </summary>
		public bool GenerateProcedureSync { get; set; } = true;

		/// <summary>
		/// Enables generation of async version of stored procedure mapping.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateProcedureAsync { get; set; } = true;
		#endregion

		#region Schemas
		/// <summary>
		/// Gets or sets name generation and normalization rules for wrapper class for non-default schema (when <see cref="GenerateSchemaAsType"/> option enabled).
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Schema"</c></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>Suffix = "Schema"</c></item>
		/// </list>
		/// </summary>
		public NormalizationOptions SchemaClassNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Schema" };

		/// <summary>
		/// Gets or sets name generation and normalization rules for non-default schema data context class accessor property on main data context (when <see cref="GenerateSchemaAsType"/> option enabled).
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.Pascal"/>, <see cref="NameTransformation.SplitByUnderscore"/></item>
		/// </list>
		/// </summary>
		public NormalizationOptions SchemaPropertyNameOptions { get; set; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		/// <summary>
		/// Enables generation of context for entities and procedures/functions from non-default schemas in separate context-like class.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateSchemaAsType { get; set; } = true;

		/// <summary>
		/// Enables generation of <see cref="IEquatable{T}"/> interface implementation on entity classes with primary key.
		/// <list type="bullet">
		/// <item>Default: <c>false</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool GenerateIEquatable { get; set; }

		/// <summary>
		/// Provides base names for schema wrapper class and main data context property for additional schemas (when <see cref="GenerateSchemaAsType"/> option set).
		/// <list type="bullet">
		/// <item>Default: empty</item>
		/// <item>In T4 compability mode: empty</item>
		/// </list>
		/// </summary>
		public IDictionary<string, string> SchemaMap { get; } = new Dictionary<string, string>();
		#endregion

		#region Find Extensions
		/// <summary>
		/// Enables generation of extension methods to access entity by primary key value (using name Find/FindAsync/FindQuery for generated method).
		/// <list type="bullet">
		/// <item>Default: <c><see cref="FindTypes.FindByPkOnTable"/> | <see cref="FindTypes.FindAsyncByPkOnTable"/></c></item>
		/// <item>In T4 compability mode: <see cref="FindTypes.FindByPkOnTable"/></item>
		/// </list>
		/// </summary>
		public FindTypes GenerateFindExtensions { get; set; } = FindTypes.FindByPkOnTable | FindTypes.FindAsyncByPkOnTable;

		/// <summary>
		/// Specifies order of primary key column parameters in Find method for entity with composite primary key.
		/// <list type="bullet">
		/// <item>Default: <c>true</c></item>
		/// <item>In T4 compability mode: <c>false</c></item>
		/// </list>
		/// </summary>
		public bool OrderFindParametersByColumnOrdinal { get; set; } = true;

		/// <summary>
		/// Gets or sets name generation and normalization rules for Find entity extension method parameters.
		/// <list type="bullet">
		/// <item>Default: <see cref="NameCasing.CamelCase"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>DontCaseAllCaps = false</c></item>
		/// <item>In T4 compability mode: <see cref="NameCasing.CamelCase"/>, <see cref="NameTransformation.SplitByUnderscore"/>, <c>DontCaseAllCaps = false</c></item>
		/// </list>
		/// </summary>
		public NormalizationOptions FindParameterNameOptions { get; set; } = new() { Casing = NameCasing.CamelCase, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, DontCaseAllCaps = false };
		#endregion
	}
}
