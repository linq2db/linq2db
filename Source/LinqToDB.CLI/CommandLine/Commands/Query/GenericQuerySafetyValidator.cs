using System;
using System.Collections.Generic;

namespace LinqToDB.CommandLine
{
	internal static class GenericQuerySafetyValidator
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

		public static QuerySafetyResult Validate(string sql)
		{
			var tokens = Tokenize(sql);

			if (tokens.Count == 0)
				return QuerySafetyResult.Unsafe("Query is empty.");

			var semicolonIndex = tokens.IndexOf(";");
			if (semicolonIndex >= 0 && semicolonIndex != tokens.Count - 1)
				return QuerySafetyResult.Unsafe("Only single SELECT statement is allowed.");

			for (var i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];
				if (_forbiddenTokens.Contains(token))
					return QuerySafetyResult.Unsafe($"Query is not read-only: token '{token}' is not allowed.");
			}

			var firstToken = tokens[0];
			if (!string.Equals(firstToken, "SELECT", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(firstToken, "WITH", StringComparison.OrdinalIgnoreCase))
			{
				return QuerySafetyResult.Unsafe("Only SELECT queries are allowed.");
			}

			return QuerySafetyResult.Safe;
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
				else if (current == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
				{
					i += 2;
					while (i < sql.Length && sql[i] != '\r' && sql[i] != '\n')
						i++;
				}
				else if (current == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
				{
					i += 2;
					while (i + 1 < sql.Length && (sql[i] != '*' || sql[i + 1] != '/'))
						i++;

					i = Math.Min(i + 2, sql.Length);
				}
				else if (current == '\'')
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
				}
				else if (current == '"' || current == '`' || current == '[')
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
				}
				else if (current == ';')
				{
					tokens.Add(";");
					i++;
				}
				else if (char.IsLetter(current) || current == '_')
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
			}

			return tokens;
		}
	}
}
