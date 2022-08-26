namespace LinqToDB.DataModel
{
	using CodeModel;

	// contains generation logic for data context class supplementary code like constructors
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates data context constructors.
		/// </summary>
		/// <param name="contextBuilder">Data context class builder.</param>
		/// <param name="initSchemasMethodName">(Optional) additional schemas init method name.</param>
		private void BuildDataContextConstructors(
			ClassBuilder    contextBuilder,
			CodeIdentifier? initSchemasMethodName)
		{
			var constructors = contextBuilder.Constructors();

			var ctors = new List<BlockBuilder>();

			// based on selected constructors set we generate (or not) following constructors:
			// .ctor() // default constructor
			// .ctor(string configuration) // constructor with connection configuration name parameter
			// .ctor(LinqToDBConnectionOptions options) // options constructor
			// .ctor(LinqToDBConnectionOptions<T> options) // typed options constructor

			// first we generate empty constructors and then add body to all of them as they will have same code for body

			if (_dataModel.DataContext.HasDefaultConstructor)
				ctors.Add(constructors.New().SetModifiers(Modifiers.Public).Body());
			if (_dataModel.DataContext.HasConfigurationConstructor)
			{
				var configurationParam = AST.Parameter(
					WellKnownTypes.System.String,
					AST.Name(CONTEXT_CONSTRUCTOR_CONFIGURATION_PARAMETER),
					CodeParameterDirection.In);

				ctors.Add(constructors
					.New()
						.Parameter(configurationParam)
						.SetModifiers(Modifiers.Public)
						.Base(configurationParam.Reference)
						.Body());
			}
			if (_dataModel.DataContext.HasUntypedOptionsConstructor)
			{
				var optionsParam = AST.Parameter(
					WellKnownTypes.LinqToDB.Configuration.LinqToDBConnectionOptions,
					AST.Name(CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER),
					CodeParameterDirection.In);

				ctors.Add(constructors
					.New()
						.Parameter(optionsParam)
						.SetModifiers(Modifiers.Public)
						.Base(optionsParam.Reference)
						.Body());
			}
			if (_dataModel.DataContext.HasTypedOptionsConstructor)
			{
				var typedOptionsParam = AST.Parameter(
					WellKnownTypes.LinqToDB.Configuration.LinqToDBConnectionOptionsWithType(contextBuilder.Type.Type),
					AST.Name(CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER),
					CodeParameterDirection.In);

				ctors.Add(constructors
					.New()
						.Parameter(typedOptionsParam)
						.SetModifiers(Modifiers.Public)
						.Base(typedOptionsParam.Reference)
						.Body());
			}

			// partial init method, called by all constructors, which could be used by user to add
			// additional initialization logic
			var initDataContext = contextBuilder
				.Methods(true)
					.New(AST.Name(CONTEXT_INIT_METHOD))
						.SetModifiers(Modifiers.Partial);

			foreach (var body in ctors)
			{
				// each constructor calls:
				// InitSchemas(); // for context with additional schemas
				// InitDataContext(); // partial method for custom initialization

				if (initSchemasMethodName != null)
					body.Append(AST.Call(contextBuilder.Type.This, initSchemasMethodName));

				body.Append(AST.Call(contextBuilder.Type.This, initDataContext.Method.Name));
			}
		}
	}
}
