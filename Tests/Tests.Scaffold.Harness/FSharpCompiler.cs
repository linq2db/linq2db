using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;

using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Diagnostics;

using Microsoft.FSharp.Control;

using NUnit.Framework;

namespace Tests.Scaffold.Harness
{
	public static class FSharpCompiler
	{
		// FSharpChecker.Create has only optional (FSharpOption) parameters, which are not optional for C# callers
		static readonly Lazy<FSharpChecker> _checker = new(static () =>
		{
			var create = typeof(FSharpChecker).GetMethod(nameof(FSharpChecker.Create))!;
			return (FSharpChecker)create.Invoke(null, new object?[create.GetParameters().Length])!;
		});

		// concurrent Compile calls are not documented as supported
		static readonly Lock _sync = new();

		/// <summary>
		/// Compiles sources (in specified order) to assembly and loads it, or writes it to <paramref name="outputPath"/> without loading.
		/// </summary>
		/// <param name="nullable">Enables nullness checking (<c>--checknulls+</c>), as F# projects with <c>Nullable=enable</c>.</param>
		public static CompileResult Compile(string assemblyName, IReadOnlyList<SourceFile> sources, bool nullable, IReadOnlyCollection<string>? allowedWarnings = null, string? outputPath = null)
		{
			var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "scaffold-harness", assemblyName);
			Directory.CreateDirectory(directory);

			var files = sources
				.Select(s =>
				{
					var path = Path.Combine(directory, s.FileName);
					File.WriteAllText(path, s.Code);
					return path;
				})
				.ToList();

			var output = outputPath ?? Path.Combine(directory, assemblyName + ".dll");

			var argv = new List<string>
			{
				"fsc.exe",
				"-o:" + output,
				"--target:library",
				"--noframework",
				"--targetprofile:netcore",
				"--langversion:9.0",
				"--optimize-",
				"--debug-",
				"--nologo",
				nullable ? "--checknulls+" : "--checknulls-",
			};

			argv.AddRange(HostReferences.Paths.Select(static p => "-r:" + p));
			argv.AddRange(files);

			FSharpDiagnostic[]           fsDiagnostics;
			Microsoft.FSharp.Core.FSharpOption<Exception>? exception;

			lock (_sync)
				(fsDiagnostics, exception) = FSharpAsync.StartAsTask(_checker.Value.Compile(argv.ToArray(), null), null, null).GetAwaiter().GetResult();

			var diagnostics = fsDiagnostics
				.Where(static d => d.Severity == FSharpDiagnosticSeverity.Error || d.Severity == FSharpDiagnosticSeverity.Warning)
				.Select(d => new CompileDiagnostic(
					d.Severity == FSharpDiagnosticSeverity.Error ? CompileDiagnosticSeverity.Error : CompileDiagnosticSeverity.Warning,
					d.ErrorNumberText,
					d.Message,
					Path.GetFileName(d.FileName),
					d.StartLine,
					d.StartColumn + 1))
				.ToList();

			// compiler aborts with exception after reporting errors
			if (exception != null && diagnostics.TrueForAll(static d => d.Severity != CompileDiagnosticSeverity.Error))
				throw new InvalidOperationException("F# compiler failed", exception.Value);

			var emitted = diagnostics.TrueForAll(static d => d.Severity != CompileDiagnosticSeverity.Error) && File.Exists(output);

			var assembly = emitted && outputPath == null
				? AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(File.ReadAllBytes(output)))
				: null;

			return new CompileResult(sources, diagnostics, allowedWarnings ?? [], emitted, assembly);
		}
	}
}
