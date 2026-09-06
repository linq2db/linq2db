using System;
using System.Text;

namespace Tests
{
	/// <summary>
	/// One-line, ASCII, machine-readable record of what a test gated by <see cref="ActiveIssueAttribute"/> actually
	/// did when the gate was lifted for a triage sweep. Emitted as the test's failure message, so it survives into
	/// the console summary and the <c>.trx</c> without any extra CI plumbing.
	/// <para>
	/// Deliberately one line and free of stack traces: a full-matrix sweep emits one record per gated test case per
	/// provider, and a multi-line record would add tens of thousands of lines to every CI step log.
	/// </para>
	/// </summary>
	public static class ActiveIssueSentinel
	{
		/// <summary>Line prefix, carrying a format version so a parser can reject records it does not understand.</summary>
		public const string Prefix = "##L2DB-AI|1|";

		const char Separator        = '|';
		const int  MaxMessageLength = 500;

		/// <summary>
		/// Builds the sentinel line. <paramref name="fullName"/> carries the class, which neither the console output
		/// nor the <c>.trx</c> <c>testName</c> attribute does — without it a harvested row cannot be mapped back to
		/// the source site that owns the attribute.
		/// </summary>
		/// <param name="fullName">NUnit's <c>Test.FullName</c>, e.g. <c>Tests.Linq.FooTests.Bar("SQLite.MS")</c>.</param>
		/// <param name="provider">Provider configuration the case ran against, or <see langword="null"/> when the test takes no data-source parameter.</param>
		/// <param name="isRemote">Whether the case ran over the remote (<c>LinqService</c>) transport.</param>
		/// <param name="passed">Whether the test passed once the gate was lifted.</param>
		/// <param name="errorType">Full name of the exception type that surfaced, or <see langword="null"/> for an assertion failure or a pass.</param>
		/// <param name="message">The original result message; escaped, flattened and capped.</param>
		public static string Format(string fullName, string? provider, bool isRemote, bool passed, string? errorType, string? message)
		{
			var sb = new StringBuilder(Prefix);

			Append(sb, fullName);
			sb.Append(Separator);
			Append(sb, provider ?? "-");
			sb.Append(Separator);
			sb.Append(isRemote ? '1' : '0');
			sb.Append(Separator);
			sb.Append(passed ? "PASSED" : "FAILED");
			sb.Append(Separator);
			Append(sb, errorType ?? "-");
			sb.Append(Separator);
			Append(sb, Cap(message));

			return sb.ToString();
		}

		/// <summary>
		/// Parses a line produced by <see cref="Format"/>. The PowerShell harvester mirrors this method; keeping a
		/// managed implementation beside it is what makes the wire format testable at all, since the code that emits
		/// it during a sweep is a temporary local edit.
		/// </summary>
		/// <returns><see langword="true"/> when <paramref name="line"/> is a well-formed record of a known version.</returns>
		public static bool TryParse(string? line, out ActiveIssueSentinelRecord? record)
		{
			record = null;

			if (line == null)
				return false;

			var start = line.IndexOf(Prefix, StringComparison.Ordinal);

			if (start < 0)
				return false;

			var parts = line.Substring(start + Prefix.Length).Split(Separator);

			if (parts.Length != 6)
				return false;

			var provider  = Unescape(parts[1]);
			var errorType = Unescape(parts[4]);

			record = new ActiveIssueSentinelRecord(
				Unescape(parts[0]),
				provider  == "-" ? null : provider,
				parts[2]  == "1",
				parts[3]  == "PASSED",
				errorType == "-" ? null : errorType,
				Unescape(parts[5]));

			return true;
		}

		/// <summary>
		/// Recovers the exception type from an NUnit result message, which renders an unhandled exception as
		/// <c>"{TypeFullName} : {message}"</c>. Returns <see langword="null"/> for an assertion failure, whose
		/// message carries no type — which is exactly the signal that a site's expected failure is wrong results
		/// rather than a throw.
		/// </summary>
		public static string? ExtractErrorType(string? message)
		{
			if (message == null)
				return null;

			var separator = message.IndexOf(" : ", StringComparison.Ordinal);

			if (separator <= 0)
				return null;

			var candidate = message.Substring(0, separator);

			// A type name has no whitespace and is namespace-qualified; anything else is assertion prose that
			// happens to contain " : ".
			if (candidate.IndexOf('.') < 0)
				return null;

			foreach (var c in candidate)
				if (char.IsWhiteSpace(c))
					return null;

			return candidate;
		}

		static string? Cap(string? message)
		{
			if (message == null || message.Length <= MaxMessageLength)
				return message;

			return message.Substring(0, MaxMessageLength) + "...";
		}

		// '%' first: it is the escape character, so escaping it after the others would double-escape their output.
		static void Append(StringBuilder sb, string? value)
		{
			if (value == null)
				return;

			foreach (var c in value)
			{
				switch (c)
				{
					case '%'      : sb.Append("%25"); break;
					case Separator: sb.Append("%7C"); break;
					case '\r'     : sb.Append("%0D"); break;
					case '\n'     : sb.Append("%0A"); break;
					// The record is read out of CI logs whose encoding we do not control, so anything outside
					// printable ASCII is folded rather than risking a mangled byte splitting the line.
					default       : sb.Append(c < ' ' || c > '~' ? '?' : c); break;
				}
			}
		}

		static string Unescape(string value)
		{
			if (value.IndexOf('%') < 0)
				return value;

			var sb = new StringBuilder(value.Length);

			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] == '%' && i + 2 < value.Length)
				{
					var hex = value.Substring(i + 1, 2);

					switch (hex)
					{
						case "25": sb.Append('%');  i += 2; continue;
						case "7C": sb.Append('|');  i += 2; continue;
						case "0D": sb.Append('\r'); i += 2; continue;
						case "0A": sb.Append('\n'); i += 2; continue;
					}
				}

				sb.Append(value[i]);
			}

			return sb.ToString();
		}
	}

	/// <summary>A parsed <see cref="ActiveIssueSentinel"/> record.</summary>
	public sealed class ActiveIssueSentinelRecord
	{
		internal ActiveIssueSentinelRecord(string fullName, string? provider, bool isRemote, bool passed, string? errorType, string? message)
		{
			FullName  = fullName;
			Provider  = provider;
			IsRemote  = isRemote;
			Passed    = passed;
			ErrorType = errorType;
			Message   = message;
		}

		/// <summary>NUnit's <c>Test.FullName</c>, including the argument list.</summary>
		public string  FullName  { get; }
		/// <summary>Provider configuration, or <see langword="null"/> for a test with no data-source parameter.</summary>
		public string? Provider  { get; }
		/// <summary>Whether the case ran over the remote transport.</summary>
		public bool    IsRemote  { get; }
		/// <summary>Whether the test passed once the gate was lifted.</summary>
		public bool    Passed    { get; }
		/// <summary>Full name of the surfaced exception type, or <see langword="null"/>.</summary>
		public string? ErrorType { get; }
		/// <summary>The original result message, unescaped.</summary>
		public string? Message   { get; }
	}
}
