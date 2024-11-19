using LinqToDB.DataModel;
using LinqToDB.Metadata;
using LinqToDB.Naming;
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
		public ProviderOptions ProviderOptions { get; } = new ();

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
			options.Schema.DefaultSchemas              = null;
			options.Schema.IncludeCatalogs             = true;
			options.Schema.IgnoreSystemHistoryTables   = false;
			options.Schema.UseSafeSchemaLoad           = false;
			options.Schema.LoadDatabaseName            = false;
			options.Schema.LoadProceduresSchema        = true;
			options.Schema.LoadProcedureSchema         = _ => true;
			options.Schema.LoadStoredProcedure         = _ => true;
			options.Schema.LoadTableFunction           = _ => true;
			options.Schema.LoadScalarFunction          = _ => true;
			options.Schema.LoadAggregateFunction       = _ => true;
			options.Schema.EnableSqlServerReturnValue  = false;
			options.Schema.Schemas .Clear();
			options.Schema.Catalogs.Clear();

			// data model options
			options.DataModel.IncludeDatabaseName                            = false;
			options.DataModel.GenerateDefaultSchema                          = true;
			options.DataModel.Metadata                                       = MetadataSource.Attributes;
			options.DataModel.BaseEntityClass                                = null;
			options.DataModel.EntityClassIsPartial                           = true;
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
			options.DataModel.GenerateInitDataContextMethod                  = true;
			options.DataModel.GenerateAssociations                           = true;
			options.DataModel.GenerateAssociationExtensions                  = false;
			options.DataModel.AssociationCollectionAsArray                   = false;
			options.DataModel.AssociationCollectionType                      = null;
			options.DataModel.MapProcedureResultToEntity                     = true;
			options.DataModel.TableFunctionReturnsTable                      = true;
			options.DataModel.GenerateProceduresSchemaError                  = false;
			options.DataModel.SkipProceduresWithSchemaErrors                 = true;
			options.DataModel.GenerateProcedureResultAsList                  = false;
			options.DataModel.GenerateProcedureParameterDbType               = false;
			options.DataModel.GenerateProcedureSync                          = true;
			options.DataModel.GenerateProcedureAsync                         = false;
			options.DataModel.GenerateSchemaAsType                           = false;
			options.DataModel.GenerateIEquatable                             = false;
			options.DataModel.GenerateFindExtensions                         = FindTypes.FindByPkOnTable;
			options.DataModel.OrderFindParametersByColumnOrdinal             = false;
			options.DataModel.EntityColumnPropertyNameOptions                = new() { Casing = NameCasing.T4CompatNonPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                 , MaxUpperCaseWordLength        = 2    };
			options.DataModel.EntityClassNameOptions                         = new() { Casing = NameCasing.T4CompatNonPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.Singular             , PluralizeOnlyIfLastWordIsText = true };
			options.DataModel.EntityContextPropertyNameOptions               = new() { Casing = NameCasing.T4CompatPluralized   , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.PluralIfLongerThanOne, PluralizeOnlyIfLastWordIsText = true };
			options.DataModel.DataContextClassNameOptions                    = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                                                        };
			options.DataModel.SourceAssociationPropertyNameOptions           = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.Association      , Pluralization = Pluralization.None                                                        };
			options.DataModel.TargetSingularAssociationPropertyNameOptions   = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.Association      , Pluralization = Pluralization.None                                                        };
			options.DataModel.TargetMultipleAssociationPropertyNameOptions   = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.Association      , Pluralization = Pluralization.PluralIfLongerThanOne                                       };
			options.DataModel.ProcedureParameterNameOptions                  = new() { Casing = NameCasing.CamelCase            , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                                                        };
			options.DataModel.ProcedureNameOptions                           = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                                                        };
			options.DataModel.FunctionTupleResultClassNameOptions            = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                 , Suffix = "Result"                    };
			options.DataModel.FunctionTupleResultPropertyNameOptions         = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                                                        };
			options.DataModel.ProcedureResultClassNameOptions                = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                 , Suffix = "Result"                    };
			options.DataModel.AsyncProcedureResultClassNameOptions           = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                 , Suffix = "Results"                   };
			options.DataModel.AsyncProcedureResultClassPropertiesNameOptions = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                                                        };
			options.DataModel.ProcedureResultColumnPropertyNameOptions       = new() { Casing = NameCasing.None                 , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                 , MaxUpperCaseWordLength        = 2    };
			options.DataModel.SchemaClassNameOptions                         = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                 , Suffix = "Schema"                    };
			options.DataModel.SchemaPropertyNameOptions                      = new() { Casing = NameCasing.Pascal               , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                                                        };
			options.DataModel.FindParameterNameOptions                       = new() { Casing = NameCasing.CamelCase            , Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None                 , DontCaseAllCaps = false              };
			options.DataModel.SchemaMap.Clear();

			// code generation options
			options.CodeGeneration.EnableNullableReferenceTypes  = true;
			options.CodeGeneration.Indent                        = "\t";
			options.CodeGeneration.NewLine                       = "\r\n";
			options.CodeGeneration.SuppressMissingXmlDocWarnings = true;
			options.CodeGeneration.MarkAsAutoGenerated           = true;
			options.CodeGeneration.ClassPerFile                  = false;
			options.CodeGeneration.AddGeneratedFileSuffix        = false;
			options.CodeGeneration.AutoGeneratedHeader           = null; // text was a bit different in T4: referenced T4 template instead of tool
			options.CodeGeneration.Namespace                     = "DataModel";
			options.CodeGeneration.ConflictingNames.Clear();

			return options;
		}
	}
}
