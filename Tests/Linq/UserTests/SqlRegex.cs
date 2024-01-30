using System.Linq;
using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Data.Common;
using System.Linq.Expressions;

namespace LinqToDB
{
	public static class SqlRegex
	{
		class MyRegEx : SQLiteFunction
		{
			public override object Invoke(object[] args)
			{
				return Regex.IsMatch(Convert.ToString(args![1]!)!, Convert.ToString(args![0]!)!);
			}
		}
		public static void EnableSqliteRegex(DataConnection connection)
		{
			EnableSqliteRegex(connection.Connection);
		}

		public static void EnableSqliteRegex(DbConnection connection)
		{
			if (connection is SqliteConnection ms)
			{
				ms.CreateFunction(
				"regexp",
				(string pattern, string input)
					=> Regex.IsMatch(input, pattern));
			}
			else if (connection is SQLiteConnection sqllite)
			{
				sqllite.BindFunction(
				new SQLiteFunctionAttribute() { Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar },
				new MyRegEx());
			}
		}

		/// <summary>
		/// For SQLServer support you need to register the function from
		/// https://github.com/infiniteloopltd/SQLServerRegex
		/// </summary>
		public static void AddRegexSupport()
		{
			Linq.Expressions.MapMember<Regex, string, bool>((r, s) => r.IsMatch(s), (Expression<Func<Regex, string, bool>>)((p, p1) => IsMatch(p1, p.ToString())));
		}

		/// <summary>
		/// Runs a Regex against a string and returns true when it matches
		/// </summary>
		/// <param name="text">The string to Test against.</param>
		/// <param name="expression">The Regular Expression. (Datbase specific)</param>
		/// <returns></returns>
		[LinqToDB.Sql.Expression(ProviderName.MySql, "{0} REGEXP {1}", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.SQLite, "{0} REGEXP {1}", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.PostgreSQL, "{0} ~ {1}", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.Oracle, "REGEXP_LIKE({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.SapHana, "{0} REGEXP {1}", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.DB2, "REGEXP_LIKE({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.DB2LUW, "REGEXP_LIKE({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.DB2zOS, "REGEXP_LIKE({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.Firebird, "{0} SIMILAR TO {1}", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.Informix, "regex_match({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.ClickHouse, "match({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		[LinqToDB.Sql.Expression(ProviderName.SqlServer, "dbo.regex({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		public static bool IsMatch(string text, string expression)
		{
			return Regex.IsMatch(text, expression);
		}
	}
}
