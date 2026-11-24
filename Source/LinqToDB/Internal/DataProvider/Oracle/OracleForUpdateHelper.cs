using System;
using System.Linq;
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

		internal static bool IsForUpdate(SqlQueryExtension extension)
		{
			if (extension.Scope != Sql.QueryExtensionScope.SubQueryHint)
				return false;
			if (extension.BuilderType == typeof(OracleHints.ForUpdateHintExtensionBuilder))
				return true;
			if (!extension.Arguments.TryGetValue("hint", out var hint) || hint is not SqlValue { Value: string text })
				return false;
			var tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			return tokens.Length >= 2
				&& string.Equals(tokens[0], "FOR", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(tokens[1], "UPDATE", StringComparison.OrdinalIgnoreCase);
		}

		internal static void PrepareStatement(SqlStatement statement, MappingSchema mappingSchema)
		{
			statement.VisitAll(element =>
			{
				if (element is not SelectQuery query || query.SqlQueryExtensions == null)
					return;
				var hints = query.SqlQueryExtensions.Where(e => e.Scope == Sql.QueryExtensionScope.SubQueryHint).ToArray();
				if (hints.Length == 0)
					return;
				if (hints.Any(e => !IsForUpdate(e)))
					throw new LinqToDBException("Oracle only supports FOR UPDATE subquery hints.");
				if (!ReferenceEquals(query, statement.SelectQuery))
					throw new LinqToDBException(NestedQueryError);
				if (statement is not SqlSelectStatement)
				{
					// DML already locks its target rows; a SELECT locking suffix is invalid here.
					query.SqlQueryExtensions.RemoveAll(IsForUpdate);
					return;
				}

				if (hints.Length != 1)
					throw new LinqToDBException("Oracle SELECT supports only one FOR UPDATE clause.");
				if (query.HasSetOperators)
					throw new LinqToDBException(NestedQueryError);
				if (query.Select.IsDistinct || !query.GroupBy.IsEmpty || QueryHelper.IsAggregationQuery(query))
					throw new LinqToDBException("Oracle FOR UPDATE cannot be used with DISTINCT, grouping or aggregate queries.");
				if (query.Select.TakeValue != null && IsSkipLocked(hints[0]))
					throw new LinqToDBException("Oracle FOR UPDATE SKIP LOCKED cannot be combined with row limiting (Take, First or Single): Oracle limits candidates before skipping locked rows.");
				if (query.Select.SkipValue != null || (query.Select.TakeValue != null && !query.OrderBy.IsEmpty))
					throw new LinqToDBException("Oracle FOR UPDATE cannot be combined with Skip or ordered row limiting.");
			});

			if (statement is SqlSelectStatement { SelectQuery: var select }
				&& select.SqlQueryExtensions?.Exists(IsForUpdate) == true && select.Select.TakeValue is { } take)
			{
				// FETCH NEXT is incompatible with FOR UPDATE, including First/Single's implicit Take.
				// Match Oracle 11's simple-limit lowering on every Oracle dialect.
				var rownum = new SqlExpression(mappingSchema.GetDbDataType(typeof(long)), "ROWNUM", Precedence.Primary,
					SqlFlags.IsAggregate | SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable);
				select.Where.EnsureConjunction().AddLessOrEqual(rownum, take, CompareNulls.LikeSql);
				select.Select.Take(null, null);
			}
		}

		const string HintTokenPattern = "--[^\\r\\n]*|/\\*[\\s\\S]*?\\*/|\"(?:\"\"|[^\"])*\"|[A-Za-z_][A-Za-z_0-9$#]*|\\S";

#if SUPPORTS_REGEX_GENERATORS
		[GeneratedRegex(HintTokenPattern, RegexOptions.ExplicitCapture)]
		private static partial Regex HintTokenRegex();
#else
		static readonly Regex _hintTokenRegex = new (HintTokenPattern, RegexOptions.ExplicitCapture);
		static Regex HintTokenRegex() => _hintTokenRegex;
#endif

		static bool IsSkipLocked(SqlQueryExtension extension)
		{
			if (!extension.Arguments.TryGetValue("hint", out var hint) || hint is not SqlValue { Value: string text })
				return false;

			// Keep quoted identifiers and punctuation intact, but ignore SQL comments.
			// Both the typed suffix and a generic FOR UPDATE clause end in SKIP LOCKED.
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
