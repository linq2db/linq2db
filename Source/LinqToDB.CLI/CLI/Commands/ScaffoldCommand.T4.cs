using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using LinqToDB.Scaffold;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.TextTemplating;

namespace LinqToDB.CLI
{
	partial class ScaffoldCommand : CliCommand
	{
		private const string TEMPLATE_CLASS_NAME    = "CustomT4Scaffolder";
		private const string TEMPLATE_ASSEMBLY_NAME = "_CustomT4ScaffolderAssembly";

		/// <summary>
		/// Compile and execute T4 template, provided by user.
		/// </summary>
		/// <param name="options">Scaffold options to update with customization logic.</param>
		/// <param name="t4templatePath">Path to T4 template.</param>
		/// <returns>Template execution status.</returns>
		private int ApplyTemplate(ScaffoldOptions options, string t4templatePath)
		{
			var status = PreprocessTemplate(t4templatePath, out var refs, out var templateCode);
			if (status != StatusCodes.SUCCESS)
				return status;

			var host = CompileAndLoadHost(refs, templateCode);
			if (host == null)
				return StatusCodes.T4_ERROR;

			// set options before template execution
			host.Options = options;
			// execute template to modify options with custom handlers
			host.TransformText();

			return StatusCodes.SUCCESS;
		}

		private LinqToDBHost? CompileAndLoadHost(string[] referencesFromTemplate, string templateCode)
		{
			// parse template code
			var compiledCode = new List<SyntaxTree>();
			var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.None);
			compiledCode.Add(CSharpSyntaxTree.ParseText(templateCode, parseOptions));

			// prepare references for compilation
			var references = new List<MetadataReference>();

			// imports from T4
			references.AddRange(referencesFromTemplate.Select(file => MetadataReference.CreateFromFile(file)));

			// default linq2db imports
			// linq2db.Tools
			references.Add(MetadataReference.CreateFromFile(typeof(ScaffoldOptions).Assembly.Location));
			// current utility
			references.Add(MetadataReference.CreateFromFile(typeof(ScaffoldCommand).Assembly.Location));
			// linq2db
			references.Add(MetadataReference.CreateFromFile(typeof(ProviderName).Assembly.Location));

			// reference all current runtime assemblies to not force user to specify a lot of small stuff
			// get path to framework folder
			var fwPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

			// reference netstandard + System*
			references.Add(MetadataReference.CreateFromFile(Path.Combine(fwPath, "netstandard.dll")));
			foreach (var asmName in Directory.GetFiles(fwPath, "System*.dll"))
				references.Add(MetadataReference.CreateFromFile(Path.Combine(fwPath, asmName)));

			// create compilation unit
			var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Warnings);
			var compilation        = CSharpCompilation.Create(TEMPLATE_ASSEMBLY_NAME, compiledCode, references, compilationOptions);

			// compile into memory
			using var ms    = new MemoryStream();
			var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
			var result      = compilation.Emit(ms, options: emitOptions);

			if (!result.Success)
			{
				Console.Error.WriteLine("T4 template compilation failed:");
				foreach (var diag in result.Diagnostics)
					Console.Error.WriteLine($"\t{diag}");

				return null;
			}

			// load assembly from memory
			ms.Position  = 0;
			var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

			// find and instantiate template class
			var type = assembly!.GetType(TEMPLATE_CLASS_NAME);
			if (type == null)
			{
				Console.Error.WriteLine("Cannot find template in T4 file. Make sure you didn't changed @template directive");
				return null;
			}

			var instance = Activator.CreateInstance(type) as LinqToDBHost;
			if (instance == null)
			{
				Console.Error.WriteLine("Cannot create template object. Make sure you didn't changed @template directive");
				return null;
			}

			return instance;
		}

		private int PreprocessTemplate(string t4templatePath, out string[] references, out string templateCode)
		{
			var generator = new TemplateGenerator();
			if (!generator.PreprocessTemplate(null, TEMPLATE_CLASS_NAME, null, File.ReadAllText(t4templatePath), out var language, out references, out templateCode))
			{
				Console.Error.WriteLine("T4 template pre-processing failed:");
				foreach (CompilerError? error in generator.Errors)
				{
					if (error != null)
						Console.Error.WriteLine($"\t{error.FileName} ({error.Line}, {error.Column}): {error.ErrorText}");
				}

				return StatusCodes.T4_ERROR;
			}

			if (language != "C#")
			{
				Console.Error.WriteLine($"T4 template language should be C# but got '{language}'");
				return StatusCodes.T4_ERROR;
			}

			return StatusCodes.SUCCESS;
		}
	}
}
