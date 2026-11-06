using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using Microsoft.Data.Sqlite;

namespace LinqToDB
{
	public static class SqlRegex
	{
		[Flags]
		public enum RegexOptions
		{
			IgnoreCase,
			Multiline,
			Singleline,
		}

		sealed class RegExIsMatch : SQLiteFunction
		{
			public override object Invoke(object[] args)
			{
				if (args == null || args.Length < 2)
					return false;

				var input   = Convert.ToString(args[0]);
				var pattern = Convert.ToString(args[1]);
				var options = args.Length > 2 ? Convert.ToString(args[2]) : null;

				return IsMatchCore(input, pattern, options);
			}
		}

		sealed class RegExReplace : SQLiteFunction
		{
			public override object? Invoke(object[] args)
			{
				if (args == null || args.Length < 3)
					return null;

				var input       = Convert.ToString(args[0]);
				var pattern     = Convert.ToString(args[1]);
				var replacement = Convert.ToString(args[2]);

				return args.Length switch
				{
					3 => ReplaceCore(input, pattern, replacement),
					4 => ReplaceCore(input, pattern, replacement, Convert.ToString(args[3])),
					5 => ReplaceCore(input, pattern, replacement, Convert.ToInt32(args[3]), Convert.ToInt32(args[4]), null),
					6 => ReplaceCore(input, pattern, replacement, Convert.ToInt32(args[3]), Convert.ToInt32(args[4]), Convert.ToString(args[5])),
					_ => null,
				};
			}
		}

		static readonly List<SQLiteFunction> _sqliteFunctionsKeepAlive = new ();

		static System.Text.RegularExpressions.RegexOptions ParseRegexOptions(string? options)
		{
			var regexOptions = System.Text.RegularExpressions.RegexOptions.None;
			var optionFlags  = options ?? string.Empty;

			if (optionFlags.Length != 0)
			{
				if (optionFlags.Contains("i", StringComparison.Ordinal))
					regexOptions |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;

				if (optionFlags.Contains("m", StringComparison.Ordinal))
					regexOptions |= System.Text.RegularExpressions.RegexOptions.Multiline;

				if (optionFlags.Contains("s", StringComparison.Ordinal))
					regexOptions |= System.Text.RegularExpressions.RegexOptions.Singleline;
			}

			return regexOptions;
		}

		static System.Text.RegularExpressions.RegexOptions ConvertRegexOptions(object? options)
		{
			if (options is System.Text.RegularExpressions.RegexOptions framework)
				return framework;

			if (options is RegexOptions custom)
			{
				var regexOptions = System.Text.RegularExpressions.RegexOptions.None;
				if (custom.HasFlag(RegexOptions.IgnoreCase))
					regexOptions |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
				if (custom.HasFlag(RegexOptions.Multiline))
					regexOptions |= System.Text.RegularExpressions.RegexOptions.Multiline;
				if (custom.HasFlag(RegexOptions.Singleline))
					regexOptions |= System.Text.RegularExpressions.RegexOptions.Singleline;
				return regexOptions;
			}

			return System.Text.RegularExpressions.RegexOptions.None;
		}

		static bool IsMatchCore(string? input, string? pattern, string? options)
		{
			if (input == null || pattern == null)
				return false;

			return Regex.IsMatch(input, pattern, ParseRegexOptions(options));
		}

		static string? ReplaceCore(string? input, string? pattern, string? replacement)
		{
			return ReplaceCore(input, pattern, replacement, null);
		}

		static string? ReplaceCore(string? input, string? pattern, string? replacement, string? options)
		{
			if (input == null || pattern == null || replacement == null)
				return input;

			return Regex.Replace(input, pattern, replacement, ParseRegexOptions(options));
		}

		static string? ReplaceCore(string? input, string? pattern, string? replacement, int start, int count, string? options)
		{
			if (input == null || pattern == null || replacement == null)
				return input;

			var regex    = new Regex(pattern, ParseRegexOptions(options));
			var startAt  = Math.Max(0, start - 1);
			var maxCount = count <= 0 ? -1 : count;

			return regex.Replace(input, replacement, maxCount, startAt);
		}

		public static void EnableSqliteRegex(DataConnection connection)
		{
			EnableSqliteRegex(connection.TryGetDbConnection()!);
		}

		public static void EnableSqliteRegex(DbConnection connection)
		{
			if (connection is SqliteConnection ms)
			{
				ms.CreateFunction(
				"regexp",
				(string pattern, string input)
					=> IsMatchCore(input, pattern, null));

				ms.CreateFunction(
				"REGEXP_LIKE",
				(string input, string pattern)
					=> IsMatchCore(input, pattern, null));

				ms.CreateFunction(
				"REGEXP_LIKE",
				(string input, string pattern, string options)
					=> IsMatchCore(input, pattern, options));

				ms.CreateFunction(
				"REGEXP_REPLACE",
				(string input, string pattern, string replacement)
					=> ReplaceCore(input, pattern, replacement));

				ms.CreateFunction(
				"REGEXP_REPLACE",
				(string input, string pattern, string replacement, string options)
					=> ReplaceCore(input, pattern, replacement, options));

				ms.CreateFunction(
				"REGEXP_REPLACE",
				(string input, string pattern, string replacement, int start, int count)
					=> ReplaceCore(input, pattern, replacement, start, count, null));

				ms.CreateFunction(
				"REGEXP_REPLACE",
				(string input, string pattern, string replacement, int start, int count, string options)
					=> ReplaceCore(input, pattern, replacement, start, count, options));
			}
			else if (connection is SQLiteConnection sqllite)
			{
				var regExIsMatch = new RegExIsMatch();
				var regExReplace = new RegExReplace();

				lock (_sqliteFunctionsKeepAlive)
				{
					_sqliteFunctionsKeepAlive.Add(regExIsMatch);
					_sqliteFunctionsKeepAlive.Add(regExReplace);
				}

				sqllite.BindFunction(
					new SQLiteFunctionAttribute() { Name = "REGEXP_LIKE", Arguments = 2, FuncType = FunctionType.Scalar, FuncFlags = SQLiteFunctionFlags.SQLITE_DETERMINISTIC },
					regExIsMatch);

				sqllite.BindFunction(
					new SQLiteFunctionAttribute() { Name = "REGEXP_LIKE", Arguments = 3, FuncType = FunctionType.Scalar, FuncFlags = SQLiteFunctionFlags.SQLITE_DETERMINISTIC },
					regExIsMatch);

				sqllite.BindFunction(
					new SQLiteFunctionAttribute() { Name = "REGEXP_REPLACE", Arguments = 3, FuncType = FunctionType.Scalar, FuncFlags = SQLiteFunctionFlags.SQLITE_DETERMINISTIC },
					regExReplace);

				sqllite.BindFunction(
					new SQLiteFunctionAttribute() { Name = "REGEXP_REPLACE", Arguments = 4, FuncType = FunctionType.Scalar, FuncFlags = SQLiteFunctionFlags.SQLITE_DETERMINISTIC },
					regExReplace);

				sqllite.BindFunction(
					new SQLiteFunctionAttribute() { Name = "REGEXP_REPLACE", Arguments = 5, FuncType = FunctionType.Scalar, FuncFlags = SQLiteFunctionFlags.SQLITE_DETERMINISTIC },
					regExReplace);

				sqllite.BindFunction(
					new SQLiteFunctionAttribute() { Name = "REGEXP_REPLACE", Arguments = 6, FuncType = FunctionType.Scalar, FuncFlags = SQLiteFunctionFlags.SQLITE_DETERMINISTIC },
					regExReplace);

			}
		}

		sealed class RegexReplaceOptionsBuilder : LinqToDB.Sql.IExtensionCallBuilder
		{
			public void Build(LinqToDB.Sql.ISqlExtensionBuilder builder)
			{
				var optionsIndex = builder.Arguments.Length - 1;
				var options     = ConvertRegexOptions(builder.GetObjectValue(optionsIndex));
				var config      = builder.Configuration ?? string.Empty;

				var flags = RegexOptionsToFlags(options);
				if (config.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
					flags = flags.Replace("s", "n", StringComparison.Ordinal);

				if (config.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
				{
					var pgFlags = flags;
					if (!pgFlags.Contains("g", StringComparison.Ordinal))
						pgFlags += "g";
					flags = pgFlags;
				}

				var args = new ISqlExpression[builder.Arguments.Length];
				for (var i = 0; i < optionsIndex; i++)
					args[i] = builder.GetExpression(i)!;
				args[optionsIndex] = new SqlFragment($"'{flags}'");

				builder.ResultExpression = new SqlExpression(
					builder.Mapping.GetDbDataType(typeof(string)),
					builder.Expression,
					args);
			}
		}

		sealed class RegexReplaceUnsupportedOptionsBuilder : LinqToDB.Sql.IExtensionCallBuilder
		{
			public void Build(LinqToDB.Sql.ISqlExtensionBuilder builder)
			{
				var options = ConvertRegexOptions(builder.GetObjectValue(builder.Arguments.Length - 1));
				if (options != System.Text.RegularExpressions.RegexOptions.None)
					throw new LinqToDBException($"Regex replace options are not supported by provider '{builder.Configuration ?? string.Empty}'.");

				var text        = builder.GetExpression(0)!;
				var pattern     = builder.GetExpression(1)!;
				var replacement = builder.GetExpression(2)!;

				builder.ResultExpression = new SqlExpression(
					builder.Mapping.GetDbDataType(typeof(string)),
					"REGEXP_REPLACE({0}, {1}, {2})",
					text, pattern, replacement);
			}
		}

		sealed class RegexIsMatchBuilder : LinqToDB.Sql.IExtensionCallBuilder
		{
			public void Build(LinqToDB.Sql.ISqlExtensionBuilder builder)
			{
				var text       = builder.GetExpression(0)!;
				var pattern    = builder.GetExpression(1)!;
				var options    = ConvertRegexOptions(builder.GetObjectValue(2));
				var config     = builder.Configuration ?? string.Empty;

				if (options != System.Text.RegularExpressions.RegexOptions.None &&
					(config.Contains("SapHana", StringComparison.OrdinalIgnoreCase) ||
					 config.Contains("Firebird", StringComparison.OrdinalIgnoreCase) ||
					 config.Contains("DB2", StringComparison.OrdinalIgnoreCase) ||
					 config.Contains("Informix", StringComparison.OrdinalIgnoreCase) ||
					 config.Contains("ClickHouse", StringComparison.OrdinalIgnoreCase)))
				{
					throw new LinqToDBException($"Regex options are not supported by provider '{config}' for SqlRegex.IsMatch overload with options.");
				}

				var dbType = builder.Mapping.GetDbDataType(typeof(bool));

				var fragment = "";
				if (options.HasFlag(System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				{
					fragment += "i";
				}

				if (options.HasFlag(System.Text.RegularExpressions.RegexOptions.Multiline))
				{
					fragment += "m";
				}

				if (options.HasFlag(System.Text.RegularExpressions.RegexOptions.Singleline))
				{
					fragment += "s";
				}

				var predicate = new SqlPredicate.Expr(
					new SqlExpression(dbType, builder.Expression, text, pattern, new SqlFragment($"'{fragment}'")));

				builder.ResultExpression = new SqlSearchCondition(false, canBeUnknown: null, predicate);
			}
		}

		public static void AddRegexSupport()
		{
			Linq.Expressions.MapMember<Regex, string, bool>((r, s) => r.IsMatch(s), (Expression<Func<Regex, string, bool>>)((p, p1) => p.Options == System.Text.RegularExpressions.RegexOptions.None ? IsMatch(p1, p.ToString()) : IsMatch(p1, p.ToString(), p.Options)));
			Linq.Expressions.MapMember<string, string, bool>((s, p) => Regex.IsMatch(s, p), (Expression<Func<string, string, bool>>)((p1, p2) => IsMatch(p1, p2)));
			Linq.Expressions.MapMember<string, string, System.Text.RegularExpressions.RegexOptions, bool>((s, p, o) => Regex.IsMatch(s, p, o), (Expression<Func<string, string, System.Text.RegularExpressions.RegexOptions, bool>>)((p1, p2, p3) => IsMatch(p1, p2, p3)));

			Linq.Expressions.MapMember<Regex, string, string, string>((r, s, repl) => r.Replace(s, repl), (Expression<Func<Regex, string, string, string>>)((p, p1, p2) => Replace(p1, p.ToString(), p2, p.Options)));
			Linq.Expressions.MapMember<Regex, string, string, int, string>((r, s, repl, count) => r.Replace(s, repl, count), (Expression<Func<Regex, string, string, int, string>>)((p, p1, p2, p3) => Replace(p1, p.ToString(), p2, 1, p3, p.Options)));
			Linq.Expressions.MapMember<Regex, string, string, int, int, string>((r, s, repl, count, startAt) => r.Replace(s, repl, count, startAt), (Expression<Func<Regex, string, string, int, int, string>>)((p, p1, p2, p3, p4) => Replace(p1, p.ToString(), p2, p4 + 1, p3, p.Options)));

			Linq.Expressions.MapMember<string, string, string, string>((s, p, repl) => Regex.Replace(s, p, repl), (Expression<Func<string, string, string, string>>)((p1, p2, p3) => Replace(p1, p2, p3)));
			Linq.Expressions.MapMember<string, string, string, System.Text.RegularExpressions.RegexOptions, string>((s, p, repl, o) => Regex.Replace(s, p, repl, o), (Expression<Func<string, string, string, System.Text.RegularExpressions.RegexOptions, string>>)((p1, p2, p3, p4) => Replace(p1, p2, p3, p4)));
		}

		/// <summary>
		/// Runs a Regex against a string and returns true when it matches
		/// </summary>
		/// <param name="text">The string to Test against.</param>
		/// <param name="pattern">The Regular Expression pattern. (Database specific)</param>
		/// <returns></returns>
		[LinqToDB.Sql.Expression("" ,                     "REGEXP_LIKE({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.MySql,      "{0} REGEXP {1}",        ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.MariaDB10,  "{0} REGEXP {1}",        ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.SapHana,    "{0} REGEXP {1}",        ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.Firebird,   "{0} SIMILAR TO {1}",    ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.Informix,   "regex_match({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.ClickHouse, "match({0}, {1})",       ServerSideOnly = true, IsPredicate = true)]
		public static bool IsMatch(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
		{
			return Regex.IsMatch(text, pattern);
		}

		/// <summary>
		/// Runs a Regex against a string and returns true when it matches
		/// </summary>
		/// <param name="text">The string to Test against.</param>
		/// <param name="pattern">The Regular Expression pattern. (Database specific)</param>
		/// <returns></returns>
		[LinqToDB.Sql.Extension("",                      "REGEXP_LIKE({0}, {1}, {2})", ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.SapHana,    "{0} REGEXP {1}",             ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.Firebird,   "{0} SIMILAR TO {1}",         ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.Informix,   "regex_match({0}, {1})",      ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.ClickHouse, "match({0}, {1})",            ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		public static bool IsMatch(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, System.Text.RegularExpressions.RegexOptions options)
		{
			return Regex.IsMatch(text, pattern, options);
		}

		/// <summary>
		/// Runs a Regex against a string and returns true when it matches
		/// </summary>
		/// <param name="text">The string to Test against.</param>
		/// <param name="pattern">The Regular Expression pattern. (Database specific)</param>
		/// <returns></returns>
		[LinqToDB.Sql.Extension("",                      "REGEXP_LIKE({0}, {1}, {2})", ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.SapHana,    "{0} REGEXP {1}",             ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.Firebird,   "{0} SIMILAR TO {1}",         ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.Informix,   "regex_match({0}, {1})",      ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		[LinqToDB.Sql.Extension(ProviderName.ClickHouse, "match({0}, {1})",            ServerSideOnly = true, IsPredicate = true, BuilderType = typeof(RegexIsMatchBuilder))]
		public static bool IsMatch(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, RegexOptions options)
		{
			var regexOptions = System.Text.RegularExpressions.RegexOptions.None;
			if (options.HasFlag(RegexOptions.IgnoreCase))
				regexOptions |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
			if (options.HasFlag(RegexOptions.Multiline))
				regexOptions |= System.Text.RegularExpressions.RegexOptions.Multiline;
			if (options.HasFlag(RegexOptions.Singleline))
				regexOptions |= System.Text.RegularExpressions.RegexOptions.Singleline;
			return Regex.IsMatch(text, pattern, regexOptions);
		}

		/// <summary>
		/// Runs a Regex against a string and returns true when it matches
		/// </summary>
		/// <param name="text">The string to Test against.</param>
		/// <param name="pattern">The Regular Expression pattern. (Database specific)</param>
		/// <param name="replacement">The Regular Expression pattern. (Database specific)</param>
		/// <returns></returns>
		[LinqToDB.Sql.Expression(ProviderName.MySql, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.SQLite, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.PostgreSQL, "REGEXP_REPLACE({0}, {1}, {2}, 'g')", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.Oracle, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.SapHana, "REPLACE_REGEXPR({1} IN {0} WITH {2} OCCURRENCE ALL)", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.DB2, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.DB2LUW, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.DB2zOS, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.Firebird, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.Informix, "regex_replace({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.ClickHouse, "replaceRegexpAll({0}, {1}, {2})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.SqlServer2025, "REGEXP_REPLACE({0}, {1}, {2})", ServerSideOnly = true)]
		public static string Replace(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement)
		{
			return Regex.Replace(text, pattern, replacement);
		}

		[LinqToDB.Sql.Expression(ProviderName.SQLite,        "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.PostgreSQL,    "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, 'g')", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.Oracle,        "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.MySql,         "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4})", ServerSideOnly = true)]
		[LinqToDB.Sql.Expression(ProviderName.SqlServer2025, "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4})", ServerSideOnly = true)]
		public static string Replace(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement, int start, int count)
		{
			return ReplaceCore(text, pattern, replacement, start, count, null)!;
		}

		[LinqToDB.Sql.Extension(ProviderName.SQLite,        "REGEXP_REPLACE({0}, {1}, {2}, {3})",       BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.PostgreSQL,    "REGEXP_REPLACE({0}, {1}, {2}, {3})",       BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.Oracle,        "REGEXP_REPLACE({0}, {1}, {2}, 1, 0, {3})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.MySql,         "REGEXP_REPLACE({0}, {1}, {2}, 1, 0, {3})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.SqlServer2025, "REGEXP_REPLACE({0}, {1}, {2}, 1, 0, {3})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension("",                         BuilderType = typeof(RegexReplaceUnsupportedOptionsBuilder),               ServerSideOnly = true)]
		public static string Replace(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options)
		{
			return Regex.Replace(text, pattern, replacement, options);
		}

		[LinqToDB.Sql.Extension(ProviderName.SQLite,        "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.PostgreSQL,    "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.Oracle,        "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.MySql,         "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.SqlServer2025, "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension("",                         BuilderType = typeof(RegexReplaceUnsupportedOptionsBuilder),               ServerSideOnly = true)]
		public static string Replace(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement, int start, int count, System.Text.RegularExpressions.RegexOptions options)
		{
			return ReplaceCore(text, pattern, replacement, start, count, RegexOptionsToFlags(options))!;
		}

		[LinqToDB.Sql.Extension(ProviderName.SQLite,        "REGEXP_REPLACE({0}, {1}, {2}, {3})",       BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.PostgreSQL,    "REGEXP_REPLACE({0}, {1}, {2}, {3})",       BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.Oracle,        "REGEXP_REPLACE({0}, {1}, {2}, 1, 0, {3})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.MySql,         "REGEXP_REPLACE({0}, {1}, {2}, 1, 0, {3})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.SqlServer2025, "REGEXP_REPLACE({0}, {1}, {2}, 1, 0, {3})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension("",                         BuilderType = typeof(RegexReplaceUnsupportedOptionsBuilder),               ServerSideOnly = true)]
		public static string Replace(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement, RegexOptions options)
		{
			return Regex.Replace(text, pattern, replacement, ConvertRegexOptions(options));
		}

		[LinqToDB.Sql.Extension(ProviderName.SQLite,        "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.PostgreSQL,    "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.Oracle,        "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.MySql,         "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension(ProviderName.SqlServer2025, "REGEXP_REPLACE({0}, {1}, {2}, {3}, {4}, {5})", BuilderType = typeof(RegexReplaceOptionsBuilder),          ServerSideOnly = true)]
		[LinqToDB.Sql.Extension("",                         BuilderType = typeof(RegexReplaceUnsupportedOptionsBuilder),               ServerSideOnly = true)]
		public static string Replace(string text, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement, int start, int count, RegexOptions options)
		{
			return Replace(text, pattern, replacement, start, count, ConvertRegexOptions(options));
		}

		static string RegexOptionsToFlags(System.Text.RegularExpressions.RegexOptions options)
		{
			var flags = "";
			if (options.HasFlag(System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				flags += "i";
			if (options.HasFlag(System.Text.RegularExpressions.RegexOptions.Multiline))
				flags += "m";
			if (options.HasFlag(System.Text.RegularExpressions.RegexOptions.Singleline))
				flags += "s";

			return flags;
		}
	}
}
