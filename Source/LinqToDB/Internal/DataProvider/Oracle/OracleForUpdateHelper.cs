using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using LinqToDB.DataProvider.Oracle;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	static partial class OracleForUpdateHelper
	{
		internal const string NestedQueryError = "Oracle FOR UPDATE is only supported on the outermost SELECT, not in nested queries, CTE bodies or set operations.";

		static bool IsForUpdate(SqlQueryExtension extension)
		{
			if (extension.BuilderType == typeof(OracleHints.ForUpdateHintExtensionBuilder))
				return true;
			if (!extension.Arguments.TryGetValue("hint", out var hint) || hint is not SqlValue { Value: string text })
				return false;

			var offset = 0;
			return ConsumeKeyword(text, "FOR", ref offset) && ConsumeKeyword(text, "UPDATE", ref offset);
		}

		static bool ConsumeKeyword(string text, string keyword, ref int offset)
		{
			while (offset < text.Length && char.IsWhiteSpace(text[offset]))
				offset++;

			if (text.Length - offset < keyword.Length)
				return false;

			var end = offset + keyword.Length;
			if ((end < text.Length && !char.IsWhiteSpace(text[end]))
				|| string.Compare(text, offset, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) != 0)
				return false;

			offset = end;
			return true;
		}

		static SqlQueryExtension? FindLockHint(List<SqlQueryExtension> extensions, out int count)
		{
			SqlQueryExtension? first = null;
			count = 0;
			foreach (var extension in extensions)
			{
				if (extension.Scope != Sql.QueryExtensionScope.SubQueryHint)
					continue;
				if (!IsForUpdate(extension))
					throw new LinqToDBException("Oracle only supports FOR UPDATE subquery hints.");

				first ??= extension;
				count++;
			}

			return first;
		}

		internal static void ValidateNestedQuery(List<SqlQueryExtension> extensions)
		{
			if (FindLockHint(extensions, out _) != null)
				throw new LinqToDBException(NestedQueryError);
		}

		internal static void PrepareRootQuery(SqlStatement statement, MappingSchema mappingSchema)
		{
			if (statement.SelectQuery is not { SqlQueryExtensions: { Count: > 0 } extensions } query
				|| FindLockHint(extensions, out var count) is not { } hint)
				return;

			if (statement is not SqlSelectStatement)
			{
				// DML already locks its target rows; a SELECT locking suffix is invalid here.
				extensions.RemoveAll(static extension => extension.Scope == Sql.QueryExtensionScope.SubQueryHint);
				return;
			}

			if (count != 1)
				throw new LinqToDBException("Oracle SELECT supports only one FOR UPDATE clause.");
			if (query.HasSetOperators)
				throw new LinqToDBException(NestedQueryError);
			if (query.Select.IsDistinct || !query.GroupBy.IsEmpty || QueryHelper.IsAggregationQuery(query))
				throw new LinqToDBException("Oracle FOR UPDATE cannot be used with DISTINCT, grouping or aggregate queries.");

			var take = query.Select.TakeValue;
			if (take != null && IsSkipLocked(hint))
				throw new LinqToDBException("Oracle FOR UPDATE SKIP LOCKED cannot be combined with row limiting (Take, First or Single): Oracle limits candidates before skipping locked rows.");
			if (query.Select.SkipValue != null || (take != null && !query.OrderBy.IsEmpty))
				throw new LinqToDBException("Oracle FOR UPDATE cannot be combined with Skip or ordered row limiting.");
			if (take == null)
				return;

			// FETCH NEXT is incompatible with FOR UPDATE, including First/Single's implicit Take.
			// Match Oracle 11's simple-limit lowering on every Oracle dialect.
			var rownum = new SqlExpression(mappingSchema.GetDbDataType(typeof(long)), "ROWNUM", Precedence.Primary,
				SqlFlags.IsAggregate | SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable);
			query.Where.EnsureConjunction().AddLessOrEqual(rownum, take, CompareNulls.LikeSql);
			query.Select.Take(null, null);
		}

		const string HintTokenPattern = "--[^\\r\\n]*|/\\*[\\s\\S]*?\\*/|\"(?:\"\"|[^\"])*\"|[A-Za-z_][A-Za-z_0-9$#]*|\\S";

#if SUPPORTS_REGEX_GENERATORS
		[GeneratedRegex(HintTokenPattern, RegexOptions.ExplicitCapture)]
		private static partial Regex HintTokenRegex();
#else
		static Regex? _hintTokenRegex;
		static Regex HintTokenRegex() => _hintTokenRegex ??= new(HintTokenPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled);
#endif

		static bool IsSkipLocked(SqlQueryExtension extension)
		{
			if (!extension.Arguments.TryGetValue("hint", out var hint) || hint is not SqlValue { Value: string text })
				return false;

			if (extension.BuilderType == typeof(OracleHints.ForUpdateHintExtensionBuilder))
				return string.Equals(text, OracleHints.SkipLocked, StringComparison.Ordinal);

			// Keep quoted identifiers and punctuation intact, but ignore SQL comments.
			string? previous = null;
			string? last = null;
			foreach (Match token in HintTokenRegex().Matches(text))
			{
				var value = token.Value;
				if (value.StartsWith("--", StringComparison.Ordinal) || value.StartsWith("/*", StringComparison.Ordinal))
					continue;
				previous = last;
				last = value;
			}

			return string.Equals(previous, "SKIP", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(last, "LOCKED", StringComparison.OrdinalIgnoreCase);
		}
	}
}
