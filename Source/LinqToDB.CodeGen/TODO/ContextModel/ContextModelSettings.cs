using System;
using System.Collections.Generic;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ContextModelSettings
	{
		public string? ContextClassName { get; set; }
		public string? BaseContextClassName { get; set; }
		public string? BaseEntityClass { get; set; }
		public bool TypeNameConvention { get; set; } = true;

		public Func<TableLikeObject, string?>? EntityClassNameProvider { get; set; }
		public Func<TableLikeObject, string?>? EntityContextPropertyNameProvider { get; set; }

		public ObjectNormalizationSettings DataContextClassNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "DB" };
		public ObjectNormalizationSettings EntityClassNameNormalization { get; } = new() { Casing = NameCasing.T4CompatNonPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.Singular, PluralizeOnlyIfLastWordIsText = true };
		public ObjectNormalizationSettings EntityContextPropertyNameNormalization { get; } = new() { Casing = NameCasing.T4CompatPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.PluralIfLongerThanOne, PluralizeOnlyIfLastWordIsText = true };
		public ObjectNormalizationSettings EntityColumnPropertyNameNormalization { get; } = new() { Casing = NameCasing.T4CompatNonPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public ObjectNormalizationSettings SingularForeignKeyAssociationPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.T4Association, Pluralization = Pluralization.None };
		public ObjectNormalizationSettings SingularPrimaryKeyAssociationPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.T4Association, Pluralization = Pluralization.None };
		public ObjectNormalizationSettings MultiplePrimaryKeyAssociationPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.T4Association, Pluralization = Pluralization.PluralIfLongerThanOne };

		public ObjectNormalizationSettings ProcedureMethodInfoFieldNameNormalization { get; } = new() { Casing = NameCasing.CamelCase, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Prefix = "_" };
		public ObjectNormalizationSettings ProcedureNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };
		public ObjectNormalizationSettings ProcedureParameterNameNormalization { get; } = new() { Casing = NameCasing.CamelCase, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public ObjectNormalizationSettings ProcedureResultClassNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Result" };
		public ObjectNormalizationSettings ProcedureResultColumnPropertyNameNormalization { get; } = new() { Casing = NameCasing.None, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public ObjectNormalizationSettings FunctionTupleResultClassName { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Result" };
		public ObjectNormalizationSettings FunctionTupleResultPropertyName { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public ObjectNormalizationSettings ProcedureResultSetClassNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Results" };
		public ObjectNormalizationSettings ProcedureResultSetClassPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };


		public INameConverterProvider NameConverter { get; set; } = null!;

		public bool GenerateSchemaAsType  { get; set; }
		public bool GenerateDefaultSchema { get; set; }
		public bool MapProcedureResultToEntity { get; set; } = true;

		public bool HasDefaultConstructor { get; set; } = true;
		public bool HasConfigurationConstructor { get; set; } = true;
		public bool HasUntypedOptionsConstructor { get; set; } = true;
		public bool HasTypedOptionsConstructor { get; set; } = true;


		// quite useless setting...
		public IDictionary<string, string> SchemaMap { get; } = new Dictionary<string, string>();


	}
}
