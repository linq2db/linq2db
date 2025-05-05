using System.Collections.Generic;

using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
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
			var constructors = context.MainDataContextConstructors;

			var ctors = new List<BlockBuilder>();

			// based on selected constructors set we generate (or not) following constructors:
			// .ctor() // default constructor
			// .ctor(string configuration) // constructor with connection configuration name parameter
			// .ctor(DataOptions options) // options constructor
			// .ctor(DataOptions<T> options) // typed options constructor

			// first we generate empty constructors and then add body to all of them as they will have same code for body

			if (context.Model.DataContext.HasDefaultConstructor)
			{
				var ctor = context.MainDataContextConstructors.New().SetModifiers(Modifiers.Public);

				// base(new DataOptions().UseMappingSchema(ContextSchema))
				if (context.HasContextMappingSchema)
					ctor.Base(
						context.AST.ExtCall(
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							WellKnownTypes.LinqToDB.Configuration.DataOptionsExtensions_UseMappingSchema,
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							context.AST.New(WellKnownTypes.LinqToDB.Configuration.DataOptions),
							context.ContextMappingSchema));

				ctors.Add(ctor.Body());
			}

			if (context.Model.DataContext.HasConfigurationConstructor)
			{
				var configurationParam = context.AST.Parameter(
					WellKnownTypes.System.String,
					context.AST.Name(DataModelConstants.CONTEXT_CONSTRUCTOR_CONFIGURATION_PARAMETER),
					CodeParameterDirection.In);

				var ctor = context.MainDataContextConstructors.New().Parameter(configurationParam).SetModifiers(Modifiers.Public);

				// base(new DataOptions().UseConfiguration(configuration, ContextSchema))
				if (context.HasContextMappingSchema)
					ctor.Base(
						context.AST.ExtCall(
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							WellKnownTypes.LinqToDB.Configuration.DataOptionsExtensions_UseConfiguration,
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							context.AST.New(WellKnownTypes.LinqToDB.Configuration.DataOptions),
							configurationParam.Reference,
							context.ContextMappingSchema));
				else
					// base(configuration)
					ctor.Base(configurationParam.Reference);

				ctors.Add(ctor.Body());
			}

			if (context.Model.DataContext.HasUntypedOptionsConstructor)
			{
				var optionsParam = context.AST.Parameter(
					WellKnownTypes.LinqToDB.Configuration.DataOptions,
					context.AST.Name(DataModelConstants.CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER),
					CodeParameterDirection.In);

				var ctor = context.MainDataContextConstructors.New().Parameter(optionsParam).SetModifiers(Modifiers.Public);

				// base(options.UseMappingSchema(options.ConnectionOptions.MappingSchema == null ? ContextSchema : MappingSchema.CombineSchemas(options.ConnectionOptions.MappingSchema, ContextSchema)))
				if (context.HasContextMappingSchema)
				{
					var existingSchema = context.AST.Member(
						context.AST.Member(
							optionsParam.Reference,
							WellKnownTypes.LinqToDB.Configuration.DataOptions_ConnectionOptions),
						WellKnownTypes.LinqToDB.Configuration.ConnectionOptions_MappingSchema);

					ctor.Base(
						context.AST.ExtCall(
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							WellKnownTypes.LinqToDB.Configuration.DataOptionsExtensions_UseMappingSchema,
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							optionsParam.Reference,
							context.AST.IIF(
								context.AST.Equal(existingSchema, context.AST.Null(WellKnownTypes.LinqToDB.Mapping.MappingSchema, true)),
								context.ContextMappingSchema,
								context.AST.Call(
									new CodeTypeReference(WellKnownTypes.LinqToDB.Mapping.MappingSchema),
									WellKnownTypes.LinqToDB.Mapping.MappingSchema_CombineSchemas,
									WellKnownTypes.LinqToDB.Mapping.MappingSchema,
									existingSchema,
									context.ContextMappingSchema))));
				}
				else
					// base(options)
					ctor.Base(optionsParam.Reference);

				ctors.Add(ctor.Body());
			}

			if (context.Model.DataContext.HasTypedOptionsConstructor)
			{
				var typedOptionsParam = context.AST.Parameter(
					WellKnownTypes.LinqToDB.Configuration.DataOptionsWithType(context.MainDataContext.Type.Type),
					context.AST.Name(DataModelConstants.CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER),
					CodeParameterDirection.In);

				var optionsRef = context.AST.Member(typedOptionsParam.Reference, WellKnownTypes.LinqToDB.Configuration.DataOptions_Options);
				var ctor       = context.MainDataContextConstructors.New().Parameter(typedOptionsParam).SetModifiers(Modifiers.Public);

				// base(options.Options.UseMappingSchema(options.Options.ConnectionOptions.MappingSchema == null ? ContextSchema : MappingSchema.CombineSchemas(options.Options.ConnectionOptions.MappingSchema, ContextSchema)))
				if (context.HasContextMappingSchema)
				{
					var existingSchema = context.AST.Member(
						context.AST.Member(
							optionsRef,
							WellKnownTypes.LinqToDB.Configuration.DataOptions_ConnectionOptions),
						WellKnownTypes.LinqToDB.Configuration.ConnectionOptions_MappingSchema);

					ctor.Base(
						context.AST.ExtCall(
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							WellKnownTypes.LinqToDB.Configuration.DataOptionsExtensions_UseMappingSchema,
							WellKnownTypes.LinqToDB.Configuration.DataOptions,
							optionsRef,
							context.AST.IIF(
								context.AST.Equal(existingSchema, context.AST.Null(WellKnownTypes.LinqToDB.Mapping.MappingSchema, true)),
								context.ContextMappingSchema,
								context.AST.Call(
									new CodeTypeReference(WellKnownTypes.LinqToDB.Mapping.MappingSchema),
									WellKnownTypes.LinqToDB.Mapping.MappingSchema_CombineSchemas,
									WellKnownTypes.LinqToDB.Mapping.MappingSchema,
									existingSchema,
									context.ContextMappingSchema))));
				}
				else
					// base(options.Options)
					ctor.Base(optionsRef);

				ctors.Add(ctor.Body());
			}

			// partial init method, called by all constructors, which could be used by user to add
			// additional initialization logic
			var initDataContext = !context.Options.GenerateInitDataContextMethod
				? null
				: context.MainDataContextPartialMethods
					.New(context.AST.Name(DataModelConstants.CONTEXT_INIT_METHOD))
						.SetModifiers(Modifiers.Partial);

			foreach (var body in ctors)
			{
				// each constructor calls:
				// InitSchemas(); // for context with additional schemas
				// InitDataContext(); // partial method for custom initialization

				if (initSchemasMethodName != null)
					body.Append(context.AST.Call(context.ContextReference, initSchemasMethodName));

				if (initDataContext != null)
					body.Append(context.AST.Call(context.ContextReference, initDataContext.Method.Name));
			}
		}
	}
}
