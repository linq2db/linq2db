using System;
using System.Globalization;

using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Validates SQL against the read-only query contract.
	/// </summary>
	public static class ReadOnlySqlGuard
	{
		/// <summary>
		/// The read-only check of one behaviour class: whether the text passes, and whether it writes temporary tables.
		/// </summary>
		internal delegate bool ScriptCheck(string sql, out bool writesTemporaryTables);

		/// <summary>
		/// How a provider's SQL text is read: the two checks generated for the behaviour class of its dialect. Each
		/// answers whether the text passes, and where it was refused when it does not.
		/// </summary>
		internal sealed class SqlDialect(
			ScriptCheck        check,
			Func<string, long> checkRefusedAt,
			Func<string, bool> single,
			Func<string, long> singleRefusedAt,
			bool               poolKeepsSessionState = false)
		{
			/// <summary>
			/// The provider's driver hands a pooled connection out again with the session state the previous call left
			/// (temporary tables, session options), so a call that may leave some must not return its connection to the
			/// pool.
			/// </summary>
			public bool PoolKeepsSessionState { get; } = poolKeepsSessionState;

			public bool Check(string sql, bool singleStatement, out bool writesTemporaryTables)
			{
				if (singleStatement)
				{
					writesTemporaryTables = false;
					return single(sql);
				}

				return check(sql, out writesTemporaryTables);
			}

			public bool Check(string sql, bool singleStatement)
			{
				return Check(sql, singleStatement, out _);
			}

			public long RefusedAt(string sql, bool singleStatement)
			{
				return singleStatement ? singleRefusedAt(sql) : checkRefusedAt(sql);
			}
		}

		// SQL Server and Sybase ASE run statements that follow each other without a separator. ASE is a class of its own
		// because its driver keeps session state on a pooled connection: AseClient resets nothing when a connection
		// returns to the pool.
		static readonly SqlDialect _tsql = new(
			ReadOnlySqlGrammar.TryCheckTSql,                     sql => ReadOnlySqlGrammar.TryCheckTSql (sql).Position,
			sql => ReadOnlySqlGrammar.TrySingleTSql(sql, out _), sql => ReadOnlySqlGrammar.TrySingleTSql(sql).Position);

		static readonly SqlDialect _ase = new(
			ReadOnlySqlGrammar.TryCheckAse,                      sql => ReadOnlySqlGrammar.TryCheckAse (sql).Position,
			sql => ReadOnlySqlGrammar.TrySingleAse(sql, out _),  sql => ReadOnlySqlGrammar.TrySingleAse(sql).Position,
			poolKeepsSessionState: true);

		// [x] is a quoted identifier.
		static readonly SqlDialect _bracket = new(
			ReadOnlySqlGrammar.TryCheckBracket,                     sql => ReadOnlySqlGrammar.TryCheckBracket (sql).Position,
			sql => ReadOnlySqlGrammar.TrySingleBracket(sql, out _), sql => ReadOnlySqlGrammar.TrySingleBracket(sql).Position);

		// The only place that knows providers: a provider name maps, by its longest matching prefix, to the way its SQL
		// is read. Providers not listed here use the generic best-effort validator.
		static readonly (string Prefix, SqlDialect Dialect)[] _dialects =
		[
			(ProviderName.SqlServer, _tsql   ),
			(ProviderName.Sybase,    _ase    ),
			(ProviderName.SqlCe,     _bracket),
		];

		/// <summary>
		/// Validates read-only SQL using provider-specific rules where available.
		/// </summary>
		public static SqlGuardResult Validate(IDataProvider provider, string sql)
		{
			return Validate(provider.Name, sql);
		}

		/// <summary>
		/// Validates that SQL contains a single statement.
		/// </summary>
		public static SqlGuardResult ValidateSingleStatement(IDataProvider provider, string sql)
		{
			return ValidateSingleStatement(provider.Name, sql);
		}

		internal static SqlGuardResult Validate(string providerName, string sql)
		{
			var dialect = FindDialect(providerName);

			if (dialect == null)
				return GenericReadOnlySqlGuard.Validate(sql);

			// The read-only check admits a single statement only, so it holds the single-statement check.
			return Validate(dialect, sql, false);
		}

		internal static SqlGuardResult ValidateSingleStatement(string providerName, string sql)
		{
			// Single-statement execution is a hard query command contract. Providers with a dialect grammar get it from
			// the grammar; other providers use generic best-effort validation.
			var dialect = FindDialect(providerName);

			return dialect == null
				? GenericReadOnlySqlGuard.ValidateSingleStatement(sql)
				: Validate(dialect, sql, true);
		}

		static SqlDialect? FindDialect(string providerName)
		{
			SqlDialect? found  = null;
			var         length = -1;

			foreach (var (prefix, dialect) in _dialects)
			{
				if (prefix.Length > length && providerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					found  = dialect;
					length = prefix.Length;
				}
			}

			return found;
		}

		internal static SqlGuardResult Validate(SqlDialect dialect, string sql, bool singleStatement)
		{
			if (string.IsNullOrWhiteSpace(sql))
				return SqlGuardResult.Rejected("Query is empty.");

			long refusedAt;

			try
			{
				if (dialect.Check(sql, singleStatement, out var writesTemporaryTables))
				{
					// Execute mode admits any statement, and so any session state.
					return SqlGuardResult.Accepted(
						writesTemporaryTables,
						dialect.PoolKeepsSessionState && (singleStatement || writesTemporaryTables));
				}

				refusedAt = dialect.RefusedAt(sql, singleStatement);
			}
			catch (InsufficientExecutionStackException)
			{
				return SqlGuardResult.Rejected("Query is nested too deeply to be checked.");
			}

			return Explain(dialect, sql, (int)Math.Min(refusedAt, sql.Length), singleStatement);
		}

		static SqlGuardResult Explain(SqlDialect dialect, string sql, int position, bool singleStatement)
		{
			// The parsers report the furthest position they could follow the text to. That is where the refused construct
			// begins, inside it (INTERSECT reads the INT of INTO), a few tokens past it (a word is read whole before the
			// check after it fails), or the end of the text. So the construct is looked for at that position, then at the
			// starts of the few tokens before it.
			position = SkipSeparators(sql, AlignToWordStart(sql, position));

			// In a temporary-table script a quoted name must begin with an ASCII letter, a digit, _ or $ (the grammar's
			// UnsafeQuoted), and the parsers stop at its first character. The whole name is named.
			if (position > 0 && position < sql.Length && sql[position - 1] is '[' or '"' && !IsSafeQuotedStart(sql[position]))
			{
				var (quotedKind, quotedText) = Describe(sql, position - 1);

				if (quotedKind is "other" && quotedText.Length > 2 && quotedText[0] == sql[position - 1])
					return SqlGuardResult.Rejected($"Only SELECT queries are allowed: unexpected '{quotedText}' at {Location(sql, position - 1)}. In a script that writes temporary tables, a quoted name must begin with an ASCII letter, a digit, _ or $.");
			}

			var (kind, text) = Describe(sql, position);

			if (kind is "other")
			{
				var candidate = position;

				for (var step = 0; step < MaxStepsBack; step++)
				{
					candidate = PreviousTokenStart(sql, candidate);

					if (candidate < 0)
						break;

					var (candidateKind, candidateText) = Describe(sql, candidate);

					// The SELECT or WITH that opens the query is no culprit.
					var opensQuery = string.Equals(candidateKind, "statement", StringComparison.Ordinal)
						&& (string.Equals(candidateText, "select", StringComparison.OrdinalIgnoreCase) || string.Equals(candidateText, "with", StringComparison.OrdinalIgnoreCase))
						&& sql.AsSpan(0, candidate).Trim().TrimStart(';').IsEmpty;

					if (candidateKind is not ("other" or "end") && !opensQuery)
					{
						(position, kind, text) = (candidate, candidateKind, candidateText);
						break;
					}
				}
			}
			else if (kind is "end")
			{
				// At the end of the text a word just before it may sit in an open string or comment, so it is not named
				// on its own. The refused construct begins where the longest prefix the check accepts ends.
				var candidate = position;

				for (var step = 0; step < MaxPrefixesTried; step++)
				{
					candidate = PreviousTokenStart(sql, candidate);

					if (candidate <= 0)
						break;

					if (dialect.Check(sql.Substring(0, candidate), singleStatement))
					{
						(kind, text) = Describe(sql, candidate);
						position     = candidate;
						break;
					}
				}
			}

			var at = Location(sql, position);

			switch (kind)
			{
				case "separator":
					return SqlGuardResult.Rejected($"Only single SQL statement is allowed: more than one statement at {at}.");
				case "unterminated":
					return SqlGuardResult.Rejected($"Query is not valid SQL: unterminated string, quoted identifier, comment or bracket at {at}.");
				case "end":
					return singleStatement
						? SqlGuardResult.Rejected("Only single SQL statement is allowed.")
						: SqlGuardResult.Rejected($"Query is not valid SQL: it ends at {at} inside a string, quoted identifier, comment or bracket, or in the middle of a clause.");
			}

			if (singleStatement)
			{
				return kind is "statement" or "into" or "function" or "package" or "sequence" or "routine"
					? SqlGuardResult.Rejected($"Only single SQL statement is allowed: more than one statement at {at}.")
					: SqlGuardResult.Rejected($"Only single SQL statement is allowed: unexpected '{text}' at {at}.");
			}

			var upper = text.ToUpperInvariant();

			return kind switch
			{
				"statement" when upper is "SELECT" or "WITH"
					=> SqlGuardResult.Rejected($"Only single SQL statement is allowed: more than one statement at {at}."),
				"statement"   => SqlGuardResult.Rejected($"Query is not read-only: statement word {upper} at {at}."),
				"into"        => SqlGuardResult.Rejected($"Query is not read-only: INTO at {at}."),
				"function"    => SqlGuardResult.Rejected($"Query is not read-only: function {upper} at {at} is on the write-route list."),
				"package"     => SqlGuardResult.Rejected($"Query is not read-only: {upper} at {at} is on the write-route list."),
				"sequence"    => SqlGuardResult.Rejected($"Query is not read-only: sequence call {upper} at {at}."),
				"routine"     => SqlGuardResult.Rejected($"Query is not read-only: inline routine {upper} at {at}."),
				"scope"       => SqlGuardResult.Rejected($"Query is not read-only: session option scope {upper} at {at} is not allowed."),
				"option"      => SqlGuardResult.Rejected($"Query is not read-only: session option {upper} at {at} is not allowed. Only listed session options with one plain value may precede the query."),
				"assignment"  => SqlGuardResult.Rejected($"Parameter assignment SET {text} at {at} is not a plain value. A parameter takes one plain value: no subquery, no comma outside brackets."),
				"declaration" => SqlGuardResult.Rejected($"Query is not read-only: parameter declaration DECLARE {text} at {at} is not allowed. A parameter type may not be a table, a cursor or a statement."),
				// What stops at a #name is a temporary table where an alias stands (UPDATE #t … FROM t #t).
				_ when text.Length > 1 && text[0] == '#'
				              => SqlGuardResult.Rejected($"Only SELECT queries are allowed: unexpected '{text}' at {at}. In a script that writes temporary tables, a name that starts with # may not stand where an alias does."),
				// The server reads a name and an @ right after it as one name, which may equal the target (U+0640 is ignored, so U+0640@v equals @v).
				_ when text.StartsWith('@') && position > 0 && IsWordChar(sql[position - 1])
				              => SqlGuardResult.Rejected($"Only SELECT queries are allowed: unexpected '{text}' at {at}. In a script that writes temporary tables, a name may not run into a following @."),
				_             => SqlGuardResult.Rejected($"Only SELECT queries are allowed: unexpected '{text}' at {at}."),
			};
		}

		const int MaxStepsBack     = 3;
		const int MaxPrefixesTried = 64;

		static (string Kind, string Text) Describe(string sql, int position)
		{
			if (position >= sql.Length)
				return ("end", string.Empty);

			// The description reads every class's lexing at once, so it would take # for the start of a MySQL comment.
			if (sql[position] == '#')
			{
				var end = position + 1;

				while (end < sql.Length && (sql[end] == '#' || IsWordChar(sql[end])))
					end++;

				return ("other", sql.Substring(position, end - position));
			}

			var description = ReadOnlySqlGrammar.TryDescribeRefusal(sql.Substring(position));

			if (!description.IsSuccess)
				return ("other", sql.Substring(position, 1));

			var separator = description.Value.IndexOf('|', StringComparison.Ordinal);
			var text      = NormalizeSpaces(description.Value.Substring(separator + 1)).TrimEnd('(', ' ');

			return (description.Value.Substring(0, separator), text);
		}

		// The start of the token before the one at the position: a word (with its @, @@, $ or # sigil), a quoted span or
		// one character. Strings and comments are not told apart from code here; this only picks candidates to describe.
		static int PreviousTokenStart(string sql, int position)
		{
			var i = position;

			while (i > 0 && IsSeparator(sql[i - 1]))
				i--;

			if (i == 0)
				return -1;

			var last = sql[i - 1];

			if (IsWordChar(last))
			{
				while (i > 0 && IsWordChar(sql[i - 1]))
					i--;

				while (i > 0 && sql[i - 1] is '@' or '#')
					i--;

				return i;
			}

			var opener = last switch
			{
				']'  => '[',
				'"'  => '"',
				'`'  => '`',
				'\'' => '\'',
				_    => '\0',
			};

			if (opener != '\0')
			{
				var open = sql.LastIndexOf(opener, i - 2 < 0 ? 0 : i - 2);

				if (open >= 0)
					return open;
			}

			// The opener of a block comment is one token.
			if (last == '*' && i >= 2 && sql[i - 2] == '/')
				return i - 2;

			return i - 1;
		}

		// The parsers report the furthest position they read to, which can lie inside the word that was refused when an
		// alternative read part of it. Messages name the whole word.
		static int AlignToWordStart(string sql, int position)
		{
			var start = position;

			while (start > 0 && start < sql.Length && IsWordChar(sql[start - 1]) && IsWordChar(sql[start]))
				start--;

			// A number glued to a word ("1delete") is a lexeme of its own: the word begins after it.
			while (start < position && char.IsAsciiDigit(sql[start]))
				start++;

			return start;
		}

		// A refusal that stops at a separator names what follows it.
		static int SkipSeparators(string sql, int position)
		{
			while (position < sql.Length && IsSeparator(sql[position]))
				position++;

			return position;
		}

		// The characters the grammar reads as space (Lex.Space).
		static bool IsSeparator(char c)
		{
			return c <= ' ' || c == (char)0x200B || char.IsWhiteSpace(c);
		}

		static bool IsSafeQuotedStart(char c)
		{
			return char.IsAsciiLetterOrDigit(c) || c is '_' or '$';
		}

		static bool IsWordChar(char c)
		{
			return char.IsLetterOrDigit(c) || c is '_' or '$' || CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;
		}

		static string NormalizeSpaces(string text)
		{
			return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		}

		static string Location(string sql, int position)
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

			return string.Create(CultureInfo.InvariantCulture, $"{line}:{position - lineStart + 1}");
		}
	}
}
