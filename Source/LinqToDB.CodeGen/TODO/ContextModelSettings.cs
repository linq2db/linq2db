using System;
using System.Collections.Generic;
using LinqToDB.Naming;
using LinqToDB.Schema;

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

		public NormalizationOptions DataContextClassNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "DB" };
		public NormalizationOptions EntityClassNameNormalization { get; } = new() { Casing = NameCasing.T4CompatNonPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.Singular, PluralizeOnlyIfLastWordIsText = true };
		public NormalizationOptions EntityContextPropertyNameNormalization { get; } = new() { Casing = NameCasing.T4CompatPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.PluralIfLongerThanOne, PluralizeOnlyIfLastWordIsText = true };
		public NormalizationOptions EntityColumnPropertyNameNormalization { get; } = new() { Casing = NameCasing.T4CompatNonPluralized, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public NormalizationOptions SingularForeignKeyAssociationPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.T4Compat, Pluralization = Pluralization.None };
		public NormalizationOptions SingularPrimaryKeyAssociationPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.T4Compat, Pluralization = Pluralization.None };
		public NormalizationOptions MultiplePrimaryKeyAssociationPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.T4Compat, Pluralization = Pluralization.PluralIfLongerThanOne };

		public NormalizationOptions ProcedureMethodInfoFieldNameNormalization { get; } = new() { Casing = NameCasing.CamelCase, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Prefix = "_" };
		public NormalizationOptions ProcedureNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };
		public NormalizationOptions ProcedureParameterNameNormalization { get; } = new() { Casing = NameCasing.CamelCase, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public NormalizationOptions ProcedureResultClassNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Result" };
		public NormalizationOptions ProcedureResultColumnPropertyNameNormalization { get; } = new() { Casing = NameCasing.None, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public NormalizationOptions FunctionTupleResultClassName { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Result" };
		public NormalizationOptions FunctionTupleResultPropertyName { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };

		public NormalizationOptions ProcedureResultSetClassNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None, Suffix = "Results" };
		public NormalizationOptions ProcedureResultSetClassPropertyNameNormalization { get; } = new() { Casing = NameCasing.Pascal, Transformation = NameTransformation.SplitByUnderscore, Pluralization = Pluralization.None };


		public INameConversionProvider NameConverter { get; set; } = null!;

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
