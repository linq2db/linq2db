using System.Collections.Generic;

namespace LinqToDB.DataModel
{
	using CodeModel;

	// contains generation logic for data context class supplementary code like constructors
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates data context constructors.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="initSchemasMethodName">(Optional) additional schemas init method name.</param>
		private static void BuildDataContextConstructors(IDataModelGenerationContext context, CodeIdentifier? initSchemasMethodName)
		{
			var constructors = context.MainContextBuilder.Constructors();

			var ctors = new List<BlockBuilder>();

			// based on selected constructors set we generate (or not) following constructors:
			// .ctor() // default constructor
			// .ctor(string configuration) // constructor with connection configuration name parameter
			// .ctor(DataOptions options) // options constructor
			// .ctor(DataOptions<T> options) // typed options constructor

			// first we generate empty constructors and then add body to all of them as they will have same code for body

			if (context.Model.DataContext.HasDefaultConstructor)
				ctors.Add(constructors.New().SetModifiers(Modifiers.Public).Body());

			if (context.Model.DataContext.HasConfigurationConstructor)
			{
				var configurationParam = context.AST.Parameter(
					WellKnownTypes.System.String,
					context.AST.Name(CONTEXT_CONSTRUCTOR_CONFIGURATION_PARAMETER),
					CodeParameterDirection.In);

				ctors.Add(constructors
					.New()
						.Parameter(configurationParam)
						.SetModifiers(Modifiers.Public)
						.Base(configurationParam.Reference)
						.Body());
			}

			if (context.Model.DataContext.HasUntypedOptionsConstructor)
			{
				var optionsParam = context.AST.Parameter(
					WellKnownTypes.LinqToDB.Configuration.DataOptions,
					context.AST.Name(CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER),
					CodeParameterDirection.In);

				ctors.Add(constructors
					.New()
						.Parameter(optionsParam)
						.SetModifiers(Modifiers.Public)
						.Base(optionsParam.Reference)
						.Body());
			}

			if (context.Model.DataContext.HasTypedOptionsConstructor)
			{
				var typedOptionsParam = context.AST.Parameter(
					WellKnownTypes.LinqToDB.Configuration.DataOptionsWithType(context.MainContextClass.Type),
					context.AST.Name(CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER),
					CodeParameterDirection.In);

				ctors.Add(constructors
					.New()
						.Parameter(typedOptionsParam)
						.SetModifiers(Modifiers.Public)
						.Base(context.AST.Member(typedOptionsParam.Reference, WellKnownTypes.LinqToDB.Configuration.DataOptions_Options))
						.Body());
			}

			// partial init method, called by all constructors, which could be used by user to add
			// additional initialization logic
			var initDataContext = context.MainContextBuilder
				.Methods(true)
					.New(context.AST.Name(CONTEXT_INIT_METHOD))
						.SetModifiers(Modifiers.Partial);

			foreach (var body in ctors)
			{
				// each constructor calls:
				// InitSchemas(); // for context with additional schemas
				// InitDataContext(); // partial method for custom initialization

				if (initSchemasMethodName != null)
					body.Append(context.AST.Call(context.ContextReference, initSchemasMethodName));

				body.Append(context.AST.Call(context.ContextReference, initDataContext.Method.Name));
			}
		}
	}
}
