using System;
using System.Collections.Generic;

namespace LinqToDB.CommandLine
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
			"INTO",
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
			var tokens                = Tokenize(sql);
			var singleStatementResult = ValidateSingleStatement(tokens);

			if (!singleStatementResult.IsAllowed)
				return singleStatementResult;

			foreach (var token in tokens)
			{
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
