using System;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Features of target language and its code generator, used by data model generator to select code model shape and
	/// to adjust scaffold options the language cannot support.
	/// </summary>
	[Flags]
	public enum LanguageFeatures
	{
		/// <summary>
		/// No features.
		/// </summary>
		None                              = 0x0000,
		/// <summary>
		/// Types can be nested into other types. Without it, per-schema wrapper class is generated as module
		/// (<see cref="Modifiers.Module"/>).
		/// </summary>
		NestedTypes                       = 0x0001,
		/// <summary>
		/// Compiler narrows nullability of a re-accessed property after null check. Without it, generator suppresses
		/// nullability explicitly in such places.
		/// </summary>
		NullableFlowAnalysis              = 0x0002,
		/// <summary>
		/// Generic method type arguments are inferred from lambda return type. Without it, generator emits explicit
		/// type arguments.
		/// </summary>
		LambdaReturnTypeInference         = 0x0004,
		/// <summary>
		/// Values are implicitly converted to base type or implemented interface (e.g. <c>List&lt;T&gt;</c> to
		/// <c>IEnumerable&lt;T&gt;</c>). Without it, generator uses exact types.
		/// </summary>
		ImplicitUpcast                    = 0x0008,
		/// <summary>
		/// Public methods implicitly implement interface members. Without it, interfaces are implemented explicitly and
		/// generator doesn't call interface members on <see langword="this"/>.
		/// </summary>
		ImplicitInterfaceImplementation   = 0x0010,
		/// <summary>
		/// Lambdas are implicitly converted to delegate types. Without it, code generator names delegate type of
		/// lambda.
		/// </summary>
		ImplicitDelegateConversion        = 0x0020,
		/// <summary>
		/// Constructor can call instance methods that assign auto-properties. Without it, generator emits computed
		/// properties instead of initialization method.
		/// </summary>
		ConstructorPropertyInitialization = 0x0040,
		/// <summary>
		/// Partial types support. Without it, generated classes are not marked with <see cref="Modifiers.Partial"/> and
		/// <see cref="Scaffold.DataModelOptions.EntityClassIsPartial"/> option is disabled.
		/// </summary>
		PartialTypes                      = 0x0080,
		/// <summary>
		/// Partial methods support. Without it, <see cref="Scaffold.DataModelOptions.GenerateInitDataContextMethod"/>
		/// and <see cref="Scaffold.DataModelOptions.GenerateStaticInitDataContextMethod"/> options are disabled.
		/// </summary>
		PartialMethods                    = 0x0100,
		/// <summary>
		/// Code generator supports multiple constructors for a class. Without it, only
		/// <see cref="Scaffold.DataModelOptions.HasTypedOptionsConstructor"/> constructor is generated.
		/// </summary>
		MultipleConstructors              = 0x0200,
		/// <summary>
		/// Tabs could be used for indentation. Without it, each tab in
		/// <see cref="Scaffold.CodeGenerationOptions.Indent"/> option is replaced with four spaces.
		/// </summary>
		TabIndentation                    = 0x0400,
		/// <summary>
		/// Generated code could be split into multiple files without dependency on compilation order. Without it,
		/// <see cref="Scaffold.CodeGenerationOptions.ClassPerFile"/> option is disabled.
		/// </summary>
		MultipleFiles                     = 0x0800,
		/// <summary>
		/// Entities are generated as records (<see cref="Modifiers.Record"/>). Also disables
		/// <see cref="Scaffold.DataModelOptions.GenerateAssociationExtensions"/> option, as extension method would
		/// conflict with association field of same name.
		/// </summary>
		RecordEntities                    = 0x1000,
		/// <summary>
		/// Nullable scalar values are represented by option type and converted at parameter and mapping boundaries.
		/// </summary>
		OptionTypes                       = 0x2000,
	}
}
