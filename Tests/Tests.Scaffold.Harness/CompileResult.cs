using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tests.Scaffold.Harness
{
	public enum CompileDiagnosticSeverity
	{
		Warning,
		Error,
	}

	public sealed record CompileDiagnostic(CompileDiagnosticSeverity Severity, string Id, string Message, string? FileName, int Line, int Column)
	{
		public override string ToString() => $"{FileName}({Line},{Column}): {Severity} {Id}: {Message}";
	}

	public sealed record SourceFile(string FileName, string Code);

	/// <summary>
	/// Compilation outcome. Warnings, not listed in allowed warnings, fail compilation.
	/// </summary>
	public sealed class CompileResult(IReadOnlyList<SourceFile> sources, IReadOnlyList<CompileDiagnostic> diagnostics, IReadOnlyCollection<string> allowedWarnings, bool emitted, Assembly? assembly)
	{
		public IReadOnlyList<SourceFile>        Sources     { get; } = sources;
		public IReadOnlyList<CompileDiagnostic> Diagnostics { get; } = diagnostics;

		/// <summary>
		/// Loaded assembly. <see langword="null"/> when compilation failed or assembly was written to file.
		/// </summary>
		public Assembly? Assembly { get; } = assembly;

		public IReadOnlyList<CompileDiagnostic> Failures { get; } = diagnostics
			.Where(d => d.Severity == CompileDiagnosticSeverity.Error || !allowedWarnings.Contains(d.Id))
			.ToList();

		public bool Success => emitted && Failures.Count == 0;

		/// <summary>
		/// Diagnostics with source listing for test failure message.
		/// </summary>
		public string Report()
		{
			var sb = new StringBuilder();

			sb.AppendLine("Compilation failed:");

			foreach (var diagnostic in Failures)
				sb.Append('\t').AppendLine(diagnostic.ToString());

			foreach (var source in Sources)
			{
				sb.AppendLine().Append("// ").AppendLine(source.FileName);

				var lines = source.Code.Split('\n');
				for (var i = 0; i < lines.Length; i++)
					sb.Append((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(5)).Append(": ").AppendLine(lines[i].TrimEnd('\r'));
			}

			return sb.ToString();
		}
	}
}
