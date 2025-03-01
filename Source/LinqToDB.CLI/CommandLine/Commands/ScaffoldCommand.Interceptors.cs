using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using LinqToDB.Internal.Common;
using LinqToDB.Scaffold;
using LinqToDB.Tools;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

using Mono.TextTemplating;

namespace LinqToDB.CommandLine
{
	partial class ScaffoldCommand : CliCommand
	{
		private const string TEMPLATE_CLASS_NAME        = "CustomT4Scaffolder";
		private const string TEMPLATE_ASSEMBLY_NAME     = "_CustomT4ScaffolderAssembly";
		private const string INTERCEPTORS_ASSEMBLY_NAME = "_CustomT4InterceptorsAssembly";

		/// <summary>
		/// Load interceptors from file at provided path. It could be assembly with interceptors if path must ends with .dll extension
		/// or T4 template otherwise.
		/// </summary>
		/// <param name="interceptorsPath">Path to interceptors file.</param>
		/// <returns>Method execution status and interceptors instance on success.</returns>
		private (int resultCode, ScaffoldInterceptors? interceptors) LoadInterceptors(string interceptorsPath, ScaffoldOptions options)
		{
			if (Path.GetExtension(interceptorsPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
				return LoadInterceptorsFromAssembly(interceptorsPath, options);

			return LoadInterceptorsFromT4(interceptorsPath, options);
		}

		/// <summary>
		/// Loads interceptors from assembly from specified path.
		/// </summary>
		/// <param name="assemblyPath">Path to assembly with interceptors.</param>
		/// <returns>Status of operation and loaded interceptors (on failure).</returns>
		private (int resultCode, ScaffoldInterceptors? interceptors) LoadInterceptorsFromAssembly(string assemblyPath, ScaffoldOptions options)
		{
			// as loaded assembly could have additional sideload references, we should try to handle them
			var assemblyFolder = Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;

			// try to load side-assemblies from same folder
			// TODO: Verbose logging
			Console.WriteLine($"AssemblyResolve path: {assemblyFolder}");

			Assembly assembly;
			try
			{
				assembly = Assembly.LoadFrom(assemblyPath);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to load assembly with interceptors from '{assemblyPath}': {ex}");
				return (StatusCodes.INVALID_ARGUMENTS, null);
			}

			SetupInterceptorsDependencyResolver(assemblyFolder, assembly);

			return LoadInterceptorsFromAssembly(assemblyPath, assembly, options);
		}

		private static void SetupInterceptorsDependencyResolver(string assemblyFolder, Assembly interceptorsAssembly)
		{
			if (Directory.GetFiles(assemblyFolder, "*.deps.json").Length > 0)
			{
				// this method loads assemblies from both assembly folder and nuget cache and requires deps.json file
				// see https://github.com/dotnet/runtime/issues/18527#issuecomment-611499261
				var dependencyContext = DependencyContext.Load(interceptorsAssembly)
					?? throw new InvalidOperationException($"DependencyContext.Load cannot load interceptor assembly");

				var resolver          = new ICompilationAssemblyResolver[]
				{
					new AppBaseCompilationAssemblyResolver(assemblyFolder),
					new ReferenceAssemblyPathResolver(),
					new PackageCompilationAssemblyResolver()
				};
				var assemblyResolver  = new CompositeCompilationAssemblyResolver(resolver);
				var loadContext       = AssemblyLoadContext.GetLoadContext(interceptorsAssembly);

				loadContext!.Resolving += (AssemblyLoadContext context, AssemblyName name) =>
				{
					bool NamesMatch(RuntimeLibrary runtime)
					{
						var res = string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase);
						if (!res)
						{
							foreach (var group in runtime.RuntimeAssemblyGroups)
							{
								foreach (var l in group.RuntimeFiles)
								{
									if (Path.GetFileNameWithoutExtension(l.Path) == name.Name)
										return true;
								}
							}
						}

						return res;
					}

					var library = dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);
					if (library == null)
						return null;

					var wrapper = new CompilationLibrary(
						library.Type,
						library.Name,
						library.Version,
						library.Hash,
						library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
						library.Dependencies,
						library.Serviceable);

					var assemblies = new List<string>();
					assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
					var assembly   = assemblies.FirstOrDefault(a => Path.GetFileNameWithoutExtension(a) == name.Name);

					Console.WriteLine($"Load from: {assembly}");
					return assembly == null ? null : Assembly.LoadFile(assembly);
				};
			}
			else
			{
				// alternative approach supports loading from assembly folder without deps.json file
				AssemblyLoadContext.Default.Resolving += (context, name) =>
				{
					// TODO: verbose logging
					Console.WriteLine($"AssemblyResolve: {name}");

					// as we know only assembly name, not file name (written in deps.json) - we try to guess it
					var probePath1 = Path.Combine(assemblyFolder, name.Name + ".dll");
					var probePath2 = Path.Combine(assemblyFolder, name.Name + ".exe");

					if (File.Exists(probePath1))
					{
						Console.WriteLine($"Found at: {probePath1}");
						return Assembly.LoadFile(probePath1);
					}

					if (File.Exists(probePath2))
					{
						Console.WriteLine($"Found at: {probePath2}");
						return Assembly.LoadFile(probePath2);
					}

					return null;
				};
			}
		}

		/// <summary>
		/// Locate and create instance of interceptors from <paramref name="assembly"/>.
		/// </summary>
		/// <param name="sourcePath">Interceptors source path (T4 template path or assembly path) for logging purposes.</param>
		/// <param name="assembly">Assembly with interceptors class.</param>
		/// <returns>Method execution status and interceptors instance on success.</returns>
		private static (int resultCode, ScaffoldInterceptors? interceptors) LoadInterceptorsFromAssembly(string sourcePath, Assembly assembly, ScaffoldOptions options)
		{
			Type? interceptorsType = null;
			foreach (var type in assembly.GetTypes())
			{
				if (Inherits(type, typeof(ScaffoldInterceptors)))
				{
					// only one interceptors class allowed
					if (interceptorsType != null)
					{
						Console.Error.WriteLine($"'{sourcePath}' contains multiple interceptor types: {interceptorsType}, {interceptorsType}");
						return (StatusCodes.EXPECTED_ERROR, null);
					}

					interceptorsType = type;
				}
			}

			// interceptors class not found?
			if (interceptorsType == null)
			{
				Console.Error.WriteLine($"Cannot find interceptor class in '{sourcePath}'");
				return (StatusCodes.EXPECTED_ERROR, null);
			}

			// try options constructor first
			object[]? args = null;
			var ctor = interceptorsType.GetConstructor(new[] { typeof(ScaffoldOptions) });
			if (ctor == null)
			{
				// try default constructor
				ctor = interceptorsType.GetConstructor(Array.Empty<Type>());
			}
			else
			{
				args = new[] { options };
			}

			if (ctor == null)
			{
				Console.Error.WriteLine($"Interceptor class '{interceptorsType}' missing default constrtuctor");
				return (StatusCodes.EXPECTED_ERROR, null);
			}

			try
			{
				return (StatusCodes.SUCCESS, ActivatorExt.CreateInstance<ScaffoldInterceptors>(interceptorsType, args));
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to create instance of interceptor '{interceptorsType}': {ex}");
				return (StatusCodes.EXPECTED_ERROR, null);
			}
		}

		/// <summary>
		/// Check that type <paramref name="type"/> inherited from <paramref name="baseType"/>.
		/// </summary>
		/// <param name="type">Type to check for inheritance.</param>
		/// <param name="baseType">Type that should present in inheritance chain for <paramref name="type"/>.</param>
		/// <returns><c>true</c> if <paramref name="type"/> inherits from <paramref name="baseType"/> and <c>false</c> otherwise.</returns>
		private static bool Inherits(Type type, Type baseType)
		{
			while (type.BaseType != null)
			{
				if (type.BaseType == baseType)
					return true;

				type = type.BaseType;
			}

			return false;
		}

		/// <summary>
		/// Compile and execute T4 template, provided by user.
		/// </summary>
		/// <param name="t4templatePath">Path to T4 template.</param>
		/// <returns>Template processing status and interceptors instance on success.</returns>
		private (int resultCode, ScaffoldInterceptors? interceptors) LoadInterceptorsFromT4(string t4templatePath, ScaffoldOptions options)
		{
			// load template source and assembly references from T4 file
			var status = PreprocessTemplate(t4templatePath, out var refs, out var templateCode, out var imports);
			if (status != StatusCodes.SUCCESS)
				return (status, null);

			// compile template and load interceptors assembly
			(status, var assembly) = CompileTemplateAndLoadAssembly(templateCode, refs, imports);
			if (status != StatusCodes.SUCCESS)
				return (status, null);

			// instantiate interceptors instance
			return LoadInterceptorsFromAssembly(t4templatePath, assembly!, options);
		}

		/// <summary>
		/// Execute T4 template code to generate interceptors code and load them into in-memory assembly.
		/// </summary>
		/// <param name="templateCode">T4 template code.</param>
		/// <param name="references">Additional assembly references for compilation of template and interceptors.</param>
		/// <param name="imports">Imports (usings) from T4 template.</param>
		/// <returns>Method execution status and interceptors assembly on success.</returns>
		private (int resultCode, Assembly? assembly) CompileTemplateAndLoadAssembly(string templateCode, IReadOnlyCollection<MetadataReference> references, IReadOnlyCollection<string> imports)
		{
			// load T4 template as in-memory assembly
			var (status, templateAsembly) = CompileAndLoadAssembly(TEMPLATE_ASSEMBLY_NAME, templateCode, references);

			// find and instantiate template host class
			var type = templateAsembly!.GetTypes().FirstOrDefault(type => type.Name == TEMPLATE_CLASS_NAME);
			if (type == null)
			{
				Console.Error.WriteLine("Cannot find template in T4 file. Make sure you didn't changed @template directive");
				return (StatusCodes.EXPECTED_ERROR, null);
			}

			var instance = ActivatorExt.CreateInstance(type) as LinqToDBHost;
			if (instance == null)
			{
				Console.Error.WriteLine("Cannot create template object. Make sure you didn't changed @template directive");
				return (StatusCodes.EXPECTED_ERROR, null);
			}

			// generate code of interceptors from tempate
			var interceptorsCode = instance.TransformText();

			// add imports
			interceptorsCode = string.Join(string.Empty, imports.Select(i => $"using {i};\r\n")) + interceptorsCode;

			// compile interceptors into in-memory assembly
			(status, var assembly) = CompileAndLoadAssembly(INTERCEPTORS_ASSEMBLY_NAME, interceptorsCode, references);
			if (status != StatusCodes.SUCCESS)
				return (status, null);

			return (StatusCodes.SUCCESS, assembly!);
		}

		/// <summary>
		/// Compiles provided source code into in-memory assembly using provided assembly references and assembly name.
		/// </summary>
		/// <param name="assemblyName">Name of in-memory assembly.</param>
		/// <param name="sourceCode">Source code to compile.</param>
		/// <param name="references">Additional compilation references.</param>
		/// <returns>Method execution status and loaded assembly on success.</returns>
		private (int resultCode, Assembly? assembly) CompileAndLoadAssembly(string assemblyName, string sourceCode, IReadOnlyCollection<MetadataReference> references)
		{
			// parse source code by roslyn
			var compiledCode = new List<SyntaxTree>();
			var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.None);
			compiledCode.Add(CSharpSyntaxTree.ParseText(sourceCode, parseOptions));

			// create compilation unit
			var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);
			var compilation        = CSharpCompilation.Create(assemblyName, compiledCode, references, compilationOptions);

			// compile into memory
			using var ms    = new MemoryStream();
			var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
			var result      = compilation.Emit(ms, options: emitOptions);

			if (!result.Success)
			{
				// TODO: log sources in verbose mode
				Console.WriteLine(sourceCode);

				Console.Error.WriteLine("T4 compilation failed:");
				foreach (var diag in result.Diagnostics)
					Console.Error.WriteLine(FormattableString.Invariant($"\t{diag}"));

				return (StatusCodes.EXPECTED_ERROR, null);
			}

			// load assembly from memory stream
			ms.Position = 0;
			return (StatusCodes.SUCCESS, AssemblyLoadContext.Default.LoadFromStream(ms));
		}

		/// <summary>
		/// Extracts template source and assembly  references information from T4 template file.
		/// </summary>
		/// <param name="t4templatePath">Path to T4 template.</param>
		/// <param name="references">List of assembly references for Roslyn.</param>
		/// <param name="templateCode">Template source code.</param>
		/// <param name="imports">List of imports (usings) from T4 template.</param>
		/// <returns>Status of method execution.</returns>
		private int PreprocessTemplate(string t4templatePath, out IReadOnlyCollection<MetadataReference> references, out string templateCode, out IReadOnlyList<string> imports)
		{
			references = Array.Empty<MetadataReference>();
			imports    = Array.Empty<string>();

			var generator    = new TemplateGenerator();
			var templateText = File.ReadAllText(t4templatePath);
			var template     = generator.ParseTemplate(Path.GetFileName(t4templatePath), templateText);

			// parse template by mono.t4
			if (!generator.PreprocessTemplate(null, TEMPLATE_CLASS_NAME, null, templateText, out var language, out var referencesFromTemplate, out templateCode))
			{
				Console.Error.WriteLine("T4 template pre-processing failed:");
				foreach (CompilerError? error in generator.Errors)
				{
					if (error != null)
						Console.Error.WriteLine(FormattableString.Invariant($"\t{error.FileName} ({error.Line}, {error.Column}): {error.ErrorText}"));
				}

				return StatusCodes.T4_ERROR;
			}

			// make some basic assertions
			if (language != "C#")
			{
				Console.Error.WriteLine($"T4 template language should be C# but got '{language}'");
				return StatusCodes.T4_ERROR;
			}

			// prepare references for compilation
			var referencesList = new List<MetadataReference>();

			// normalize assembly pathes
			for (var i = 0; i < referencesFromTemplate.Length; i++)
				referencesFromTemplate[i] = Path.GetFullPath(referencesFromTemplate[i]);

			// imports from T4
			referencesList.AddRange(referencesFromTemplate.Select(file => MetadataReference.CreateFromFile(file)));

			// default linq2db imports
			// current tool (for host class)
			referencesList.Add(MetadataReference.CreateFromFile(typeof(LinqToDBHost).Assembly.Location));
			// linq2db.Scaffold
			referencesList.Add(MetadataReference.CreateFromFile(typeof(ScaffoldOptions).Assembly.Location));
			// linq2db.Tools
			referencesList.Add(MetadataReference.CreateFromFile(typeof(MappingSchemaExtensions).Assembly.Location));
			// linq2db
			referencesList.Add(MetadataReference.CreateFromFile(typeof(ProviderName).Assembly.Location));

			// reference all current runtime assemblies to not force user to specify a lot of small stuff
			//
			// get path to framework folder
			var fwPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

			// reference netstandard + System*
			referencesList.Add(MetadataReference.CreateFromFile(Path.Combine(fwPath, "netstandard.dll")));
			foreach (var asmName in Directory.GetFiles(fwPath, "System*.dll"))
			{
				if (!asmName.Contains(".Native.", StringComparison.Ordinal))
					referencesList.Add(MetadataReference.CreateFromFile(Path.Combine(fwPath, asmName)));
			}

			var usings = new List<string>();
			foreach (var directive in template.Directives)
			{
				if (directive.Name.ToLowerInvariant() == "import")
					usings.Add(directive.Extract("namespace"));
			}

			references = referencesList;
			imports    = usings;

			// register assembly resolver for loaded template
			RegisterAssemblyResolver(referencesFromTemplate);

			return StatusCodes.SUCCESS;
		}

		private void RegisterAssemblyResolver(string[] assemblyFiles)
		{
			if (assemblyFiles.Length == 0)
				return;

			var assemblyLookup = assemblyFiles.Distinct().ToDictionary(_ => Path.GetFileNameWithoutExtension(Path.GetFileName(_)));

			AssemblyLoadContext.Default.Resolving += (context, name) =>
			{
				// TODO: verbose logging
				Console.WriteLine($"AssemblyResolve: {name}");

				if (assemblyLookup.TryGetValue(name.Name!, out var path))
					return Assembly.LoadFile(path);

				return null;
			};
		}
	}
}
