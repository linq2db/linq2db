using System;
using System.Collections.Generic;

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
			"USE",
			"VACUUM",
		};

		public static SqlGuardResult Validate(string sql)
		{
			if (sql.Contains("/*!", StringComparison.Ordinal))
				return SqlGuardResult.Rejected("MySQL executable comments are not allowed in read-only queries.");

			var tokens                = Tokenize(sql);
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

			if (!string.Equals(firstToken, "SELECT", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(firstToken, "WITH", StringComparison.OrdinalIgnoreCase))
			{
				return SqlGuardResult.Rejected("Only SELECT queries are allowed.");
			}

			return SqlGuardResult.Allowed;
		}

		public static SqlGuardResult ValidateSingleStatement(string sql)
		{
			return ValidateSingleStatement(Tokenize(sql));
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

		private static List<string> Tokenize(string sql)
		{
			var tokens = new List<string>();

			for (var i = 0; i < sql.Length;)
			{
				var current = sql[i];

				if (char.IsWhiteSpace(current))
				{
					i++;
				}
				else switch (current)
				{
					case '-' when i + 1 < sql.Length && sql[i + 1] == '-':
					{
						i += 2;
						while (i < sql.Length && sql[i] != '\r' && sql[i] != '\n')
							i++;
						break;
					}
					case '/' when i + 1 < sql.Length && sql[i + 1] == '*':
					{
						i += 2;
						while (i + 1 < sql.Length && (sql[i] != '*' || sql[i + 1] != '/'))
							i++;

						i = Math.Min(i + 2, sql.Length);
						break;
					}
					case '\'':
					{
						i++;
						while (i < sql.Length)
						{
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
					case '$':
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
						var close     = sql.IndexOf(delimiter, delimiterEnd + 1, StringComparison.Ordinal);

						i = close < 0 ? sql.Length : close + delimiter.Length;
						break;
					}
					case '"' or '`' or '[':
					{
						var close = current == '[' ? ']' : current;

						i++;
						while (i < sql.Length)
						{
							if (sql[i] == close)
							{
								i++;
								if (i < sql.Length && sql[i] == close)
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
					case ';':
						tokens.Add(";");
						i++;
						break;
					default:
					{
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

			return tokens;
		}
	}
}
