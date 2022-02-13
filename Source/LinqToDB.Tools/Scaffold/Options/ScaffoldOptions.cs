using LinqToDB.DataModel;
using LinqToDB.Schema;

namespace LinqToDB.Scaffold
{
	public class ScaffoldOptions
	{
		private ScaffoldOptions()
		{
		}

		public SchemaOptions         Schema         { get; } = new ();
		public DataModelOptions      DataModel      { get; } = new ();
		public CodeGenerationOptions CodeGeneration { get; } = new ();

		/// <summary>
		/// Gets default scaffold options.
		/// </summary>
		/// <returns>Options object.</returns>
		public static ScaffoldOptions Default()
		{
			// no need to configure, default options already set
			return new ScaffoldOptions();
		}

		/// <summary>
		/// Gets options that correspond to default settings, used by T4 templates.
		/// </summary>
		/// <returns>Options object.</returns>
		public static ScaffoldOptions T4()
		{
			var options = new ScaffoldOptions();

			// explicitly set each option even if it match defaults as we can change defaults in future and it shouldn't result in regression

			// schema load options
			options.Schema.LoadedObjects               = SchemaObjects.Table | SchemaObjects.View | SchemaObjects.ForeignKey | SchemaObjects.StoredProcedure | SchemaObjects.AggregateFunction | SchemaObjects.ScalarFunction | SchemaObjects.TableFunction;
			options.Schema.PreferProviderSpecificTypes = false;
			options.Schema.LoadTableOrView             = (_, _) => true;
			options.Schema.IgnoreDuplicateForeignKeys  = false;
			options.Schema.IncludeSchemas              = true;
			options.Schema.IncludeCatalogs             = true;
			options.Schema.UseSafeSchemaLoad           = false;
			options.Schema.LoadProceduresSchema        = true;
			options.Schema.LoadProcedureSchema         = _ => true;
			options.Schema.LoadTableFunction           = _ => true;
			options.Schema.EnableSqlServerReturnValue  = false;
			options.Schema.Schemas .Clear();
			options.Schema.Catalogs.Clear();

			// data model options
			options.DataModel.IncludeDatabaseName                            = false;
			options.DataModel.GenerateDefaultSchema                          = true;
			options.DataModel.BaseEntityClass                                = null;
			//options.DataModel.EntityColumnPropertyNameOptions                = null;
			//options.DataModel.EntityClassNameOptions                         = null;
			//options.DataModel.EntityContextPropertyNameOptions               = null;
			options.DataModel.EntityClassNameProvider                        = null;
			options.DataModel.EntityContextPropertyNameProvider              = null;
			options.DataModel.GenerateDataType                               = false;
			options.DataModel.GenerateDbType                                 = false;
			options.DataModel.GenerateLength                                 = false;
			options.DataModel.GeneratePrecision                              = false;
			options.DataModel.GenerateScale                                  = false;
			options.DataModel.IncludeDatabaseInfo                            = false;
			options.DataModel.HasDefaultConstructor                          = true;
			options.DataModel.HasConfigurationConstructor                    = true;
			options.DataModel.HasUntypedOptionsConstructor                   = true;
			options.DataModel.HasTypedOptionsConstructor                     = true;
			options.DataModel.ContextClassName                               = null;
			options.DataModel.BaseContextClass                               = null;
			//options.DataModel.DataContextClassNameOptions                    = null;
			//options.DataModel.SourceAssociationPropertyNameOptions           = null;
			//options.DataModel.TargetSingularAssociationPropertyNameOptions   = null;
			//options.DataModel.TargetMultipleAssociationPropertyNameOptions   = null;
			options.DataModel.GenerateAssociations                           = true;
			options.DataModel.GenerateAssociationExtensions                  = false;
			options.DataModel.AssociationCollectionAsArray                   = false;
			options.DataModel.AssociationCollectionType                      = null;
			options.DataModel.MapProcedureResultToEntity                     = true;
			//options.DataModel.ProcedureParameterNameOptions                  = null;
			//options.DataModel.ProcedureNameOptions                           = null;
			//options.DataModel.FunctionTupleResultClassNameOptions            = null;
			//options.DataModel.FunctionTupleResultPropertyNameOptions         = null;
			//options.DataModel.TableFunctionMethodInfoFieldNameOptions        = null;
			//options.DataModel.ProcedureResultClassNameOptions                = null;
			//options.DataModel.AsyncProcedureResultClassNameOptions           = null;
			//options.DataModel.AsyncProcedureResultClassPropertiesNameOptions = null;
			//options.DataModel.ProcedureResultColumnPropertyNameOptions       = null;
			options.DataModel.TableFunctionReturnsTable                      = true;
			options.DataModel.GenerateProceduresSchemaError                  = true;
			options.DataModel.SkipProceduresWithSchemaErrors                 = false;
			options.DataModel.GenerateProcedureResultAsList                  = false;
			options.DataModel.GenerateProcedureParameterDbType               = false;
			options.DataModel.GenerateProcedureSync                          = true;
			options.DataModel.GenerateProcedureAsync                         = false;
			//options.DataModel.SchemaClassNameOptions                         = null;
			//options.DataModel.SchemaPropertyNameOptions                      = null;
			options.DataModel.GenerateSchemaAsType                           = false;
			options.DataModel.GenerateFindExtensions                         = FindTypes.FindByPkOnTable;
			options.DataModel.OrderFindParametersByColumnOrdinal             = false;
			//options.DataModel.FindParameterNameOptions                       = null;
			options.DataModel.SchemaMap.Clear();

			// code generation options
			options.CodeGeneration.EnableNullableReferenceTypes  = true;
			options.CodeGeneration.Indent                        = "\t";
			options.CodeGeneration.NewLine                       = "\r\n";
			options.CodeGeneration.SuppressMissingXmlDocWarnings = true;
			options.CodeGeneration.MarkAsAutoGenerated           = true;
			options.CodeGeneration.ClassPerFile                  = false;
			options.CodeGeneration.AutoGeneratedHeader           = null; // text was a bit different in T4: referenced T4 template instead of tool
			options.CodeGeneration.Namespace                     = "DataModel";
			options.CodeGeneration.ConflictingNames.Clear();

			return options;
		}
	}
}
