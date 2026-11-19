using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;

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
			bool               poolKeepsSessionState = false,
			bool               crEndsLineComment     = true,
			Func<string?, IReadOnlyList<Encoding>?>? wireEncodings = null)
		{
			/// <summary>
			/// A carriage return that no line feed follows ends a <c>--</c> comment on the server.
			/// </summary>
			public bool CrEndsLineComment { get; } = crEndsLineComment;

			/// <summary>
			/// The encodings the driver sends the command text in, for a connection string: none when it sends the text as
			/// UTF-16, as it stands; <see langword="null"/> when the server picks the character set at login, so only ASCII
			/// is known to arrive unchanged.
			/// </summary>
			public IReadOnlyList<Encoding>? WireEncodings(string? connectionString)
			{
				return wireEncodings == null ? [] : wireEncodings(connectionString);
			}

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
			poolKeepsSessionState: true,
			crEndsLineComment:     false,
			wireEncodings:         AseWireEncodings);

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
		/// Validates read-only SQL as the server receives it over a connection with this connection string.
		/// </summary>
		internal static SqlGuardResult Validate(IDataProvider provider, string sql, string? connectionString)
		{
			return Validate(provider.Name, sql, connectionString);
		}

		/// <summary>
		/// Validates that SQL contains a single statement.
		/// </summary>
		public static SqlGuardResult ValidateSingleStatement(IDataProvider provider, string sql)
		{
			return ValidateSingleStatement(provider.Name, sql);
		}

		/// <summary>
		/// Validates that SQL contains a single statement, as the server receives it over a connection with this
		/// connection string.
		/// </summary>
		internal static SqlGuardResult ValidateSingleStatement(IDataProvider provider, string sql, string? connectionString)
		{
			return ValidateSingleStatement(provider.Name, sql, connectionString);
		}

		// Without a connection string the driver's defaults apply.
		internal static SqlGuardResult Validate(string providerName, string sql, string? connectionString = null)
		{
			var dialect = FindDialect(providerName);

			if (dialect == null)
				return GenericReadOnlySqlGuard.Validate(sql);

			// The read-only check admits a single statement only, so it holds the single-statement check.
			return Validate(dialect, sql, false, connectionString);
		}

		internal static SqlGuardResult ValidateSingleStatement(string providerName, string sql, string? connectionString = null)
		{
			// Single-statement execution is a hard query command contract. Providers with a dialect grammar get it from
			// the grammar; other providers use generic best-effort validation.
			var dialect = FindDialect(providerName);

			return dialect == null
				? GenericReadOnlySqlGuard.ValidateSingleStatement(sql)
				: Validate(dialect, sql, true, connectionString);
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

		internal static SqlGuardResult Validate(SqlDialect dialect, string sql, bool singleStatement, string? connectionString = null)
		{
			if (string.IsNullOrWhiteSpace(sql))
				return SqlGuardResult.Rejected("Query is empty.");

			// The check reads the text the server receives. A driver that encodes it in a narrower character set
			// replaces what that set does not hold, often by a lookalike (U+2019 by ', a fullwidth letter by its ASCII
			// letter), so such a text is refused at the first character that does not survive the trip.
			// Every character set a driver may choose holds ASCII, so an ASCII text is the same on both sides.
			if (!Ascii.IsValid(sql))
			{
				var encodings = dialect.WireEncodings(connectionString);

				if (encodings == null)
				{
					var position = sql.AsSpan().IndexOfAnyExceptInRange((char)0, (char)0x7F);

					return RejectUnknownCharset(sql, position, dialect.CrEndsLineComment);
				}

				foreach (var encoding in encodings)
				{
					var changed = FindTranscoded(sql, encoding, out var received);

					if (changed >= 0)
						return RejectTranscoded(sql, changed, received, encoding, dialect.CrEndsLineComment);
				}
			}

			long refusedAt;

			try
			{
				if (dialect.Check(sql, singleStatement, out var writesTemporaryTables))
				{
					if (writesTemporaryTables)
					{
						var nonAscii = FindNonAsciiCode(sql, dialect.CrEndsLineComment);

						if (nonAscii >= 0)
							return RejectNonAscii(sql, nonAscii, dialect.CrEndsLineComment);
					}

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
					return SqlGuardResult.Rejected($"Only SELECT queries are allowed: unexpected '{quotedText}' at {Location(sql, position - 1, dialect.CrEndsLineComment)}. In a script that writes temporary tables, a quoted name must begin with an ASCII letter, a digit, _ or $.");
			}

			// The describer reads comments the way the servers that nest them do; a class whose servers disagree refuses at
			// the comment itself.
			if (sql.AsSpan(position).StartsWith("/*", StringComparison.Ordinal))
			{
				if (sql.AsSpan(position).StartsWith("/*!", StringComparison.Ordinal) || sql.AsSpan(position).StartsWith("/*M!", StringComparison.OrdinalIgnoreCase))
					return SqlGuardResult.Rejected($"Query is not accepted: the executable comment at {Location(sql, position, dialect.CrEndsLineComment)} runs or not depending on the server version.");

				if (sql.IndexOf("*/", position + 2, StringComparison.Ordinal) >= 0)
					return SqlGuardResult.Rejected($"Query is not accepted: the comment at {Location(sql, position, dialect.CrEndsLineComment)} holds /*; databases that nest comments and databases that end a comment at its first */ read it differently.");
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

			var at = Location(sql, position, dialect.CrEndsLineComment);

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
				_             => SqlGuardResult.Rejected($"Only SELECT queries are allowed: unexpected '{text}' at {at}."),
			};
		}

		// SQL Server matches the target of UPDATE/DELETE #t (or @v) against the aliases, table names and common table
		// expressions of the statement under the database collation. Collations ignore some characters (U+0640, U+200D,
		// U+FEFF, ...) and fold others (U+FF03 equals #), and the server reads characters into a name that the guard
		// reads as symbols (U+3007, U+200E, U+1369, ...): such a name can equal the target, and the statement writes the
		// permanent table it aliases. No list of those characters is complete or stable across server versions, so a
		// script that writes temporary tables is ASCII only outside string literals and comments; quoted names count as
		// code. The text is read the way the server reads T-SQL: '...' with '' inside, -- to the end of the line, nested
		// /* */, [...] with ]] inside and "..." with "" inside (a name, or a string under QUOTED_IDENTIFIER OFF).
		// Returns the position of the first other character, or -1.
		internal static int FindNonAsciiCode(string sql, bool crEndsLineComment)
		{
			var i = 0;

			while (i < sql.Length)
			{
				var c = sql[i];

				if (c == '\'')
				{
					i = SkipQuoted(sql, i, '\'');
				}
				else if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
				{
					// SQL Server ends -- at a carriage return too, ASE at a line feed only.
					while (i < sql.Length && sql[i] != '\n' && !(crEndsLineComment && sql[i] == '\r'))
						i++;
				}
				else if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
				{
					i = SkipBlockComment(sql, i);
				}
				else if (c is '[' or '"')
				{
					var end = SkipQuoted(sql, i, c == '[' ? ']' : '"');

					for (var j = i + 1; j < end; j++)
					{
						if (!IsAsciiCode(sql[j]))
							return j;
					}

					i = end;
				}
				else
				{
					if (!IsAsciiCode(c))
						return i;

					i++;
				}
			}

			return -1;
		}

		// Printable ASCII, and the control characters the grammar reads as space.
		static bool IsAsciiCode(char c)
		{
			return c < 0x7F;
		}

		// The position after the closing character of a quoted span that starts at the position; a doubled closing
		// character stands for itself. An unterminated span runs to the end of the text.
		static int SkipQuoted(string sql, int start, char close)
		{
			var i = start + 1;

			while (i < sql.Length)
			{
				if (sql[i] == close)
				{
					if (i + 1 < sql.Length && sql[i + 1] == close)
					{
						i += 2;
						continue;
					}

					return i + 1;
				}

				i++;
			}

			return i;
		}

		// The position after a block comment that starts at the position; block comments nest, as in the grammar.
		static int SkipBlockComment(string sql, int start)
		{
			var depth = 0;
			var i     = start;

			while (i < sql.Length)
			{
				if (sql[i] == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
				{
					depth++;
					i += 2;
				}
				else if (sql[i] == '*' && i + 1 < sql.Length && sql[i + 1] == '/')
				{
					depth--;
					i += 2;

					if (depth == 0)
						return i;
				}
				else
				{
					i++;
				}
			}

			return i;
		}

		// AdoNetCore.AseClient sends the command text in the character set the server names at login: the one the connection
		// string asks for with Charset=, or the server's own when it asks for none. It encodes with .NET's encoder for that
		// set, which replaces what the set does not hold, often by a lookalike. Which set the server names is known only
		// after login, so only ASCII, which every set holds, is known to arrive unchanged. With Charset=utf8 the driver
		// sends UTF-8, which carries every character but an unpaired surrogate, and the server converts it or reports an
		// error; nothing is replaced on the way.
		static IReadOnlyList<Encoding>? AseWireEncodings(string? connectionString)
		{
			return RequestedCharset(connectionString)?.Trim().ToLowerInvariant() is "utf8" or "utf-8" ? [Encoding.UTF8] : null;
		}

		static string? RequestedCharset(string? connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
				return null;

			try
			{
				var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

				return builder.TryGetValue("Charset", out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		// The position of the first character (or surrogate pair) the encoding does not carry unchanged, with what arrives
		// in its place; -1 when the whole text survives.
		static int FindTranscoded(string sql, Encoding encoding, out string received)
		{
			received = string.Empty;

			if (string.Equals(encoding.GetString(encoding.GetBytes(sql)), sql, StringComparison.Ordinal))
				return -1;

			for (var i = 0; i < sql.Length; i++)
			{
				var length = char.IsHighSurrogate(sql[i]) && i + 1 < sql.Length && char.IsLowSurrogate(sql[i + 1]) ? 2 : 1;
				var text   = sql.Substring(i, length);
				var back   = encoding.GetString(encoding.GetBytes(text));

				if (!string.Equals(back, text, StringComparison.Ordinal))
				{
					received = back;
					return i;
				}

				i += length - 1;
			}

			return -1;
		}

		static SqlGuardResult RejectUnknownCharset(string sql, int position, bool crIsLineBreak)
		{
			return SqlGuardResult.Rejected(string.Create(
				CultureInfo.InvariantCulture,
				$"Query is not accepted: character U+{CodePointAt(sql, position):X4} at {Location(sql, position, crIsLineBreak)} is outside ASCII. The Sybase ASE driver sends the text in the character set the server names at login, which may replace such a character by another; to send characters outside ASCII, set Charset=utf8 in the connection string."));
		}

		static int CodePointAt(string sql, int position)
		{
			return char.IsHighSurrogate(sql[position]) && position + 1 < sql.Length && char.IsLowSurrogate(sql[position + 1])
				? char.ConvertToUtf32(sql[position], sql[position + 1])
				: sql[position];
		}

		static SqlGuardResult RejectTranscoded(string sql, int position, string received, Encoding encoding, bool crIsLineBreak)
		{
			var code = CodePointAt(sql, position);

			return SqlGuardResult.Rejected(string.Create(
				CultureInfo.InvariantCulture,
				$"Query is not accepted: character U+{code:X4} at {Location(sql, position, crIsLineBreak)} reaches the server as '{received}'. The driver sends the text in {encoding.WebName}, which does not hold it; write the text in that character set."));
		}

		static SqlGuardResult RejectNonAscii(string sql, int position, bool crIsLineBreak)
		{
			var code = CodePointAt(sql, position);

			return SqlGuardResult.Rejected(string.Create(
				CultureInfo.InvariantCulture,
				$"Only SELECT queries are allowed: character U+{code:X4} at {Location(sql, position, crIsLineBreak)}. A script that writes temporary tables accepts non-ASCII characters only inside string literals ('...', N'...') and comments; names, quoted names included, must be ASCII."));
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

		// A carriage return alone breaks a line only where the server reads it so (it ends a -- comment there too).
		static string Location(string sql, int position, bool crIsLineBreak)
		{
			var line      = 1;
			var lineStart = 0;

			for (var i = 0; i < position; i++)
			{
				if (sql[i] == '\r' && crIsLineBreak)
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
