using System;
using System.Collections.Generic;
using System.Globalization;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	internal static class GenericReadOnlySqlGuard
	{
		private static readonly HashSet<string> _forbiddenTokens = new(StringComparer.OrdinalIgnoreCase)
		{
			"ALTER",
			"BACKUP",
			"BEGIN",
			"CALL",
			"CREATE",
			"DBCC",
			"DECLARE",
			"DELETE",
			"DENY",
			"DO",
			"DROP",
			"EXEC",
			"EXECUTE",
			"GRANT",
			"INSERT",
			"MERGE",
			"REINDEX",
			"RESTORE",
			"REVOKE",
			"SET",
			"TRUNCATE",
			"UPDATE",
			"UPSERT",
			"USE",
			"VACUUM",
		};

		public static SqlGuardResult Validate(string sql)
		{
			if (!TryTokenize(sql, out var tokens, out var tokenizationError))
				return tokenizationError!;

			var singleStatementResult = ValidateSingleStatement(tokens);

			if (!singleStatementResult.IsAllowed)
				return singleStatementResult;

			for (var index = 0; index < tokens.Count; index++)
			{
				var token = tokens[index];

				if (string.Equals(token, "INTO", StringComparison.Ordinal))
				{
					// A bare "into" table reference right after FROM/JOIN is a read-only identifier
					// (e.g. "FROM into"); everywhere else INTO marks a write (SELECT ... INTO table,
					// MySQL's SELECT ... INTO OUTFILE, INSERT INTO which is already forbidden above).
					var precedingToken = index > 0 ? tokens[index - 1] : null;

					if (precedingToken is "FROM" or "JOIN")
						continue;

					return SqlGuardResult.Rejected("Query is not read-only: token 'INTO' is not allowed.");
				}

				if (_forbiddenTokens.Contains(token))
					return SqlGuardResult.Rejected($"Query is not read-only: token '{token}' is not allowed.");
			}

			var firstToken = tokens[0];

			if (!string.Equals(firstToken, "SELECT", StringComparison.OrdinalIgnoreCase) &&
			    !string.Equals(firstToken, "WITH",   StringComparison.OrdinalIgnoreCase))
			{
				return SqlGuardResult.Rejected("Only SELECT queries are allowed.");
			}

			return SqlGuardResult.Allowed;
		}

		public static SqlGuardResult ValidateSingleStatement(string sql)
		{
			return TryTokenize(sql, out var tokens, out var tokenizationError)
				? ValidateSingleStatement(tokens)
				: tokenizationError!;
		}

		private static SqlGuardResult ValidateSingleStatement(List<string> tokens)
		{
			if (tokens.Count == 0)
				return SqlGuardResult.Rejected("Query is empty.");

			var semicolonIndex = tokens.IndexOf(";");
			if (semicolonIndex >= 0 && semicolonIndex != tokens.Count - 1)
				return SqlGuardResult.Rejected("Only single SQL statement is allowed.");

			return SqlGuardResult.Allowed;
		}

		private static bool TryTokenize(string sql, out List<string> tokens, out SqlGuardResult? error)
		{
			tokens = [];
			error  = null;

			for (var i = 0; i < sql.Length;)
			{
				var remaining = sql.AsSpan(i);

				switch (remaining)
				{
					case [var current, ..] when char.IsWhiteSpace(current):
					{
						i++;
						break;
					}
					case ['-', '-']:
					case ['-', '-', var next, ..] when char.IsWhiteSpace(next) || char.IsControl(next):
					{
						i += 2;
						while (i < sql.Length && sql[i] != '\r' && sql[i] != '\n')
							i++;

						break;
					}
					case ['-', '-', var next, ..]:
					{
						error = CreateAmbiguousSyntaxError(
							sql,
							i,
							$"--{next}",
							"'--' without following whitespace has provider-dependent meaning.",
							"Add whitespace after a comment marker or use explicit spaces around arithmetic operators, for example '1 - -1'.");

						return false;
					}
					case ['/', '*', '!', ..]:
					case ['/', '*', 'M' or 'm', '!', ..]:
					{
						var syntax = remaining[2] == '!' ? "/*!" : sql.Substring(i, 4);

						error = CreateAmbiguousSyntaxError(
							sql,
							i,
							syntax,
							"Executable comments are interpreted as SQL by MySQL or MariaDB.",
							"Remove the executable comment and express the read-only operation as regular SQL.");

						return false;
					}
					case ['/', '*', ..]:
					{
						i += 2;
						while (i + 1 < sql.Length && (sql[i] != '*' || sql[i + 1] != '/'))
							i++;

						i = Math.Min(i + 2, sql.Length);

						break;
					}
					case ['\'', ..]:
					{
						i++;

						while (i < sql.Length)
						{
							if (sql[i] == '\\')
							{
								error = CreateAmbiguousSyntaxError(
									sql,
									i,
									"\\",
									"Backslash escaping inside single-quoted strings is provider-dependent.",
									"Use doubled quote escaping or rewrite the string expression without backslash escapes.");

								return false;
							}

							if (sql[i] == '\'')
							{
								i++;

								if (i < sql.Length && sql[i] == '\'')
								{
									i++;
									continue;
								}

								break;
							}

							i++;
						}

						break;
					}
					case ['$', ..]:
					{
						var delimiterEnd = i + 1;

						if (delimiterEnd < sql.Length && sql[delimiterEnd] != '$')
						{
							if (!char.IsLetter(sql[delimiterEnd]) && sql[delimiterEnd] != '_')
							{
								i++;
								break;
							}

							delimiterEnd++;

							while (delimiterEnd < sql.Length && (char.IsLetterOrDigit(sql[delimiterEnd]) || sql[delimiterEnd] == '_'))
								delimiterEnd++;
						}

						if (delimiterEnd >= sql.Length || sql[delimiterEnd] != '$')
						{
							i++;
							break;
						}

						var delimiter = sql.Substring(i, delimiterEnd - i + 1);

						error = CreateAmbiguousSyntaxError(
							sql,
							i,
							delimiter,
							"Dollar-quoted strings conflict with dollar signs allowed in identifiers by other providers.",
							"Rewrite the string using standard single quotes with doubled quote escaping.");

						return false;
					}
					case ['"', ..] or ['`', ..] or ['[', ..]:
					{
						var current = remaining[0];
						var close   = current == '[' ? ']' : current;

						i++;

						while (i < sql.Length)
						{
							if (current == '"' && sql[i] == '\\')
							{
								error = CreateAmbiguousSyntaxError(
									sql,
									i,
									"\\",
									"Backslash escaping inside double-quoted strings is provider-dependent.",
									"Use doubled quote escaping or rewrite the string expression without backslash escapes.");

								return false;
							}

							if (sql[i] == close)
							{
								i++;

								// Doubling escapes a quoted identifier only for "" and ``. Providers routed to this guard
								// use [ ] for array/list subscripts, so reading "]]" as an escaped ] would consume the rest
								// of the statement - including the ';' and any write tokens after it.
								if (current != '[' && i < sql.Length && sql[i] == close)
								{
									i++;
									continue;
								}

								break;
							}

							i++;
						}

						break;
					}
					case [';', ..]:
						tokens.Add(";");
						i++;
						break;
					default:
					{
						var current = remaining[0];

						if (char.IsLetter(current) || current == '_')
						{
							var start = i++;
							while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_' || sql[i] == '$'))
								i++;

							tokens.Add(sql[start..i].ToUpperInvariant());
						}
						else
						{
							i++;
						}

						break;
					}
				}
			}

			return true;
		}

		private static SqlGuardResult CreateAmbiguousSyntaxError(string sql, int position, string syntax, string reason, string rewrite)
		{
			var line      = 1;
			var lineStart = 0;

			for (var i = 0; i < position; i++)
			{
				if (sql[i] == '\r')
				{
					line++;
					if (i + 1 < position && sql[i + 1] == '\n')
						i++;

					lineStart = i + 1;
				}
				else if (sql[i] == '\n')
				{
					line++;
					lineStart = i + 1;
				}
			}

			var lineEnd = position;
			while (lineEnd < sql.Length && sql[lineEnd] != '\r' && sql[lineEnd] != '\n')
				lineEnd++;

			var fragment = sql[lineStart..lineEnd];
			const int maxFragmentLength = 120;

			if (fragment.Length > maxFragmentLength)
			{
				var positionInLine = position - lineStart;
				var fragmentStart  = Math.Max(0, positionInLine - maxFragmentLength / 2);

				if (fragmentStart + maxFragmentLength > fragment.Length)
					fragmentStart = fragment.Length - maxFragmentLength;

				fragment = string.Concat(
					(fragmentStart > 0 ? "..." : string.Empty).AsSpan(),
					fragment.AsSpan(fragmentStart, maxFragmentLength),
					(fragmentStart + maxFragmentLength < lineEnd - lineStart ? "..." : string.Empty).AsSpan());
			}

			return SqlGuardResult.Rejected(string.Create(
				CultureInfo.InvariantCulture,
				$"Query was rejected because ambiguous SQL syntax cannot be validated safely. Line {line}, column {position - lineStart + 1}: '{syntax}'. Reason: {reason} Rewrite: {rewrite} SQL fragment: {fragment}"));
		}
	}
}
